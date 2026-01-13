using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
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

        public EmployeeController(AdventureWorksContext db, IMapper mapper, ILogger<EmployeeController> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
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

        private void AddLog(string message)
        {
            _db.Logs.Add(new Log { Message = message, Date = DateTime.UtcNow });
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
            AddLog("Recebida requisição para obter todos os Employees (Role=admin).");

            try
            {
                var employees = await _db.Employees
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories).ThenInclude(dh => dh.Department)
                    .Include(e => e.Person)
                    .ToListAsync(ct);

                _logger.LogInformation("Encontrados {Count} Employees.", employees.Count);
                AddLog($"Encontrados {employees.Count} Employees.");
                await _db.SaveChangesAsync(ct);

                var employeesDto = _mapper.Map<List<EmployeeDto>>(employees);
                return Ok(employeesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter lista de Employees.");
                AddLog("Erro ao obter lista de Employees.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao obter empregados",
                    detail: "Ocorreu um erro ao obter os empregados.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // GET: api/v1/employee/{id}
        // -----------------------
        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id, CancellationToken ct)
        {
            if (!IsSelfAccessAllowed(HttpContext.User, id))
            {
                _logger.LogWarning("Tentativa de acesso não autorizada. ID solicitado={RequestedId}.", id);
                return Forbid();
            }

            _logger.LogInformation("Recebida requisição para obter Employee com ID={Id}.", id);
            AddLog($"Recebida requisição para obter Employee com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var employee = await _db.Employees
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories).ThenInclude(dh => dh.Department)
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == id, ct);

                if (employee is null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={Id}.", id);
                    AddLog($"Employee não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _logger.LogInformation("Employee encontrado para ID={Id}.", id);
                AddLog($"Employee encontrado para ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<EmployeeDto>(employee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter Employee com ID={Id}.", id);
                AddLog($"Erro ao obter Employee com ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao obter empregado",
                    detail: "Ocorreu um erro ao obter o empregado.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // DELETE (soft): api/v1/employee/{id}
        // -----------------------
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para eliminar (soft delete) Employee com ID={Id}.", id);
            AddLog($"Recebida requisição para eliminar (soft delete) Employee com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == id, ct);
                if (employee is null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={Id}.", id);
                    AddLog($"Employee não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _logger.LogInformation("Employee encontrado para ID={Id}. A marcar como inativo…", id);
                AddLog($"Employee encontrado para ID={id}. A marcar como inativo…");

                employee.CurrentFlag = false;
                employee.ModifiedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Employee marcado como inativo com sucesso. ID={Id}.", id);
                AddLog($"Employee marcado como inativo com sucesso. ID={id}.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao atualizar Employee (soft delete) para ID={Id}.", id);
                AddLog($"Erro ao atualizar Employee (soft delete) para ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao atualizar",
                    detail: "Erro ao atualizar o empregado.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar (soft delete) Employee com ID={Id}.", id);
                AddLog($"Erro inesperado ao eliminar (soft delete) Employee com ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao eliminar",
                    detail: "Ocorreu um erro ao eliminar o empregado.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // PATCH: api/v1/employee/{id}
        // -----------------------
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> Patch(int id, [FromBody] EmployeeDto employeeDto, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para atualizar parcialmente Employee com ID={Id}.", id);
            AddLog($"Recebida requisição para atualizar parcialmente Employee com ID={id}.");
            await _db.SaveChangesAsync(ct);

            if (employeeDto is null)
            {
                _logger.LogWarning("DTO ausente no corpo da requisição para Patch de Employee ID={Id}.", id);
                AddLog($"DTO ausente no corpo da requisição para Patch de Employee ID={id}.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Body is required." });
            }

            if (id != employeeDto.BusinessEntityID)
            {
                _logger.LogWarning("ID no path ({PathId}) difere do ID no corpo ({BodyId}).", id, employeeDto.BusinessEntityID);
                AddLog($"ID no path ({id}) difere do ID no corpo ({employeeDto.BusinessEntityID}).");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Path ID must match body BusinessEntityID." });
            }

            if (!IsSelfAccessAllowed(HttpContext.User, id))
                return Forbid();

            try
            {
                var employee = await _db.Employees
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == id, ct);

                if (employee is null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={Id}.", id);
                    AddLog($"Employee não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _logger.LogInformation("Employee encontrado para ID={Id}. A aplicar alterações parciais…", id);
                AddLog($"Employee encontrado para ID={id}. A aplicar alterações parciais…");

                // NOTE: Ideally DTO has nullable properties to indicate intent for PATCH.
                // If your DTO is non-nullable, these guards mimic partial updates.

                if (!string.IsNullOrEmpty(employeeDto.LoginID)) employee.LoginID = employeeDto.LoginID;
                if (!string.IsNullOrEmpty(employeeDto.JobTitle)) employee.JobTitle = employeeDto.JobTitle;
                if (!string.IsNullOrEmpty(employeeDto.Gender)) employee.Gender = employeeDto.Gender;
                if (!string.IsNullOrEmpty(employeeDto.MaritalStatus)) employee.MaritalStatus = employeeDto.MaritalStatus;
                if (!string.IsNullOrEmpty(employeeDto.NationalIDNumber)) employee.NationalIDNumber = employeeDto.NationalIDNumber;

                if (employeeDto.VacationHours != default(short)) employee.VacationHours = employeeDto.VacationHours;
                if (employeeDto.SickLeaveHours != default(short)) employee.SickLeaveHours = employeeDto.SickLeaveHours;
                if (employeeDto.SalariedFlag != default(bool)) employee.SalariedFlag = employeeDto.SalariedFlag;
                if (employeeDto.HireDate != default(DateTime)) employee.HireDate = employeeDto.HireDate;
                if (employeeDto.BirthDate != default(DateTime)) employee.BirthDate = employeeDto.BirthDate;

                employee.ModifiedDate = DateTime.UtcNow;

                if (employee.Person is not null && employeeDto.Person is not null)
                {
                    var p = employee.Person;
                    var pd = employeeDto.Person;

                    if (!string.IsNullOrWhiteSpace(pd.FirstName)) p.FirstName = pd.FirstName;
                    if (!string.IsNullOrWhiteSpace(pd.LastName)) p.LastName = pd.LastName;

                    // allow nulls when explicitly provided
                    p.MiddleName = pd.MiddleName != null && string.IsNullOrWhiteSpace(pd.MiddleName) ? null : pd.MiddleName ?? p.MiddleName;
                    p.Title = pd.Title != null && string.IsNullOrWhiteSpace(pd.Title) ? null : pd.Title ?? p.Title;
                    p.Suffix = pd.Suffix != null && string.IsNullOrWhiteSpace(pd.Suffix) ? null : pd.Suffix ?? p.Suffix;

                    p.ModifiedDate = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Patch aplicado com sucesso para Employee ID={Id}.", id);
                AddLog($"Patch aplicado com sucesso para Employee ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<EmployeeDto>(employee));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao persistir alterações do Patch para Employee ID={Id}.", id);
                AddLog($"Erro ao persistir alterações do Patch para Employee ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao atualizar",
                    detail: "Erro ao atualizar o empregado.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao aplicar Patch para Employee ID={Id}.", id);
                AddLog($"Erro inesperado ao aplicar Patch para Employee ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao atualizar",
                    detail: "Ocorreu um erro ao atualizar o empregado.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // POST: api/v1/employee/approve/{jobCandidateId}
        // -----------------------
        [HttpPost("approve/{jobCandidateId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ApproveCandidate(int jobCandidateId, CancellationToken ct)
        {
            _logger.LogInformation("Iniciando aprovação do candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
            AddLog($"Iniciando aprovação do candidato. JobCandidateId={jobCandidateId}");
            await _db.SaveChangesAsync(ct);

            var candidate = await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == jobCandidateId, ct);

            if (candidate is null)
            {
                _logger.LogWarning("Candidato não encontrado. JobCandidateId={JobCandidateId}", jobCandidateId);
                AddLog($"Candidato não encontrado. JobCandidateId={jobCandidateId}");
                await _db.SaveChangesAsync(ct);
                return NotFound(new { message = "Candidato não encontrado", jobCandidateId });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            AddLog($"Transação iniciada para aprovação. JobCandidateId={jobCandidateId}");
            await _db.SaveChangesAsync(ct);

            try
            {
                // 1) Reativar empregado existente, se aplicável
                var previousEmployee = await _db.Employees
                    .FirstOrDefaultAsync(pe => pe.NationalIDNumber == candidate.NationalIDNumber, ct);

                if (previousEmployee is not null && !previousEmployee.CurrentFlag)
                {
                    _logger.LogInformation("Ativar empregado existente. BusinessEntityID={BusinessEntityID}", previousEmployee.BusinessEntityID);
                    AddLog($"Ativar empregado existente. BusinessEntityID={previousEmployee.BusinessEntityID}");

                    previousEmployee.CurrentFlag = true;
                    previousEmployee.ModifiedDate = DateTime.UtcNow;

                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    _db.JobCandidates.Remove(candidate);
                    await _db.SaveChangesAsync(ct);

                    _logger.LogInformation("Transação concluída. BusinessEntityID={BusinessEntityID}", previousEmployee.BusinessEntityID);
                    AddLog($"Transação concluída. BusinessEntityID={previousEmployee.BusinessEntityID}");
                    await _db.SaveChangesAsync(ct);

                    var dto = _mapper.Map<EmployeeDto>(previousEmployee);
                    return CreatedAtAction(nameof(GetEmployee), new { id = previousEmployee.BusinessEntityID }, dto);
                }

                // 2) Criar BusinessEntity
                var now = DateTime.UtcNow;
                var be = new BusinessEntity { RowGuid = Guid.NewGuid(), ModifiedDate = now };
                _db.BusinessEntities.Add(be);
                await _db.SaveChangesAsync(ct);

                var beId = be.BusinessEntityID;
                _logger.LogInformation("BusinessEntity criado. BusinessEntityID={BusinessEntityID}", beId);
                AddLog($"BusinessEntity criado. BusinessEntityID={beId}");
                await _db.SaveChangesAsync(ct);

                // 3) Criar Person
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

                _logger.LogInformation("Person criado. BusinessEntityID={BusinessEntityID}", beId);
                AddLog($"Person criado. BusinessEntityID={beId}");
                await _db.SaveChangesAsync(ct);

                // 4) Username
                var username = GenerateUsername(person);
                var usernameExists = await _db.SystemUsers.AnyAsync(u => u.Username == username, ct);
                if (usernameExists)
                {
                    _logger.LogWarning("Conflito de username. Username={Username}", username);
                    AddLog($"Conflito de username. Username={username}");
                    await _db.SaveChangesAsync(ct);

                    await transaction.RollbackAsync(ct);
                    AddLog($"Transação revertida por conflito de username. JobCandidateId={jobCandidateId}");
                    await _db.SaveChangesAsync(ct);

                    return Conflict(new { message = "Username já existe", username });
                }

                _logger.LogInformation("Username gerado e validado. Username={Username}", username);
                AddLog($"Username gerado e validado. Username={username}");
                await _db.SaveChangesAsync(ct);

                // 5) Criar Employee
                var employee = new Employee
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
                _db.Employees.Add(employee);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Employee criado. BusinessEntityID={BusinessEntityID}, LoginID={LoginID}", beId, employee.LoginID);
                AddLog($"Employee criado. BusinessEntityID={beId}, LoginID={employee.LoginID}");
                await _db.SaveChangesAsync(ct);

                // 6) Criar SystemUser e remover candidato
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

                _db.SystemUsers.Add(sysUser);
                _db.JobCandidates.Remove(candidate);
                await _db.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("SystemUser criado e candidato removido. SystemUserId={SystemUserId}, Username={Username}", sysUser.SystemUserId, sysUser.Username);
                AddLog($"SystemUser criado e candidato removido. SystemUserId={sysUser.SystemUserId}, Username={sysUser.Username}");
                AddLog($"Transação concluída com sucesso. BusinessEntityID={beId}");
                await _db.SaveChangesAsync(ct);

                // DEV-only: tempPassword exposto na resposta (atenção em produção)
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
                _logger.LogError(ex, "Erro de atualização ao aprovar candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
                AddLog($"Erro de atualização ao aprovar candidato. JobCandidateId={jobCandidateId}");
                await _db.SaveChangesAsync(ct);

                await transaction.RollbackAsync(ct);
                AddLog($"Transação revertida devido a DbUpdateException. JobCandidateId={jobCandidateId}");
                await _db.SaveChangesAsync(ct);

                return Conflict(new { message = "Erro ao aprovar candidato", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao aprovar candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
                AddLog($"Erro interno ao aprovar candidato. JobCandidateId={jobCandidateId}");
                await _db.SaveChangesAsync(ct);

                await transaction.RollbackAsync(ct);
                AddLog($"Transação revertida devido a erro interno. JobCandidateId={jobCandidateId}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro interno ao aprovar candidato",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}