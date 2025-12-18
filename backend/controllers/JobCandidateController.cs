using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.models.tools;
using Microsoft.AspNetCore.Authorization;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class JobCandidateController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<JobCandidateController> _logger;

        public JobCandidateController(AdventureWorksContext db, IMapper mapper, IWebHostEnvironment env, ILogger<JobCandidateController> logger)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
            _logger = logger;
        }

        //GET: api/v1/jobcandidate
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Recebida requisição para obter todos os JobCandidates.");
            try
            {
                var candidates = await _db.JobCandidates
                    .OrderByDescending(c => c.ModifiedDate)
                    .ToListAsync();

                _logger.LogInformation("Encontrados {Count} JobCandidates.", candidates.Count);

                var dto = _mapper.Map<List<JobCandidateDto>>(candidates);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter lista de JobCandidates.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter os candidatos.");
            }
        }

        // GET: api/v1/jobcandidate/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("Recebida requisição para obter JobCandidate com ID={ID}.", id);

            try
            {
                var candidate = await _db.JobCandidates
                    .FirstOrDefaultAsync(jc => jc.JobCandidateId == id);

                if (candidate == null)
                {
                    _logger.LogWarning("JobCandidate não encontrado para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("JobCandidate encontrado para ID={ID}.", id);

                var dto = _mapper.Map<JobCandidateDto>(candidate);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter JobCandidate com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter o candidato.");
            }
        }


        // POST: api/v1/jobcandidate
        // [HttpPost]
        // public async Task<IActionResult> Create(JobCandidateDto dto)
        // {
        //     var candidate = _mapper.Map<JobCandidate>(dto);
        //     candidate.ModifiedDate = DateTime.Now;

        //     _db.JobCandidates.Add(candidate);
        //     await _db.SaveChangesAsync();

        //     return CreatedAtAction(nameof(Get),
        //         new { id = candidate.JobCandidateId },
        //         _mapper.Map<JobCandidateDto>(candidate));
        // }


        // POST: api/v1/jobcandidates/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> UploadAndCreate([FromForm] JobCandidateCreateForm form, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para upload de CV e criação de JobCandidate.");

            // 1) Validação do ficheiro
            var cv = form.Cv;
            if (cv is null || cv.Length == 0)
            {
                _logger.LogWarning("Nenhum ficheiro enviado.");
                return BadRequest(new { message = "Nenhum ficheiro enviado." });
            }

            var ext = Path.GetExtension(cv.FileName);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ficheiro inválido: extensão {Ext}. Esperado .pdf.", ext);
                return BadRequest(new { message = "O ficheiro deve ser um PDF (.pdf)." });
            }

            byte[] pdfBytes;
            await using (var ms = new MemoryStream())
            {
                await cv.CopyToAsync(ms, ct);
                pdfBytes = ms.ToArray();
            }

            if (!IsPdf(pdfBytes))
            {
                _logger.LogWarning("Conteúdo inválido: ficheiro não é um PDF válido.");
                return BadRequest(new { message = "Conteúdo inválido: o ficheiro não é um PDF válido." });
            }

            // 2) Extrair texto + construir XML
            string resumeXml = string.Empty;
            try
            {
                using var parseStream = new MemoryStream(pdfBytes, writable: false);
                var text = PdfTextExtractor.ExtractAllText(parseStream);
                var resumeData = ResumeParser.ParseFromText(text);

                if (resumeData != null)
                    resumeXml = AdventureWorksResumeXmlBuilder.Build(resumeData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao processar PDF para extração de texto.");
            }

            // 3) Guardar ficheiro em disco
            var baseRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsRoot = Path.Combine(baseRoot, "uploads", "cv");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            try
            {
                await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes, ct);
                _logger.LogInformation("Ficheiro guardado com sucesso: {File}.", safeFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao guardar ficheiro {File}.", safeFileName);
                return Problem(title: "Erro ao guardar o ficheiro",
                               detail: ex.Message,
                               statusCode: StatusCodes.Status500InternalServerError);
            }

            var now = DateTime.UtcNow;
            var relativeUrl = $"/uploads/cv/{safeFileName}";
            var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

            // 4) Construir entidade
            var candidate = new JobCandidate
            {
                BusinessEntityId = null,
                Resume = resumeXml,
                CvFileUrl = relativeUrl,
                ModifiedDate = now,

                BirthDate = form.BirthDate,
                NationalIDNumber = form.NationalIDNumber,
                MaritalStatus = form.MaritalStatus,
                Gender = form.Gender,

                FirstName = form.FirstName,
                LastName = form.LastName,

                PasswordHash = "DevOnly!234",
                Role = "employee"
            };

            _db.JobCandidates.Add(candidate);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("JobCandidate criado com sucesso. ID={ID}, ficheiro={File}.", candidate.JobCandidateId, safeFileName);
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(fullPath); } catch { /* ignore */ }
                _logger.LogError(ex, "Erro ao gravar JobCandidate. Ficheiro removido: {File}.", safeFileName);
                return Problem(title: "Erro ao persistir o candidato",
                               detail: ex.Message,
                               statusCode: StatusCodes.Status500InternalServerError);
            }

            var result = new
            {
                jobCandidateId = candidate.JobCandidateId,
                fileUrl = absoluteUrl
            };

            return Created($"/api/v1/jobcandidates/{candidate.JobCandidateId}", result);
        }

        private static bool IsPdf(byte[] bytes)
        {
            return bytes.Length > 4 &&
                   bytes[0] == 0x25 &&
                   bytes[1] == 0x50 &&
                   bytes[2] == 0x44 &&
                   bytes[3] == 0x46; // "%PDF"
        }


        // DELETE: api/v1/jobcandidate/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Recebida requisição para eliminar JobCandidate com ID={ID}.", id);

            try
            {
                var jobcandidate = await _db.JobCandidates.FindAsync(id);

                if (jobcandidate == null)
                {
                    _logger.LogWarning("JobCandidate não encontrado para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("JobCandidate encontrado para ID={ID}. A eliminar...", id);

                _db.JobCandidates.Remove(jobcandidate);
                await _db.SaveChangesAsync();

                _logger.LogInformation("JobCandidate eliminado com sucesso. ID={ID}.", id);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao eliminar JobCandidate com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar o candidato.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar JobCandidate com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar o candidato.");
            }
        }
    }
}