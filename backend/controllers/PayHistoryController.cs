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
    public class PayHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;

        public PayHistoryController(AdventureWorksContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: api/v1/payhistory
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var histories = await _db.PayHistories.ToListAsync();
            var dto = _mapper.Map<List<PayHistoryDto>>(histories);
            return Ok(dto);
        }

        // GET: api/v1/payhistory/{businessEntityId}
        [HttpGet("{businessEntityId}")]
        public async Task<IActionResult> GetAllByEmployee(int businessEntityId)
        {
            var histories = await _db.PayHistories
                .Where(ph => ph.BusinessEntityID == businessEntityId)
                .OrderByDescending(ph => ph.RateChangeDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<PayHistoryDto>>(histories);
            return Ok(dtos);
        }


        // GET: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpGet("{businessEntityId}/{rateChangeDate}")]
        public async Task<IActionResult> Get(int businessEntityId, DateTime rateChangeDate)
        {
            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                        && ph.RateChangeDate == rateChangeDate);

            if (history == null) return NotFound();

            var dto = _mapper.Map<PayHistoryDto>(history);
            return Ok(dto);
        }

        // POST: api/v1/payhistory
        [HttpPost]
        public async Task<IActionResult> Create(PayHistoryDto dto)
        {
            var history = _mapper.Map<PayHistory>(dto);
            history.ModifiedDate = DateTime.Now;

            _db.PayHistories.Add(history);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new { businessEntityId = history.BusinessEntityID, rateChangeDate = history.RateChangeDate },
                _mapper.Map<PayHistoryDto>(history));
        }

        // PUT: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpPut("{businessEntityId}/{rateChangeDate}")]
        public async Task<IActionResult> Update(int businessEntityId, DateTime rateChangeDate, PayHistoryDto dto)
        {
            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                        && ph.RateChangeDate == rateChangeDate);

            if (history == null) return NotFound();

            _mapper.Map(dto, history);
            history.ModifiedDate = DateTime.Now;

            _db.Entry(history).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpPatch("{businessEntityId}/{rateChangeDate}")]
        public async Task<IActionResult> Patch(int businessEntityId, DateTime rateChangeDate, PayHistoryDto dto)
        {
            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                        && ph.RateChangeDate == rateChangeDate);

            if (history == null) return NotFound();

            if (dto.Rate != default(decimal)) history.Rate = dto.Rate;
            if (dto.PayFrequency != default(byte)) history.PayFrequency = dto.PayFrequency;

            history.ModifiedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(_mapper.Map<PayHistoryDto>(history));
        }
    }
}