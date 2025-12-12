using AutoMapper;
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
        public async Task<IActionResult> Create(DepartmentHistoryDto dto)
        {
            var history = _mapper.Map<DepartmentHistory>(dto);
            history.ModifiedDate = DateTime.Now;

            _db.DepartmentHistories.Add(history);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new { businessEntityId = history.BusinessEntityID, departmentId = history.DepartmentID, shiftId = history.ShiftID, startDate = history.StartDate },
                _mapper.Map<DepartmentHistoryDto>(history));
        }

        // PUT: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpPut("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        public async Task<IActionResult> Update(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        {
            var history = await _db.DepartmentHistories
                .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
                                        && dh.DepartmentID == departmentId
                                        && dh.ShiftID == shiftId
                                        && dh.StartDate == startDate);

            if (history == null) return NotFound();

            _mapper.Map(dto, history);
            history.ModifiedDate = DateTime.Now;

            _db.Entry(history).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpPatch("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        public async Task<IActionResult> Patch(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        {
            var history = await _db.DepartmentHistories
                .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
                                        && dh.DepartmentID == departmentId
                                        && dh.ShiftID == shiftId
                                        && dh.StartDate == startDate);

            if (history == null) return NotFound();

            if (dto.DepartmentId != default(short)) history.DepartmentID = (short)dto.DepartmentId;
            if (dto.ShiftID != default(byte)) history.ShiftID = dto.ShiftID;
            if (dto.EndDate.HasValue) history.EndDate = dto.EndDate;

            history.ModifiedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(_mapper.Map<DepartmentHistoryDto>(history));
        }
    }
}