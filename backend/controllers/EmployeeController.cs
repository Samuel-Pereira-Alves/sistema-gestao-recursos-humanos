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
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, EmployeeDto employeeDto)
        {
            if (id != employeeDto.BusinessEntityID) return BadRequest();

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == id);
            if (employee == null) return NotFound();

            _mapper.Map(employeeDto, employee);
            employee.ModifiedDate = DateTime.Now;

            _db.Entry(employee).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/v1/employee/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();

            //await _db.Database.ExecuteSqlRawAsync("EXEC HumanResources.uspDeleteEmployee {0}", id);


            return NoContent();
        }

        // PATCH: api/v1/employee/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, EmployeeDto employeeDto)
        {
            if (id != employeeDto.BusinessEntityID) return BadRequest();

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == id);
            if (employee == null) return NotFound();

            // Strings
            if (!string.IsNullOrEmpty(employeeDto.LoginID)) employee.LoginID = employeeDto.LoginID;
            if (!string.IsNullOrEmpty(employeeDto.JobTitle)) employee.JobTitle = employeeDto.JobTitle;

            // Numerics and booleans: check if different from default
            if (employeeDto.VacationHours != default(short)) employee.VacationHours = employeeDto.VacationHours;
            if (employeeDto.SickLeaveHours != default(short)) employee.SickLeaveHours = employeeDto.SickLeaveHours;
            if (employeeDto.SalariedFlag != default(bool)) employee.SalariedFlag = employeeDto.SalariedFlag;

            // Dates
            if (employeeDto.HireDate != default(DateTime)) employee.HireDate = employeeDto.HireDate;
            if (employeeDto.BirthDate != default(DateTime)) employee.BirthDate = employeeDto.BirthDate;

            employee.ModifiedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(_mapper.Map<EmployeeDto>(employee));
        }


        // Extra: Histórico de pagamentos
        [HttpGet("{id}/payhistory")]
        public async Task<IActionResult> GetPayHistory(int id)
        {
            var employee = await _db.Employees
                .Include(e => e.PayHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

            if (employee == null) return NotFound();

            var payHistoryDto = _mapper.Map<List<PayHistoryDto>>(employee.PayHistories);
            return Ok(payHistoryDto);
        }

        // Extra: Histórico de departamentos
        [HttpGet("{id}/departmenthistory")]
        public async Task<IActionResult> GetDepartmentHistory(int id)
        {
            var employee = await _db.Employees
                .Include(e => e.DepartmentHistories)
                .FirstOrDefaultAsync(e => e.BusinessEntityID == id);

            if (employee == null) return NotFound();

            var deptHistoryDto = _mapper.Map<List<DepartmentHistoryDto>>(employee.DepartmentHistories);
            return Ok(deptHistoryDto);
        }

        //Secção de Aprovação de candidatura
        private static string GenerateUsername(Employee employee)
        {
            if (!string.IsNullOrWhiteSpace(employee.LoginID))
                return employee.LoginID.ToLowerInvariant();

            if (employee.Person != null &&
                !string.IsNullOrWhiteSpace(employee.Person.FirstName) &&
                !string.IsNullOrWhiteSpace(employee.Person.LastName))
            {
                return $"{employee.Person.FirstName}.{employee.Person.LastName}@emailnadainventado.com".ToLowerInvariant();
            }

            return $"emp{employee.BusinessEntityID}";
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

            // Garantir pelo menos um de cada tipo (opcional)
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





        [HttpPost("approve")]
        public async Task<IActionResult> ApproveCandidate([FromBody] ApproveCandidateDTO request)
        {
            if (request?.Employee == null)
                return BadRequest(new { message = "Employee é obrigatório" });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.Now;

                // (A) Criar BusinessEntity para obter o ID
                var be = new BusinessEntity { RowGuid = Guid.NewGuid(), ModifiedDate = now };
                _db.BusinessEntities.Add(be);
                await _db.SaveChangesAsync();               // ID gerado aqui
                var beId = be.BusinessEntityID;

                // (B) Mapear Person (a partir do DTO)
                var person = new Person
                {
                    BusinessEntityID = beId,
                    FirstName = request.Employee.Person?.FirstName ?? "",
                    LastName = request.Employee.Person?.LastName ?? "",
                    MiddleName = request.Employee.Person?.MiddleName,
                    Title = request.Employee.Person?.Title,
                    Suffix = request.Employee.Person?.Suffix,
                    EmailPromotion = 0,
                    ModifiedDate = now,
                    PersonType = "EM"
                };
                _db.Persons.Add(person);
                await _db.SaveChangesAsync();

                // (C) Mapear Employee (usar o mesmo BusinessEntityID)
                var employee = _mapper.Map<Employee>(request.Employee);
                employee.BusinessEntityID = beId;
                employee.HireDate = employee.HireDate.Year < 1753 ? now : employee.HireDate;  // garantir range válido por causa das constrains do SQL
                employee.ModifiedDate = now;
                employee.Person = person; // ligar navegação

                _db.Employees.Add(employee);
                await _db.SaveChangesAsync();

                // (D) Username
                var username = string.IsNullOrWhiteSpace(request.Username)
                    ? GenerateUsername(employee)
                    : request.Username.Trim().ToLowerInvariant();

                var usernameExists = await _db.SystemUsers.AnyAsync(u => u.Username == username);
                if (usernameExists)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { message = "Username já existe", username });
                }

                // (E) Password temporária + hash
                var tempPassword = string.IsNullOrWhiteSpace(request.TempPassword)
                    ? GenerateTempPassword()
                    : request.TempPassword!;
                var hashed = HashWithBcrypt(tempPassword);

                // (F) Criar SystemUser
                var role = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role!.Trim();

                var sysUser = new SystemUser
                {
                    BusinessEntityID = beId,
                    Username = username,
                    PasswordHash = hashed,
                    Role = role
                };

                _db.SystemUsers.Add(sysUser);
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