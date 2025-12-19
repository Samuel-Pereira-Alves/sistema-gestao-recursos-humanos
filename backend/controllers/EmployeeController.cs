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
            // 1) Pedido recebido
            string msg1 = "Recebida requisição para obter todos os Employees (Role=admin).";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Consulta com includes
                var employees = await _db.Employees
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories)
                        .ThenInclude(dh => dh.Department)
                    .Include(e => e.Person)
                    .ToListAsync();

                // 3) Resultado
                string msg2 = $"Encontrados {employees.Count} Employees.";
                _logger.LogInformation(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 4) Mapeamento e retorno
                var employeesDto = _mapper.Map<List<EmployeeDto>>(employees);
                return Ok(employeesDto);
            }
            catch (Exception ex)
            {
                // 5) Erro
                string msg3 = "Erro ao obter lista de Employees.";
                _logger.LogError(ex, msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter os empregados.");
            }
        }

        // GET: api/v1/employee/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para obter Employee com ID={id}. Roles permitidas: admin, employee.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Consulta com includes
                var employee = await _db.Employees
                    .Include(e => e.PayHistories)
                    .Include(e => e.DepartmentHistories)
                        .ThenInclude(dh => dh.Department)
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

                // 3) Não encontrado
                if (employee == null)
                {
                    string msg2 = $"Employee não encontrado para ID={id}.";
                    _logger.LogWarning(msg2);
                    _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // 4) Encontrado
                string msg3 = $"Employee encontrado para ID={id}.";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 5) Mapeamento e retorno
                var employeeDto = _mapper.Map<EmployeeDto>(employee);
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                // 6) Erro
                string msg4 = $"Erro ao obter Employee com ID={id}.";
                _logger.LogError(ex, msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

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
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para eliminar (soft delete) Employee com ID={id}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Procurar o empregado
                var employee = await _db.Employees.FindAsync(id);

                // 3) Não encontrado
                if (employee == null)
                {
                    string msg2 = $"Employee não encontrado para ID={id}.";
                    _logger.LogWarning(msg2);
                    _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // 4) Encontrado — a marcar como inativo
                string msg3 = $"Employee encontrado para ID={id}. A marcar como inativo...";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                employee.CurrentFlag = false;
                employee.ModifiedDate = DateTime.Now;

                await _db.SaveChangesAsync();

                // 5) Sucesso
                string msg4 = $"Employee marcado como inativo com sucesso. ID={id}.";
                _logger.LogInformation(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // Caso futuro: execução de SP (comentada)
                // await _db.Database.ExecuteSqlRawAsync("EXEC HumanResources.uspDeleteEmployee {0}", id);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                string msg5 = $"Erro ao atualizar Employee (soft delete) para ID={id}.";
                _logger.LogError(dbEx, msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar o empregado.");
            }
            catch (Exception ex)
            {
                string msg6 = $"Erro inesperado ao eliminar (soft delete) Employee com ID={id}.";
                _logger.LogError(ex, msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar o empregado.");
            }
        }

        // PATCH: api/v1/employee/{id}
        [HttpPatch("{id}")]
        [Authorize(Roles = "admin, employee")]

        [HttpPatch("employees/{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] EmployeeDto employeeDto)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para atualizar parcialmente Employee com ID={id}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            // 2) Validações básicas
            if (employeeDto is null)
            {
                string msg2 = $"DTO ausente no corpo da requisição para Patch de Employee ID={id}.";
                _logger.LogWarning(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return BadRequest("Body is required.");
            }

            if (id != employeeDto.BusinessEntityID)
            {
                string msg3 = $"ID no path ({id}) difere do ID no corpo ({employeeDto.BusinessEntityID}).";
                _logger.LogWarning("ID no path ({PathId}) difere do ID no corpo ({BodyId}).", id, employeeDto.BusinessEntityID);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return BadRequest("Path ID must match body BusinessEntityID.");
            }

            try
            {
                // 3) Carregar Employee + Person
                var employee = await _db.Employees
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

                if (employee == null)
                {
                    string msg4 = $"Employee não encontrado para ID={id}.";
                    _logger.LogWarning(msg4);
                    _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                string msg5 = $"Employee encontrado para ID={id}. A aplicar alterações parciais…";
                _logger.LogInformation(msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 4) Aplicar alterações (apenas campos presentes/válidos)
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

                string msg6 = $"Patch aplicado com sucesso para Employee ID={id}.";
                _logger.LogInformation(msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Ok(_mapper.Map<EmployeeDto>(employee));
            }
            catch (DbUpdateException dbEx)
            {
                string msg7 = $"Erro ao persistir alterações do Patch para Employee ID={id}.";
                _logger.LogError(dbEx, msg7);
                _db.Logs.Add(new Log { Message = msg7, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar o empregado.");
            }
            catch (Exception ex)
            {
                string msg8 = $"Erro inesperado ao aplicar Patch para Employee ID={id}.";
                _logger.LogError(ex, msg8);
                _db.Logs.Add(new Log { Message = msg8, Date = DateTime.Now });
                await _db.SaveChangesAsync();

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
            // 1) Pedido recebido
            string m0 = $"Iniciando aprovação do candidato. JobCandidateId={jobCandidateId}";
            _logger.LogInformation(m0);
            _db.Logs.Add(new Log { Message = m0, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            // 2) Procurar candidato
            var candidate = await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == jobCandidateId);

            if (candidate is null)
            {
                string m1 = $"Candidato não encontrado. JobCandidateId={jobCandidateId}";
                _logger.LogWarning(m1);
                _db.Logs.Add(new Log { Message = m1, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NotFound(new { message = "Candidato não encontrado", jobCandidateId });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            string m2 = $"Transação iniciada para aprovação. JobCandidateId={jobCandidateId}";
            _logger.LogInformation(m2);
            _db.Logs.Add(new Log { Message = m2, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 3) Reativar empregado existente, se aplicável
                var previousEmployee = await _db.Employees.FirstOrDefaultAsync(
                    pe => pe.NationalIDNumber == candidate.NationalIDNumber);

                if (previousEmployee != null && !previousEmployee.CurrentFlag)
                {
                    string m3 = $"Ativar empregado existente. BusinessEntityID={previousEmployee.BusinessEntityID}";
                    _logger.LogInformation(m3);
                    _db.Logs.Add(new Log { Message = m3, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    previousEmployee.CurrentFlag = true;
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    string m4 = $"Transação concluída. BusinessEntityID={previousEmployee.BusinessEntityID}";
                    _logger.LogInformation(m4);
                    _db.Logs.Add(new Log { Message = m4, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return CreatedAtAction(nameof(GetEmployee),
                        new { id = previousEmployee.BusinessEntityID },
                        previousEmployee
                    );
                }
                else
                {
                    // 4) Criar novo BusinessEntity
                    var now = DateTime.UtcNow;

                    var be = new BusinessEntity { RowGuid = Guid.NewGuid(), ModifiedDate = now };
                    _db.BusinessEntities.Add(be);
                    await _db.SaveChangesAsync();
                    var beId = be.BusinessEntityID;

                    string m5 = $"BusinessEntity criado. BusinessEntityID={beId}";
                    _logger.LogInformation(m5);
                    _db.Logs.Add(new Log { Message = m5, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    // 5) Criar Person
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

                    string m6 = $"Person criado. BusinessEntityID={beId}";
                    _logger.LogInformation(m6);
                    _db.Logs.Add(new Log { Message = m6, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    // 6) Gerar e validar username
                    var username = GenerateUsername(person);
                    var usernameExists = await _db.SystemUsers.AnyAsync(u => u.Username == username);
                    if (usernameExists)
                    {
                        string m7 = $"Conflito de username. Username={username}";
                        _logger.LogWarning(m7);
                        _db.Logs.Add(new Log { Message = m7, Date = DateTime.Now });
                        await _db.SaveChangesAsync();

                        await transaction.RollbackAsync();

                        string m8 = $"Transação revertida por conflito de username. JobCandidateId={jobCandidateId}";
                        _logger.LogInformation(m8);
                        _db.Logs.Add(new Log { Message = m8, Date = DateTime.Now });
                        await _db.SaveChangesAsync();

                        return Conflict(new { message = "Username já existe", username });
                    }

                    string m9 = $"Username gerado e validado. Username={username}";
                    _logger.LogInformation(m9);
                    _db.Logs.Add(new Log { Message = m9, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    // 7) Criar Employee
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

                    string m10 = $"Employee criado. BusinessEntityID={beId}, LoginID={employee.LoginID}";
                    _logger.LogInformation(m10);
                    _db.Logs.Add(new Log { Message = m10, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    // 8) Criar SystemUser e remover candidato
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

                    string m11 = $"SystemUser criado e candidato removido. SystemUserId={sysUser.SystemUserId}, Username={sysUser.Username}";
                    _logger.LogInformation(m11);
                    _db.Logs.Add(new Log { Message = m11, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    string m12 = $"Transação concluída com sucesso. BusinessEntityID={beId}";
                    _logger.LogInformation("Transação concluída com sucesso. BusinessEntityID={BusinessEntityID}", beId);
                    _db.Logs.Add(new Log { Message = m12, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    // DEV-only: tempPassword vai na resposta mas não fica nos logs
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
                string mErr1 = $"Erro de atualização ao aprovar candidato. JobCandidateId={jobCandidateId}";
                _logger.LogError(ex, mErr1);
                _db.Logs.Add(new Log { Message = mErr1, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                await transaction.RollbackAsync();

                string mRol1 = $"Transação revertida devido a DbUpdateException. JobCandidateId={jobCandidateId}";
                _logger.LogInformation(mRol1);
                _db.Logs.Add(new Log { Message = mRol1, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Conflict(new { message = "Erro ao aprovar candidato", detail = ex.Message });
            }
            catch (Exception ex)
            {
                string mErr2 = $"Erro interno ao aprovar candidato. JobCandidateId={jobCandidateId}";
                _logger.LogError(ex, mErr2);
                _db.Logs.Add(new Log { Message = mErr2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                await transaction.RollbackAsync();

                string mRol2 = $"Transação revertida devido a erro interno. JobCandidateId={jobCandidateId}";
                _logger.LogInformation(mRol2);
                _db.Logs.Add(new Log { Message = mRol2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(500, new { message = "Erro interno ao aprovar candidato", detail = ex.Message });
            }
        }
    }
}