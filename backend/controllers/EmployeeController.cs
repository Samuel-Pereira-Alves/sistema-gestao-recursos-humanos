using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/v1/employee
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Recebida requisição para obter todos os Employees (Role=admin).");

            try
            {
                var employees = await _db.Employees
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories)
                        .ThenInclude(dh => dh.Department)
                    .Include(e => e.Person)
                    .ToListAsync();

                _logger.LogInformation("Encontrados {Count} Employees.", employees.Count);

                var employeesDto = _mapper.Map<List<EmployeeDto>>(employees);
                return Ok(employeesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter lista de Employees.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter os empregados.");
            }
        }

        // GET: api/v1/employee/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            _logger.LogInformation("Recebida requisição para obter Employee com ID={ID}. Roles permitidas: admin, employee.", id);

            try
            {
                var employee = await _db.Employees
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories)
                        .ThenInclude(dh => dh.Department)
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("Employee encontrado para ID={ID}.", id);

                var employeeDto = _mapper.Map<EmployeeDto>(employee);
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter Employee com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter o empregado.");
            }
        }



        // POST: api/v1/employee
        // [HttpPost]
        // public async Task<IActionResult> Create(EmployeeDto employeeDto)
        // {
        //     var employee = _mapper.Map<Employee>(employeeDto);
        //     employee.HireDate = DateTime.Now;
        //     employee.ModifiedDate = DateTime.Now;

        //     _db.Employees.Add(employee);
        //     await _db.SaveChangesAsync();

        //     var createdDto = _mapper.Map<EmployeeDto>(employee);
        //     return CreatedAtAction(nameof(GetEmployee), new { id = employee.BusinessEntityID }, createdDto);
        // }

        // PUT: api/v1/employee/{id}
        // [HttpPut("{id}")]
        // public async Task<IActionResult> Update(int id, EmployeeDto employeeDto)
        // {
        //     if (id != employeeDto.BusinessEntityID) return BadRequest();

        //     var employee = await _db.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == id);
        //     if (employee == null) return NotFound();

        //     _mapper.Map(employeeDto, employee);
        //     employee.ModifiedDate = DateTime.Now;

        //     _db.Entry(employee).State = EntityState.Modified;
        //     await _db.SaveChangesAsync();

        //     return NoContent();
        // }

        // DELETE: api/v1/employee/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Recebida requisição para eliminar (soft delete) Employee com ID={ID}.", id);
            try
            {
                var employee = await _db.Employees.FindAsync(id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("Employee encontrado para ID={ID}. A marcar como inativo...", id);

                employee.CurrentFlag = false;
                employee.ModifiedDate = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Employee marcado como inativo com sucesso. ID={ID}.", id);

                // await _db.Database.ExecuteSqlRawAsync("EXEC HumanResources.uspDeleteEmployee {0}", id);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao atualizar Employee (soft delete) para ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar o empregado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar (soft delete) Employee com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar o empregado.");
            }
        }


        // PATCH: api/v1/employee/{id}

        [HttpPatch("{id}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> Patch(int id, [FromBody] EmployeeDto employeeDto)
        {
            _logger.LogInformation("Recebida requisição para atualizar parcialmente Employee com ID={ID}.", id);

            // Validação básica de ID e DTO
            if (employeeDto is null)
            {
                _logger.LogWarning("DTO ausente no corpo da requisição para Patch de Employee ID={ID}.", id);
                return BadRequest("Body is required.");
            }
            if (id != employeeDto.BusinessEntityID)
            {
                _logger.LogWarning("ID no path ({PathId}) difere do ID no corpo ({BodyId}).", id, employeeDto.BusinessEntityID);
                return BadRequest("Path ID must match body BusinessEntityID.");
            }

            try
            {
                // Carregar Employee + Person
                var employee = await _db.Employees
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee não encontrado para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("Employee encontrado para ID={ID}. A aplicar alterações parciais...", id);

                // Strings
                if (!string.IsNullOrEmpty(employeeDto.LoginID)) employee.LoginID = employeeDto.LoginID;
                if (!string.IsNullOrEmpty(employeeDto.JobTitle)) employee.JobTitle = employeeDto.JobTitle;
                if (!string.IsNullOrEmpty(employeeDto.Gender)) employee.Gender = employeeDto.Gender;
                if (!string.IsNullOrEmpty(employeeDto.MaritalStatus)) employee.MaritalStatus = employeeDto.MaritalStatus;
                if (!string.IsNullOrEmpty(employeeDto.NationalIDNumber)) employee.NationalIDNumber = employeeDto.NationalIDNumber;

                // Numéricos e booleanos (apenas se diferentes do default)
                if (employeeDto.VacationHours != default(short)) employee.VacationHours = employeeDto.VacationHours;
                if (employeeDto.SickLeaveHours != default(short)) employee.SickLeaveHours = employeeDto.SickLeaveHours;
                if (employeeDto.SalariedFlag != default(bool)) employee.SalariedFlag = employeeDto.SalariedFlag;

                // Datas
                if (employeeDto.HireDate != default(DateTime)) employee.HireDate = employeeDto.HireDate;
                if (employeeDto.BirthDate != default(DateTime)) employee.BirthDate = employeeDto.BirthDate;

                employee.ModifiedDate = DateTime.Now;

                // Atualizar Person (quando presente)
                if (employee.Person is not null && employeeDto.Person is not null)
                {
                    var p = employee.Person;
                    var pd = employeeDto.Person;

                    if (!string.IsNullOrWhiteSpace(pd.FirstName)) p.FirstName = pd.FirstName;
                    if (!string.IsNullOrWhiteSpace(pd.LastName)) p.LastName = pd.LastName;

                    if (pd.MiddleName != null) p.MiddleName = string.IsNullOrWhiteSpace(pd.MiddleName) ? null : pd.MiddleName;
                    if (pd.Title != null) p.Title = string.IsNullOrWhiteSpace(pd.Title) ? null : pd.Title;
                    if (pd.Suffix != null) p.Suffix = string.IsNullOrWhiteSpace(pd.Suffix) ? null : pd.Suffix;

                    p.ModifiedDate = DateTime.Now;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Patch aplicado com sucesso para Employee ID={ID}.", id);

                return Ok(_mapper.Map<EmployeeDto>(employee));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao persistir alterações do Patch para Employee ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar o empregado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao aplicar Patch para Employee ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao atualizar o empregado.");
            }
        }

        //Secção de Aprovação de candidatura
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
            // Password forte com maiúsculas, minúsculas, dígitos e símbolos
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
            {
                chars[i] = all[bytes[i] % all.Length];
            }

            // Garantir pelo menos um de cada tipo
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = lower[bytes[1] % lower.Length];
            chars[2] = digits[bytes[2] % digits.Length];
            chars[3] = symbols[bytes[3] % symbols.Length];

            return new string(chars);
        }

        private static string HashWithBcrypt(string plain)
        {
            return BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 11);
        }


        [HttpPost("approve/{jobCandidateId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ApproveCandidate(int jobCandidateId)
        {
            _logger.LogInformation("Iniciando aprovação do candidato. JobCandidateId={JobCandidateId}", jobCandidateId);

            var candidate = await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == jobCandidateId);

            if (candidate is null)
            {
                _logger.LogWarning("Candidato não encontrado. JobCandidateId={JobCandidateId}", jobCandidateId);
                return NotFound(new { message = "Candidato não encontrado", jobCandidateId });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            _logger.LogInformation("Transação iniciada para aprovação. JobCandidateId={JobCandidateId}", jobCandidateId);

            try
            {
                var previousEmployee = await _db.Employees.FirstOrDefaultAsync(
                    pe => pe.NationalIDNumber == candidate.NationalIDNumber);

                if (previousEmployee != null && !previousEmployee.CurrentFlag)
                {
                    _logger.LogInformation("Tornar empregado empregado existente. BusinessEntityID={BusinessEntityID}", previousEmployee.BusinessEntityID);

                    previousEmployee.CurrentFlag = true;
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Transação concluída. BusinessEntityID={BusinessEntityID}", previousEmployee.BusinessEntityID);

                    return CreatedAtAction(nameof(GetEmployee),
                        new { id = previousEmployee.BusinessEntityID },
                        previousEmployee
                    );
                }
                else
                {
                    var now = DateTime.UtcNow;

                    var be = new BusinessEntity { RowGuid = Guid.NewGuid(), ModifiedDate = now };
                    _db.BusinessEntities.Add(be);
                    await _db.SaveChangesAsync();
                    var beId = be.BusinessEntityID;

                    _logger.LogInformation("BusinessEntity criado. BusinessEntityID={BusinessEntityID}", beId);

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
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Person criado. BusinessEntityID={BusinessEntityID}", beId);

                    var username = GenerateUsername(person);
                    var usernameExists = await _db.SystemUsers.AnyAsync(u => u.Username == username);
                    if (usernameExists)
                    {
                        _logger.LogWarning("Conflito de username. Username={Username}", username);
                        await transaction.RollbackAsync();
                        _logger.LogInformation("Transação revertida por conflito de username. JobCandidateId={JobCandidateId}", jobCandidateId);
                        return Conflict(new { message = "Username já existe", username });
                    }

                    _logger.LogInformation("Username gerado e validado. Username={Username}", username);

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
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Employee criado. BusinessEntityID={BusinessEntityID}, LoginID={LoginID}", beId, employee.LoginID);

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

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("SystemUser criado e candidato removido. SystemUserId={SystemUserId}, Username={Username}", sysUser.SystemUserId, sysUser.Username);
                    _logger.LogInformation("Transação concluída com sucesso. BusinessEntityID={BusinessEntityID}", beId);

                    return CreatedAtAction(nameof(GetEmployee),
                        new { id = beId },
                        new
                        {
                            employeeId = beId,
                            systemUserId = sysUser.SystemUserId,
                            username,
                            role,
                            tempPassword // DEV only
                        });
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de atualização ao aprovar candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
                await transaction.RollbackAsync();
                _logger.LogInformation("Transação revertida devido a DbUpdateException. JobCandidateId={JobCandidateId}", jobCandidateId);
                return Conflict(new { message = "Erro ao aprovar candidato", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao aprovar candidato. JobCandidateId={JobCandidateId}", jobCandidateId);
                await transaction.RollbackAsync();
                _logger.LogInformation("Transação revertida devido a erro interno. JobCandidateId={JobCandidateId}", jobCandidateId);
                return StatusCode(500, new { message = "Erro interno ao aprovar candidato", detail = ex.Message });
            }
        }
    }
}