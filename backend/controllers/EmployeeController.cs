using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;

        public EmployeeController(AdventureWorksContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: api/v1/employee
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _db.Employees
                .Include(e => e.PayHistories)
                .Include(e => e.DepartmentHistories)
                    .ThenInclude(dh => dh.Department)
                .Include(e => e.Person)
                .ToListAsync();

            var employeesDto = _mapper.Map<List<EmployeeDto>>(employees);
            return Ok(employeesDto);
        }

        // GET: api/v1/employee/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee = await _db.Employees
                .Include(e => e.PayHistories)
                .Include(e => e.DepartmentHistories)
                    .ThenInclude(dh => dh.Department)
                .Include(e => e.Person)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

            if (employee == null) return NotFound();

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return Ok(employeeDto);
        }

        // POST: api/v1/employee
        [HttpPost]
        public async Task<IActionResult> Create(EmployeeDto employeeDto)
        {
            var employee = _mapper.Map<Employee>(employeeDto);
            employee.HireDate = DateTime.Now;
            employee.ModifiedDate = DateTime.Now;

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            var createdDto = _mapper.Map<EmployeeDto>(employee);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.BusinessEntityID }, createdDto);
        }

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
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.CurrentFlag = false;
            employee.ModifiedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            //await _db.Database.ExecuteSqlRawAsync("EXEC HumanResources.uspDeleteEmployee {0}", id);


            return NoContent();
        }

        // PATCH: api/v1/employee/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] EmployeeDto employeeDto)
        {
            if (id != employeeDto.BusinessEntityID) return BadRequest();

            // Load Employee + Person (via navigation)
            var employee = await _db.Employees
                .Include(e => e.Person)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

            if (employee == null) return NotFound();

            // Strings
            if (!string.IsNullOrEmpty(employeeDto.LoginID)) employee.LoginID = employeeDto.LoginID;
            if (!string.IsNullOrEmpty(employeeDto.JobTitle)) employee.JobTitle = employeeDto.JobTitle;
            if (!string.IsNullOrEmpty(employeeDto.Gender)) employee.Gender = employeeDto.Gender;
            if (!string.IsNullOrEmpty(employeeDto.MaritalStatus)) employee.MaritalStatus = employeeDto.MaritalStatus;
            if (!string.IsNullOrEmpty(employeeDto.NationalIDNumber)) employee.NationalIDNumber = employeeDto.NationalIDNumber;

            // Numerics and booleans: check if different from default
            if (employeeDto.VacationHours != default(short)) employee.VacationHours = employeeDto.VacationHours;
            if (employeeDto.SickLeaveHours != default(short)) employee.SickLeaveHours = employeeDto.SickLeaveHours;
            if (employeeDto.SalariedFlag != default(bool)) employee.SalariedFlag = employeeDto.SalariedFlag;

            // Dates
            if (employeeDto.HireDate != default(DateTime)) employee.HireDate = employeeDto.HireDate;
            if (employeeDto.BirthDate != default(DateTime)) employee.BirthDate = employeeDto.BirthDate;

            employee.ModifiedDate = DateTime.Now;

            // ---Update Person(only if dto.Person present) ---
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

            return Ok(_mapper.Map<EmployeeDto>(employee));
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
        public async Task<IActionResult> ApproveCandidate(int jobCandidateId)
        {
            // (0) Carregar candidato
            var candidate = await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == jobCandidateId);

            // Se não existir, 404
            if (candidate is null)
            {
                return NotFound(new { message = "Candidato não encontrado", jobCandidateId });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                // (A) Criar BusinessEntity para obter o ID
                var be = new BusinessEntity { RowGuid = Guid.NewGuid(), ModifiedDate = now };
                _db.BusinessEntities.Add(be);
                await _db.SaveChangesAsync(); // ID gerado aqui
                var beId = be.BusinessEntityID;

                // (B) Mapear Person
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

                // (C) Gerar username e validar unicidade
                var username = GenerateUsername(person);
                var usernameExists = await _db.SystemUsers.AnyAsync(u => u.Username == username);
                if (usernameExists)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { message = "Username já existe", username });
                }

                // (D) Criar Employee
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

                // (E) Password temporária + hash
                var tempPassword = string.IsNullOrWhiteSpace(candidate.PasswordHash)
                    ? GenerateTempPassword()
                    : candidate.PasswordHash;
                var hashed = HashWithBcrypt(tempPassword);

                // (F) Criar SystemUser
                var role = "employee";

                var sysUser = new SystemUser
                {
                    BusinessEntityID = beId,
                    Username = username,
                    PasswordHash = hashed,
                    Role = role
                };

                _db.SystemUsers.Add(sysUser);

                // (G) Remover o candidato aprovado
                _db.JobCandidates.Remove(candidate);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

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
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = "Erro ao aprovar candidato", detail = ex.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Erro interno ao aprovar candidato", detail = ex.Message });
            }
        }
    }
}