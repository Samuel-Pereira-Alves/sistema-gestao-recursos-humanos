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
    public class JobCandidateController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;

        public JobCandidateController(AdventureWorksContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: api/v1/jobcandidate
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var candidates = await _db.JobCandidates.ToListAsync();
            var dto = _mapper.Map<List<JobCandidateDto>>(candidates);
            return Ok(dto);
        }

        // GET: api/v1/jobcandidate/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var candidate = await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == id);

            if (candidate == null) return NotFound();

            var dto = _mapper.Map<JobCandidateDto>(candidate);
            return Ok(dto);
        }

        // POST: api/v1/jobcandidate
        [HttpPost]
        public async Task<IActionResult> Create(JobCandidateDto dto)
        {
            var candidate = _mapper.Map<JobCandidate>(dto);
            candidate.ModifiedDate = DateTime.Now;

            _db.JobCandidates.Add(candidate);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new { id = candidate.JobCandidateId },
                _mapper.Map<JobCandidateDto>(candidate));
        }
    }
}