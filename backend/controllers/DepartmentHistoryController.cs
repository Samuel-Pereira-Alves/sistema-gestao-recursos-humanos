using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DepartmentHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;

        public DepartmentHistoryController(AdventureWorksContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: api/v1/departmenthistory
        [HttpGet]
        [Authorize(Roles ="admin, employee")]
        public async Task<IActionResult> GetAll()
        {
            var histories = await _db.DepartmentHistories
                .Include(dh => dh.Department)
                .ToListAsync();

            var dto = _mapper.Map<List<DepartmentHistoryDto>>(histories);
            return Ok(dto);
        }

        // GET: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpGet("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Get(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            var history = await _db.DepartmentHistories
                .Include(dh => dh.Department)
                .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
                                        && dh.DepartmentID == departmentId
                                        && dh.ShiftID == shiftId
                                        && dh.StartDate == startDate);

            if (history == null) return NotFound();
            var dto = _mapper.Map<DepartmentHistoryDto>(history);
            return Ok(dto);
        }

        // POST: api/v1/departmenthistory
        [HttpPost]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Create(DepartmentHistoryDto dto)
        {
            // 1) Validar Employee (FK)
            var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID);
            if (!employeeExists)
                return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });

            // 2) Validar Department (FK)
            if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });

            var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
            if (!deptExists)
                return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

            // 3) Validar StartDate no range de SQL Server datetime
            var minSqlDate = new DateTime(1753, 1, 1);
            if (dto.StartDate < minSqlDate)
                return BadRequest(new { message = "StartDate inválida para SQL Server datetime", dto.StartDate });

            // 4) Evitar duplicado de PK composta (BusinessEntityID, DepartmentID, ShiftID, StartDate)
            var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                dh.BusinessEntityID == dto.BusinessEntityID &&
                dh.DepartmentID == (short)dto.DepartmentId &&
                dh.ShiftID == dto.ShiftID &&
                dh.StartDate == dto.StartDate);

            if (exists)
                return Conflict(new
                {
                    message = "Registo de DepartmentHistory já existe",
                    businessEntityId = dto.BusinessEntityID,
                    departmentId = dto.DepartmentId,
                    shiftId = dto.ShiftID,
                    startDate = dto.StartDate
                });

            var lastMovements = await _db.DepartmentHistories
                                        .Where(dh => dh.BusinessEntityID == dto.BusinessEntityID &&
                                        dh.EndDate == null
                                        ).ToListAsync();

            foreach (var movement in lastMovements)
            {
                movement.EndDate = DateTime.Now;
                movement.ModifiedDate = DateTime.Now;
            }
            // 5) Mapear e inserir
            var history = _mapper.Map<DepartmentHistory>(dto);
            history.ModifiedDate = DateTime.Now;

            _db.DepartmentHistories.Add(history);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new
                {
                    businessEntityId = history.BusinessEntityID,
                    departmentId = history.DepartmentID,
                    shiftId = history.ShiftID,
                    startDate = history.StartDate
                },
                _mapper.Map<DepartmentHistoryDto>(history));
        }

        // PUT: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        // [HttpPut("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        // public async Task<IActionResult> Update(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        // {
        //     var history = await _db.DepartmentHistories
        //         .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
        //                                 && dh.DepartmentID == departmentId
        //                                 && dh.ShiftID == shiftId
        //                                 && dh.StartDate == startDate);

        //     if (history == null) return NotFound();

        //     _mapper.Map(dto, history);
        //     history.ModifiedDate = DateTime.Now;

        //     _db.Entry(history).State = EntityState.Modified;
        //     await _db.SaveChangesAsync();

        //     return NoContent();
        // }

        // PATCH: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpPatch("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Patch(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        {
            var history = await _db.DepartmentHistories
                .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
                                        && dh.DepartmentID == departmentId
                                        && dh.ShiftID == shiftId
                                        && dh.StartDate == startDate);

            if (history == null) return NotFound();

            // if (dto.DepartmentId != default(short)) history.DepartmentID = (short)dto.DepartmentId;
            // if (dto.ShiftID != default(byte)) history.ShiftID = dto.ShiftID;
            if (dto.EndDate.HasValue) history.EndDate = dto.EndDate;

            history.ModifiedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(_mapper.Map<DepartmentHistoryDto>(history));
        }

        [HttpPost("{businessEntityId}")]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> CreateByEmployee(
            int businessEntityId,
            [FromBody] DepartmentHistoryDto dto)
        {
            // 1) Validar existência do Employee (FK)
            var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == businessEntityId);
            if (!employeeExists)
                return NotFound(new { message = "Employee não encontrado", businessEntityId });

            // 2) Validar existência do Department (FK) — opcional mas recomendado
            if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                return BadRequest(new { message = "DepartmentId fora do intervalo de short", dto.DepartmentId });

            var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
            if (!deptExists)
                return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

            // 3) Garantir StartDate dentro do range de datetime (>= 1753-01-01)
            var minSqlDate = new DateTime(1753, 1, 1);
            if (dto.StartDate < minSqlDate)
                return BadRequest(new { message = "StartDate inválida para SQL Server datetime", dto.StartDate });

            // 4) Mapear DTO → Modelo e fixar BusinessEntityID
            var history = _mapper.Map<DepartmentHistory>(dto);
            history.BusinessEntityID = businessEntityId;
            history.DepartmentID = (short)dto.DepartmentId;
            history.ModifiedDate = DateTime.Now;

            // 5) Evitar duplicado de PK composta (BusinessEntityID, DepartmentID, ShiftID, StartDate)
            var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                dh.BusinessEntityID == history.BusinessEntityID &&
                dh.DepartmentID == history.DepartmentID &&
                dh.ShiftID == history.ShiftID &&
                dh.StartDate == history.StartDate);

            if (exists)
                return Conflict(new
                {
                    message = "Registo de DepartmentHistory já existe",
                    businessEntityId = history.BusinessEntityID,
                    departmentId = history.DepartmentID,
                    shiftId = history.ShiftID,
                    startDate = history.StartDate
                });

            // 6) Inserir
            _db.DepartmentHistories.Add(history);
            await _db.SaveChangesAsync();

            // 7) Responder com Location para GET composto
            return CreatedAtAction(nameof(Get),
                new
                {
                    businessEntityId = history.BusinessEntityID,
                    departmentId = history.DepartmentID,
                    shiftId = history.ShiftID,
                    startDate = history.StartDate.ToString("o") // ISO 8601 para segurança
                },
                _mapper.Map<DepartmentHistoryDto>(history));
        }

        [HttpDelete("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Delete(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            var history = await _db.DepartmentHistories
                .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
                                        && dh.DepartmentID == departmentId
                                        && dh.ShiftID == shiftId
                                        && dh.StartDate == startDate);

            if (history == null) return NotFound();

            _db.DepartmentHistories.Remove(history);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}