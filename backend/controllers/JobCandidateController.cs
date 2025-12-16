using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.models.tools;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class JobCandidateController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public JobCandidateController(AdventureWorksContext db, IMapper mapper, IWebHostEnvironment env)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
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


        // POST: api/v1/jobcandidates/upload
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadCv([FromForm] IFormFile cv)
        {
            if (cv == null || cv.Length == 0)
                return BadRequest(new { message = "Nenhum ficheiro enviado." });

            if (!string.Equals(cv.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "O ficheiro deve ser um PDF." });

            // 0) Read uploaded file once
            byte[] pdfBytes;
            using (var ms = new MemoryStream())
            {
                await cv.CopyToAsync(ms);
                pdfBytes = ms.ToArray();
            }

            // 1) Extract text from a fresh MemoryStream (position at 0)
            string text;
            using (var parseStream = new MemoryStream(pdfBytes, writable: false))
            {
                text = PdfTextExtractor.ExtractAllText(parseStream);
            }

            // 2) Parse into structured data (your heuristics)
            var resumeData = ResumeParser.ParseFromText(text);

            // 3) Build AdventureWorks XML (order + required fields)
            var resumeXml = AdventureWorksResumeXmlBuilder.Build(resumeData);

            // 4) Save PDF to disk
            var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "cv");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);
            await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes);

            var now = DateTime.Now;
            var relativeUrl = $"/uploads/cv/{safeFileName}";

            // 5) Persist entity
            var jc = new JobCandidate
            {
                BusinessEntityId = null,
                Resume = resumeXml,
                ModifiedDate = now
            };

            _db.JobCandidates.Add(jc);
            await _db.SaveChangesAsync();

            return Created($"/api/v1/jobcandidate/{jc.JobCandidateId}", new
            {
                jobCandidateId = jc.JobCandidateId,
                fileUrl = relativeUrl
            });

        }

        // DELETE: api/v1/jobcandidate/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var jobcandidate = await _db.JobCandidates.FindAsync(id);
            if (jobcandidate == null) return NotFound();

            _db.JobCandidates.Remove(jobcandidate);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}