using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IAppLogService _appLog;

        public EmployeeController(AdventureWorksContext db, IMapper mapper, ILogger<EmployeeController> logger, IAppLogService appLog)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _appLog = appLog;
        }

        // -----------------------
        // Helpers
        // -----------------------
        private static bool IsSelfAccessAllowed(ClaimsPrincipal user, int requestedId)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var tokenBusinessEntityId = user.FindFirst("BusinessEntityID")?.Value;

            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                return true;

            return !string.IsNullOrEmpty(tokenBusinessEntityId)
                && tokenBusinessEntityId == requestedId.ToString();
        }

        private static string GenerateUsername(Person person)
        {
            if (person != null &&
                !string.IsNullOrWhiteSpace(person.FirstName) &&
                !string.IsNullOrWhiteSpace(person.LastName))
            {
                return $"{person.FirstName}.{person.LastName}{person.BusinessEntityID}@emailnadainventado.com".ToLowerInvariant();
            }
            return $"emp{person?.BusinessEntityID}";
        }

        private static string GenerateTempPassword(int length = 12)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*()-_=+[]{}";
            string all = upper + lower + digits + symbols;

            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = all[bytes[i] % all.Length];

            // Ensure at least one of each type
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = lower[bytes[1] % lower.Length];
            chars[2] = digits[bytes[2] % digits.Length];
            chars[3] = symbols[bytes[3] % symbols.Length];

            return new string(chars);
        }

        private static string HashWithBcrypt(string plain) =>
            BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 11);

        // -----------------------
        // GET: api/v1/employee
        // -----------------------
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<List<EmployeeDto>>> GetAll(CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter todos os Employees (Role=admin).");
            await _appLog.InfoAsync("Recebida requisição para obter todos os Employees (Role=admin).");

            try
            {
                // 1) Obter dados via helper dedicado (reutilizável)
                var employees = await GetAllEmployeesWithIncludesAsync(ct);

                // 2) Logs e persistência de log (conforme original)
                _logger.LogInformation("Encontrados {Count} Employees.", employees.Count);
                await _appLog.InfoAsync($"Encontrados {employees.Count} Employees.");
                await _db.SaveChangesAsync(ct);

                // 3) Mapear para DTOs e devolver
                var employeesDto = _mapper.Map<List<EmployeeDto>>(employees);
                _logger.LogInformation("Requisição para obter Employees executada com sucesso.");
                await _appLog.InfoAsync("Requisição para obter Employees executada com sucesso.");
                return Ok(employeesDto);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedEmployeeErrorAsync(ex, ct);
            }
        }

        [HttpGet("paged")]
        [Authorize(Roles = "admin")]

        public async Task<ActionResult<PagedResult<EmployeeDto>>> GetAllPagination(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = "asc",
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Recebida requisição para obter Employees (admin).");
            await _appLog.InfoAsync("Recebida requisição para obter Employees (admin).");

            try
            {
                const int MaxPageSize = 200;
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > MaxPageSize) pageSize = MaxPageSize;

                IQueryable<Employee> query = _db.Employees
                    .AsNoTracking()
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories)
                        .ThenInclude(dh => dh.Department)
                    .Include(e => e.Person);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string term = search.Trim().ToLower();
                    query = query.Where(e =>
                        (e.Person!.FirstName != null && e.Person.FirstName.ToLower().Contains(term)) ||
                        (e.Person.LastName != null && e.Person.LastName.ToLower().Contains(term))
                    );
                }

                query = query.Where(item => item.CurrentFlag).OrderBy(n => n.Person!.FirstName);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                _logger.LogInformation("Encontrados {Count} Employees (antes da paginação).", totalCount);
                await _appLog.InfoAsync($"Encontrados {totalCount} Employees (antes da paginação).");
                await _db.SaveChangesAsync(ct);

                var itemsDto = _mapper.Map<List<EmployeeDto>>(items);

                var result = new PagedResult<EmployeeDto>
                {
                    Items = itemsDto,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var paginationHeader = System.Text.Json.JsonSerializer.Serialize(new
                {
                    result.TotalCount,
                    result.PageNumber,
                    result.PageSize,
                    result.TotalPages,
                    result.HasPrevious,
                    result.HasNext
                });
                Response.Headers["X-Pagination"] = paginationHeader;

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedEmployeeErrorAsync(ex, ct);
            }
        }

        private async Task<List<Employee>> GetAllEmployeesWithIncludesAsync(CancellationToken ct)
        {
            return await _db.Employees
                .Include(e => e.PayHistories)
                .Include(e => e.DepartmentHistories)
                    .ThenInclude(dh => dh.Department)
                .Include(e => e.Person)
                .ToListAsync(ct);
        }
        // -----------------------
        // GET: api/v1/employee/{id}
        // -----------------------
        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id, CancellationToken ct)
        {
            // 0) Autorização específica (mantida)
            if (!IsSelfAccessAllowed(HttpContext.User, id))
            {
                _logger.LogWarning("Tentativa de acesso não autorizada. ID solicitado={RequestedId}.", id);
                await _appLog.WarnAsync($"Tentativa de acesso não autorizada. ID solicitado={id}.");
                return Forbid();
            }

            // 1) Logging de entrada (mantendo padrão e SaveChanges inicial)
            _logger.LogInformation("Recebida requisição para obter Employee com ID={Id}.", id);
            await _appLog.InfoAsync($"Recebida requisição para obter Employee com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                // 2) Obter Employee com Includes via helper reutilizável
                var employee = await GetEmployeeWithIncludesAsync(id, ct);
                if (employee is null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={Id}.", id);
                    await _appLog.WarnAsync($"Employee não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                // 3) Log de sucesso (mantendo SaveChanges como no original)
                _logger.LogInformation("Employee encontrado para ID={Id}.", id);
                await _appLog.InfoAsync($"Employee encontrado para ID={id}.");
                await _db.SaveChangesAsync(ct);

                // 4) Mapear e devolver
                return Ok(_mapper.Map<EmployeeDto>(employee));
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedEmployeeErrorAsync(ex, ct);
            }
        }

        [HttpGet("{id:int}/paged")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult> GetByEmployee(
                int id,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                CancellationToken ct = default)
        {
            _logger.LogInformation("Listar DepartmentHistories do employee {Id} page={Page} size={Size}", id, pageNumber, pageSize);
            await _appLog.InfoAsync($"Listar DepartmentHistories do employee {id} page={pageNumber} size={pageSize}");

            // Sanitização de parâmetros
            if (pageNumber < 1) pageNumber = 1;
            const int MaxPageSize = 100;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            // Confirma se o employee existe (opcional mas recomendado)
            var employeeExists = await _db.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessEntityID == id, ct);

            if (!employeeExists)
            {
                _logger.LogWarning("Employee {Id} não encontrado.", id);
                await _appLog.WarnAsync($"Employee {id} não encontrado.");
                return NotFound(new { message = $"Employee {id} não encontrado." });
            }

            // Query base: histories do employee + include Department
            var q = _db.DepartmentHistories
                .AsNoTracking()
                .Where(h => h.BusinessEntityID == id)
                .Include(h => h.Department);

            // Total antes da paginação
            var totalCount = await q.CountAsync(ct);

            // Ordenação + paginação (mais recente primeiro)
            var skip = (pageNumber - 1) * pageSize;
            var items = await q
                .OrderByDescending(h => h.StartDate)
                .Skip(skip)
                .Take(pageSize)
                .Select(h => new DepartmentHistoryDto
                {
                    BusinessEntityID = h.BusinessEntityID,
                    DepartmentId = h.DepartmentID,
                    ShiftID = h.ShiftID,
                    StartDate = h.StartDate,
                    EndDate = h.EndDate,
                    Department = new DepartmentDto
                    {
                        DepartmentID = h.Department!.DepartmentID,
                        Name = h.Department.Name,
                        GroupName = h.Department.GroupName
                    }
                })
                .ToListAsync(ct);

            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

            var response = new
            {
                items,
                meta = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages
                }
            };
            _logger.LogInformation("Employee encontrado para ID={Id}.", id);
            await _appLog.InfoAsync($"Employee encontrado para ID={id}.");
            await _db.SaveChangesAsync(ct);
            return Ok(response);
        }

        private async Task<Employee?> GetEmployeeWithIncludesAsync(int id, CancellationToken ct)
        {
            return await _db.Employees
                .Include(e => e.PayHistories)
                .Include(e => e.DepartmentHistories).ThenInclude(dh => dh.Department)
                .Include(e => e.Person)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id, ct);
        }

        // -----------------------
        // DELETE (soft): api/v1/employee/{id}
        // -----------------------
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            // 1) Logging inicial + persistência de log (como no original)
            _logger.LogInformation("Recebida requisição para eliminar (soft delete) Employee com ID={Id}.", id);
            await _appLog.InfoAsync($"Recebida requisição para eliminar (soft delete) Employee com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                // 2) Obter employee (sem includes) via helper
                var employee = await GetEmployeeByIdAsync(id, ct);
                if (employee is null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={Id}.", id);
                    await _appLog.WarnAsync($"Employee não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                // 3) Log de encontrado (mantendo mensagem original) 
                _logger.LogInformation("Employee encontrado para ID={Id}. A marcar como inativo…", id);
                await _appLog.InfoAsync($"Employee encontrado para ID={id}. A marcar como inativo…");

                // 4) Marcar como inativo (soft delete) e gravar
                ApplyEmployeeSoftDelete(employee);
                await _db.SaveChangesAsync(ct);

                // 5) Log de sucesso + persistência (como no original)
                _logger.LogInformation("Employee marcado como inativo com sucesso. ID={Id}.", id);
                await _appLog.InfoAsync($"Employee marcado como inativo com sucesso. ID={id}.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleEmployeeSoftDeleteDbErrorAsync(dbEx, id, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedEmployeeErrorAsync(ex, ct);
            }
        }

        private async Task<Employee?> GetEmployeeByIdAsync(int id, CancellationToken ct)
        {
            return await _db.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == id, ct);
        }

        private void ApplyEmployeeSoftDelete(Employee employee)
        {
            employee.CurrentFlag = false;
            employee.ModifiedDate = DateTime.UtcNow;
        }

        private async Task<IActionResult> HandleEmployeeSoftDeleteDbErrorAsync(DbUpdateException dbEx, int id, CancellationToken ct)
        {
            _logger.LogError(dbEx, "Erro ao atualizar Employee (soft delete) para ID={Id}.", id);
            await _appLog.ErrorAsync($"Erro ao atualizar Employee (soft delete) para ID={id}.", dbEx);

            return Problem(
                title: "Erro ao atualizar",
                detail: "Erro ao atualizar o empregado.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        private async Task<ActionResult> HandleUnexpectedEmployeeErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro inesperado ao processar Employee.");
            await _appLog.ErrorAsync($"Erro inesperado ao processar Employee.", ex);

            return Problem(
                title: "Erro ao processar empregado",
                detail: "Ocorreu um erro ao processar o empregado.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // -----------------------
        // PATCH: api/v1/employee/{id}
        // -----------------------
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> Patch(int id, [FromBody] EmployeeDto employeeDto, CancellationToken ct)
        {
            // Logging inicial e persistência (como no original)    
            _logger.LogInformation("Recebida requisição para atualizar parcialmente Employee com ID={Id}.", id);
            await _appLog.InfoAsync($"Recebida requisição para atualizar parcialmente Employee com ID={id}.");
            await _db.SaveChangesAsync(ct);

            // Validação de body
            if (employeeDto is null)
            {
                _logger.LogWarning("DTO ausente no corpo da requisição para Patch de Employee ID={Id}.", id);
                await _appLog.WarnAsync($"DTO ausente no corpo da requisição para Patch de Employee ID={id}.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Body is required." });
            }

            // Validação de ID no path vs body
            if (id != employeeDto.BusinessEntityID)
            {
                _logger.LogWarning("ID no path ({PathId}) difere do ID no corpo ({BodyId}).", id, employeeDto.BusinessEntityID);
                await _appLog.WarnAsync($"ID no path ({id}) difere do ID no corpo ({employeeDto.BusinessEntityID}).");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Path ID must match body BusinessEntityID." });
            }

            // Autorização (mantida exatamente como no original)
            if (!IsSelfAccessAllowed(HttpContext.User, id))
            {
                _logger.LogWarning("Tentativa de acesso não autorizada. ID solicitado={RequestedId}.", id);
                await _appLog.WarnAsync($"Tentativa de acesso não autorizada. ID solicitado={id}.");
                return Forbid();
            }

            try
            {
                // 1) Obter Employee com includes (reutiliza helper do GET)
                var employee = await GetEmployeeWithIncludesAsync(id, ct);
                if (employee is null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={Id}.", id);
                    await _appLog.WarnAsync($"Employee não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                // 2) Log de encontrado (mantendo mensagem original)        
                _logger.LogInformation("Employee encontrado para ID={Id}. A aplicar alterações parciais…", id);
                await _appLog.InfoAsync($"Employee encontrado para ID={id}. A aplicar alterações parciais…");

                // 3) Aplicar alterações parciais (isolado em helper)
                await ApplyEmployeePartialUpdate(employeeDto, employee, ct);

                // 4) Gravar alterações
                await _db.SaveChangesAsync(ct);

                // 5) Log de sucesso + persistência (mantido)
                _logger.LogInformation("Patch aplicado com sucesso para Employee ID={Id}.", id);
                await _appLog.InfoAsync($"Patch aplicado com sucesso para Employee ID={id}.");
                await _db.SaveChangesAsync(ct);

                // 6) Resposta
                return Ok(_mapper.Map<EmployeeDto>(employee));
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleEmployeePatchDbErrorAsync(dbEx, id, ct);
            }
            catch (Exception ex)
            {
                // Reutiliza o handler genérico de erro inesperado (já existente nesta classe)
                return await HandleUnexpectedEmployeeErrorAsync(ex, ct);
            }
        }
        private async Task ApplyEmployeePartialUpdate(EmployeeDto dto, Employee employee, CancellationToken ct)
        {
            // Campos simples (guarda sem alterar semântica do original)
            if (!string.IsNullOrEmpty(dto.LoginID)) employee.LoginID = dto.LoginID;
            if (!string.IsNullOrEmpty(dto.JobTitle)) employee.JobTitle = dto.JobTitle;
            if (!string.IsNullOrEmpty(dto.Gender)) employee.Gender = dto.Gender;
            if (!string.IsNullOrEmpty(dto.MaritalStatus)) employee.MaritalStatus = dto.MaritalStatus;
            if (!string.IsNullOrEmpty(dto.NationalIDNumber)) employee.NationalIDNumber = dto.NationalIDNumber;

            if (dto.VacationHours != default(short)) employee.VacationHours = dto.VacationHours;
            if (dto.SickLeaveHours != default(short)) employee.SickLeaveHours = dto.SickLeaveHours;
            if (dto.SalariedFlag != default(bool)) employee.SalariedFlag = dto.SalariedFlag;
            if (dto.HireDate != default(DateTime)) employee.HireDate = dto.HireDate;
            if (dto.BirthDate != default(DateTime)) employee.BirthDate = dto.BirthDate;
            if (dto.DepartmentID.HasValue)
            {
                var nowUtc = DateTime.UtcNow;

                var currentMovement = await _db.DepartmentHistories
                    .FirstOrDefaultAsync(dh => dh.BusinessEntityID == dto.BusinessEntityID && dh.StartDate < nowUtc && (dh.EndDate > nowUtc || dh.EndDate == null), ct);

                _logger.LogInformation("Encontrados movimento para BusinessEntityID={BEID}", dto.BusinessEntityID);
                await _appLog.InfoAsync($"Encontrados movimento para BEID={dto.BusinessEntityID}");

                var newMovement = new DepartmentHistory
                {
                    BusinessEntityID = dto.BusinessEntityID,
                    DepartmentID = (short)dto.DepartmentID,
                    StartDate = nowUtc,
                    ModifiedDate = nowUtc
                };

                if (currentMovement != null)
                {
                    newMovement.EndDate = currentMovement.EndDate;
                    newMovement.ShiftID = currentMovement.ShiftID;
                    currentMovement!.EndDate = nowUtc;
                }
                else
                {
                    newMovement.ShiftID = 1;  //Por default vão todos para o turno da manhã
                }

                _logger.LogInformation("Alteração de departamento para BusinessEntityID={BEID} realizada com sucesso", dto.BusinessEntityID);
                await _appLog.InfoAsync($"Alteração de departamento para BEID={dto.BusinessEntityID} realizada com sucesso");

                _db.DepartmentHistories.Add(newMovement);
            }

            employee.ModifiedDate = DateTime.UtcNow;

            // Subentidade Person
            if (employee.Person is not null && dto.Person is not null)
            {
                var p = employee.Person;
                var pd = dto.Person;

                if (!string.IsNullOrWhiteSpace(pd.FirstName)) p.FirstName = pd.FirstName;
                if (!string.IsNullOrWhiteSpace(pd.LastName)) p.LastName = pd.LastName;

                // Permitir null explícito quando vier string vazia
                p.MiddleName = pd.MiddleName != null && string.IsNullOrWhiteSpace(pd.MiddleName) ? null : pd.MiddleName ?? p.MiddleName;
                p.Title = pd.Title != null && string.IsNullOrWhiteSpace(pd.Title) ? null : pd.Title ?? p.Title;
                p.Suffix = pd.Suffix != null && string.IsNullOrWhiteSpace(pd.Suffix) ? null : pd.Suffix ?? p.Suffix;

                p.ModifiedDate = DateTime.UtcNow;
            }
        }
        private async Task<IActionResult> HandleEmployeePatchDbErrorAsync(DbUpdateException dbEx, int id, CancellationToken ct)
        {
            _logger.LogError(dbEx, "Erro ao persistir alterações do Patch para Employee ID={Id}.", id);
            await _appLog.ErrorAsync($"Erro ao persistir alterações do Patch para Employee ID={id}.", dbEx);

            return Problem(
                title: "Erro ao atualizar",
                detail: "Erro ao atualizar o empregado.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // -----------------------
        // POST: api/v1/employee/approve/{jobCandidateId}
        // -----------------------
        [HttpPost("approve/{jobCandidateId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ApproveCandidate(int jobCandidateId, CancellationToken ct)
        {
            _logger.LogInformation("Iniciando aprovação do candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
            await _appLog.InfoAsync($"Iniciando aprovação do candidato. JobCandidateId={jobCandidateId}");
            await _db.SaveChangesAsync(ct);

            // 0) Obter candidato
            var candidate = await GetJobCandidateAsync(jobCandidateId, ct);
            if (candidate is null)
            {
                _logger.LogWarning("Candidato não encontrado. JobCandidateId={JobCandidateId}", jobCandidateId);
                await _appLog.WarnAsync($"Candidato não encontrado. JobCandidateId={jobCandidateId}");
                await _db.SaveChangesAsync(ct);
                return NotFound(new { message = "Candidato não encontrado", jobCandidateId });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            await _appLog.InfoAsync($"Transação iniciada para aprovação. JobCandidateId={jobCandidateId}");
            await _db.SaveChangesAsync(ct);

            try
            {
                // 1) Se existir empregado anterior inativo pelo mesmo NID, reativar e concluir
                var reactivated = await TryReactivatePreviousEmployeeAsync(candidate, transaction, ct);
                if (reactivated is IActionResult earlyResult)
                    return earlyResult;

                // 2) Criar BusinessEntity
                var now = DateTime.UtcNow;
                var beId = await CreateBusinessEntityAsync(now, ct);

                _logger.LogInformation("BusinessEntity criado. BusinessEntityID={BusinessEntityID}", beId);
                await _appLog.InfoAsync($"BusinessEntity criado. BusinessEntityID={beId}");
                await _db.SaveChangesAsync(ct);

                // 3) Criar Person
                var person = await CreatePersonForEmployeeAsync(beId, candidate, now, ct);

                _logger.LogInformation("Person criado. BusinessEntityID={BusinessEntityID}", beId);
                await _appLog.InfoAsync($"Person criado. BusinessEntityID={beId}");
                await _db.SaveChangesAsync(ct);

                // 4) Gerar username e validar unicidade
                var username = GenerateUsername(person);
                if (await UsernameExistsAsync(username, ct))
                {
                    _logger.LogWarning("Conflito de username. Username={Username}", username);
                    await _appLog.WarnAsync($"Conflito de username. Username={username}");
                    await _db.SaveChangesAsync(ct);

                    await transaction.RollbackAsync(ct);
                    await _appLog.InfoAsync($"Transação revertida por conflito de username. JobCandidateId={jobCandidateId}");
                    await _db.SaveChangesAsync(ct);

                    return Conflict(new { message = "Username já existe", username });
                }

                _logger.LogInformation("Username gerado e validado. Username={Username}", username);
                await _appLog.InfoAsync($"Username gerado e validado. Username={username}");
                await _db.SaveChangesAsync(ct);

                // 5) Criar Employee
                var employee = CreateEmployeeEntity(beId, person, candidate, username, now);
                _db.Employees.Add(employee);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Employee criado. BusinessEntityID={BusinessEntityID}, LoginID={LoginID}", beId, employee.LoginID);
                await _appLog.InfoAsync($"Employee criado. BusinessEntityID={beId}, LoginID={employee.LoginID}");
                await _db.SaveChangesAsync(ct);

                // 6) Criar SystemUser e remover candidato
                var (sysUser, tempPassword, role) = CreateSystemUserForNewEmployee(beId, username, candidate);
                _db.SystemUsers.Add(sysUser);
                _db.JobCandidates.Remove(candidate);
                await _db.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
                _logger.LogInformation("SystemUser criado e candidato removido. SystemUserId={SystemUserId}, Username={Username}", sysUser.SystemUserId, sysUser.Username);
                await _appLog.InfoAsync($"SystemUser criado e candidato removido. SystemUserId={sysUser.SystemUserId}, Username={sysUser.Username}");
                await _appLog.InfoAsync($"Transação concluída com sucesso. BusinessEntityID={beId}");
                await _db.SaveChangesAsync(ct);

                // DEV-only: expõe tempPassword na resposta (não fazer em produção)
                var createdDto = _mapper.Map<EmployeeDto>(employee);
                return CreatedAtAction(
                    nameof(GetEmployee),
                    new { id = beId },
                    new
                    {
                        employee = createdDto,
                        systemUserId = sysUser.SystemUserId,
                        username,
                        role,
                        tempPassword // DEV only
                    });
            }
            catch (DbUpdateException ex)
            {
                return await HandleApproveDbUpdateExceptionAsync(ex, jobCandidateId, transaction, ct);
            }
            catch (Exception ex)
            {
                return await HandleApproveUnexpectedErrorAsync(ex, jobCandidateId, transaction, ct);
            }
        }
        private async Task<JobCandidate?> GetJobCandidateAsync(int jobCandidateId, CancellationToken ct)
        {
            return await _db.JobCandidates.FirstOrDefaultAsync(jc => jc.JobCandidateId == jobCandidateId, ct);
        }
        private async Task<IActionResult?> TryReactivatePreviousEmployeeAsync(
            JobCandidate candidate,
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
            CancellationToken ct)
        {
            var previousEmployee = await _db.Employees
                .FirstOrDefaultAsync(pe => pe.NationalIDNumber == candidate.NationalIDNumber, ct);

            if (previousEmployee is null || previousEmployee.CurrentFlag)
                return null;

            _logger.LogInformation("Ativar empregado existente. BusinessEntityID={BusinessEntityID}", previousEmployee.BusinessEntityID);
            await _appLog.InfoAsync($"Ativar empregado existente. BusinessEntityID={previousEmployee.BusinessEntityID}");

            previousEmployee.CurrentFlag = true;
            previousEmployee.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            _db.JobCandidates.Remove(candidate);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Transação concluída. BusinessEntityID={BusinessEntityID}", previousEmployee.BusinessEntityID);
            await _appLog.InfoAsync($"Transação concluída. BusinessEntityID={previousEmployee.BusinessEntityID}");
            await _db.SaveChangesAsync(ct);

            var dto = _mapper.Map<EmployeeDto>(previousEmployee);
            return CreatedAtAction(nameof(GetEmployee), new { id = previousEmployee.BusinessEntityID }, dto);
        }
        private async Task<int> CreateBusinessEntityAsync(DateTime now, CancellationToken ct)
        {
            var be = new BusinessEntity { RowGuid = Guid.NewGuid(), ModifiedDate = now };
            _db.BusinessEntities.Add(be);
            await _db.SaveChangesAsync(ct);
            return be.BusinessEntityID;
        }
        private async Task<Person> CreatePersonForEmployeeAsync(int beId, JobCandidate candidate, DateTime now, CancellationToken ct)
        {
            var person = new Person
            {
                BusinessEntityID = beId,
                FirstName = candidate.FirstName ?? "",
                LastName = candidate.LastName ?? "",
                EmailPromotion = 0,
                ModifiedDate = now,
                PersonType = "EM"
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync(ct);
            return person;
        }
        private async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
        {
            return await _db.SystemUsers.AnyAsync(u => u.Username == username, ct);
        }
        private Employee CreateEmployeeEntity(int beId, Person person, JobCandidate candidate, string username, DateTime now)
        {
            return new Employee
            {
                BusinessEntityID = beId,
                VacationHours = 0,
                LoginID = username,
                SickLeaveHours = 0,
                SalariedFlag = true,
                NationalIDNumber = candidate.NationalIDNumber,
                BirthDate = candidate.BirthDate,
                MaritalStatus = candidate.MaritalStatus,
                Gender = candidate.Gender,
                JobTitle = "Cargo não atribuido",
                CurrentFlag = true,
                HireDate = now,
                ModifiedDate = now,
                PayHistories = new List<PayHistory>(),
                DepartmentHistories = new List<DepartmentHistory>(),
                Person = person
            };
        }
        private (SystemUser sysUser, string tempPassword, string role) CreateSystemUserForNewEmployee(
            int beId, string username, JobCandidate candidate)
        {
            var tempPassword = string.IsNullOrWhiteSpace(candidate.PasswordHash)
                ? GenerateTempPassword()
                : candidate.PasswordHash;

            var hashed = HashWithBcrypt(tempPassword);
            var role = "employee";

            var sysUser = new SystemUser
            {
                BusinessEntityID = beId,
                Username = username,
                PasswordHash = hashed,
                Role = role
            };

            return (sysUser, tempPassword, role);
        }
        private async Task<IActionResult> HandleApproveDbUpdateExceptionAsync(
            DbUpdateException ex,
            int jobCandidateId,
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
            CancellationToken ct)
        {
            _logger.LogError(ex, "Erro de atualização ao aprovar candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
            await _appLog.ErrorAsync($"Erro de atualização ao aprovar candidato. JobCandidateId={jobCandidateId}", ex);
            await _db.SaveChangesAsync(ct);

            await transaction.RollbackAsync(ct);
            await _appLog.InfoAsync($"Transação revertida devido a DbUpdateException. JobCandidateId={jobCandidateId}");
            await _db.SaveChangesAsync(ct);

            return Conflict(new { message = "Erro ao aprovar candidato", detail = ex.Message });
        }

        private async Task<IActionResult> HandleApproveUnexpectedErrorAsync(
            Exception ex,
            int jobCandidateId,
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
            CancellationToken ct)
        {
            _logger.LogError(ex, "Erro interno ao aprovar candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
            await _appLog.ErrorAsync($"Erro interno ao aprovar candidato. JobCandidateId={jobCandidateId}", ex);
            await _db.SaveChangesAsync(ct);

            await transaction.RollbackAsync(ct);
            await _appLog.InfoAsync($"Transação revertida devido a erro interno. JobCandidateId={jobCandidateId}");
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro interno ao aprovar candidato",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}