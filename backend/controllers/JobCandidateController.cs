using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.models.tools;
using System.Security.Claims;
using System.Security.Cryptography;

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

        public JobCandidateController(
            AdventureWorksContext db,
            IMapper mapper,
            IWebHostEnvironment env,
            ILogger<JobCandidateController> logger)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
            _logger = logger;
        }

        // -----------------------
        // Helpers
        // -----------------------
        private void AddLog(string message)
        {
            _db.Logs.Add(new Log { Message = message, Date = DateTime.UtcNow });
        }

        private static bool IsPdf(byte[] bytes)
        {
            // Assinatura "%PDF"
            return bytes.Length > 4 &&
                   bytes[0] == 0x25 &&
                   bytes[1] == 0x50 &&
                   bytes[2] == 0x44 &&
                   bytes[3] == 0x46;
        }

        // -----------------------
        // GET: api/v1/jobcandidate
        // -----------------------
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<List<JobCandidateDto>>> GetAll(CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter todos os JobCandidates.");
            AddLog("Recebida requisição para obter todos os JobCandidates.");

            try
            {
                var candidates = await _db.JobCandidates
                    .AsNoTracking()
                    .OrderByDescending(c => c.ModifiedDate)
                    .ToListAsync(ct);

                _logger.LogInformation("Encontrados {Count} JobCandidates.", candidates.Count);
                AddLog($"Encontrados {candidates.Count} JobCandidates.");
                await _db.SaveChangesAsync(ct);

                var dto = _mapper.Map<List<JobCandidateDto>>(candidates);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter lista de JobCandidates.");
                AddLog("Erro ao obter lista de JobCandidates.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao obter candidatos",
                    detail: "Ocorreu um erro ao obter os candidatos.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // GET: api/v1/jobcandidate/{id}
        // -----------------------
        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<JobCandidateDto>> Get(int id, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter JobCandidate com ID={Id}.", id);
            AddLog($"Recebida requisição para obter JobCandidate com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var candidate = await _db.JobCandidates
                    .FirstOrDefaultAsync(jc => jc.JobCandidateId == id, ct);

                if (candidate is null)
                {
                    _logger.LogWarning("JobCandidate não encontrado para ID={Id}.", id);
                    AddLog($"JobCandidate não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _logger.LogInformation("JobCandidate encontrado para ID={Id}.", id);
                AddLog($"JobCandidate encontrado para ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<JobCandidateDto>(candidate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter JobCandidate com ID={Id}.", id);
                AddLog($"Erro ao obter JobCandidate com ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao obter candidato",
                    detail: "Ocorreu um erro ao obter o candidato.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // POST: api/v1/jobcandidate/upload
        // -----------------------
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> UploadAndCreate([FromForm] JobCandidateCreateForm form, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para upload de CV e criação de JobCandidate.");
            AddLog("Recebida requisição para upload de CV e criação de JobCandidate.");
            await _db.SaveChangesAsync(ct);

            // 1) Validação do ficheiro
            var cv = form.Cv;
            if (cv is null || cv.Length == 0)
            {
                _logger.LogWarning("Nenhum ficheiro enviado.");
                AddLog("Nenhum ficheiro enviado.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Nenhum ficheiro enviado." });
            }

            var ext = Path.GetExtension(cv.FileName);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ficheiro inválido: extensão {Ext}. Esperado .pdf.", ext);
                AddLog($"Ficheiro inválido: extensão {ext}. Esperado .pdf.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "O ficheiro deve ser um PDF (.pdf)." });
            }

            // 2) Ler bytes do PDF (com cancelamento)
            byte[] pdfBytes;
            await using (var ms = new MemoryStream())
            {
                await cv.CopyToAsync(ms, ct);
                pdfBytes = ms.ToArray();
            }

            if (!IsPdf(pdfBytes))
            {
                _logger.LogWarning("Conteúdo inválido: ficheiro não é um PDF válido.");
                AddLog("Conteúdo inválido: ficheiro não é um PDF válido.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Conteúdo inválido: o ficheiro não é um PDF válido." });
            }

            // 3) Extrair texto + construir XML (best-effort)
            string resumeXml = string.Empty;
            try
            {
                _logger.LogInformation("A processar PDF para extração de texto.");
                AddLog("A processar PDF para extração de texto.");
                await _db.SaveChangesAsync(ct);

                using var parseStream = new MemoryStream(pdfBytes, writable: false);
                var text = PdfTextExtractor.ExtractAllText(parseStream);          // mantém comportamento atual
                var resumeData = ResumeParser.ParseFromText(text);                // idem
                if (resumeData != null)
                    resumeXml = AdventureWorksResumeXmlBuilder.Build(resumeData);

                _logger.LogInformation("Extração concluída (XML construído? {HasXml}).", !string.IsNullOrEmpty(resumeXml));
                AddLog($"Extração de texto concluída (XML construído? {!string.IsNullOrEmpty(resumeXml)}).");
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Prossegue mesmo sem XML
                _logger.LogWarning(ex, "Falha ao processar PDF para extração de texto.");
                AddLog("Falha ao processar PDF para extração de texto.");
                await _db.SaveChangesAsync(ct);
            }

            // 4) Guardar ficheiro em disco
            var baseRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsRoot = Path.Combine(baseRoot, "uploads", "cv");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            try
            {
                await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes, ct);
                _logger.LogInformation("Ficheiro guardado com sucesso: {File}.", safeFileName);
                AddLog($"Ficheiro guardado com sucesso: {safeFileName}.");
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao guardar ficheiro {File}.", safeFileName);
                AddLog($"Erro ao guardar ficheiro {safeFileName}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao guardar o ficheiro",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // 5) Construir entidade e persistir
            var now = DateTime.UtcNow;
            var candidate = new JobCandidate
            {
                BusinessEntityId = null,
                Resume = resumeXml,
                CvFileBytes = pdfBytes,
                ModifiedDate = now,
                BirthDate = form.BirthDate,
                NationalIDNumber = form.NationalIDNumber,
                MaritalStatus = form.MaritalStatus,
                Gender = form.Gender,
                FirstName = form.FirstName,
                LastName = form.LastName,
                PasswordHash = "DevOnly!234",  // TODO: Em produção, gerar uma password temporária segura
                Role = "employee"
            };

            _db.JobCandidates.Add(candidate);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("JobCandidate criado com sucesso. ID={ID}, ficheiro={File}.", candidate.JobCandidateId, safeFileName);
                AddLog($"JobCandidate criado com sucesso. ID={candidate.JobCandidateId}, ficheiro={safeFileName}.");
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(fullPath); } catch { /* ignore */ }
                _logger.LogError(ex, "Erro ao gravar JobCandidate. Ficheiro removido: {File}.", safeFileName);
                AddLog($"Erro ao gravar JobCandidate. Ficheiro removido: {safeFileName}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao persistir o candidato",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // 6) Resposta
            var result = new { jobCandidateId = candidate.JobCandidateId };

            _logger.LogInformation("Upload e criação concluídos. ID={ID}.", candidate.JobCandidateId);
            AddLog($"Upload e criação concluídos. ID={candidate.JobCandidateId}.");
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(Get), new { id = candidate.JobCandidateId }, result);
        }

        // -----------------------
        // GET: api/v1/jobcandidate/{id}/cv
        // -----------------------
        [HttpGet("{id:int}/cv")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetCv(int id, CancellationToken ct)
        {
            var candidate = await _db.JobCandidates.FirstOrDefaultAsync(c => c.JobCandidateId == id, ct);
            if (candidate is null || candidate.CvFileBytes is null)
                return NotFound();

            return File(candidate.CvFileBytes, "application/pdf", $"cv_{id}.pdf");
        }

        // -----------------------
        // DELETE: api/v1/jobcandidate/{id}
        // -----------------------
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para eliminar JobCandidate com ID={Id}.", id);
            AddLog($"Recebida requisição para eliminar JobCandidate com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var jobcandidate = await _db.JobCandidates.FirstOrDefaultAsync(c => c.JobCandidateId == id, ct);

                if (jobcandidate is null)
                {
                    _logger.LogWarning("JobCandidate não encontrado para ID={Id}.", id);
                    AddLog($"JobCandidate não encontrado para ID={id}.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _logger.LogInformation("JobCandidate encontrado para ID={Id}. A eliminar…", id);
                AddLog($"JobCandidate encontrado para ID={id}. A eliminar…");

                _db.JobCandidates.Remove(jobcandidate);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("JobCandidate eliminado com sucesso. ID={Id}.", id);
                AddLog($"JobCandidate eliminado com sucesso. ID={id}.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao eliminar JobCandidate com ID={Id}.", id);
                AddLog($"Erro ao eliminar JobCandidate com ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao eliminar",
                    detail: "Erro ao eliminar o candidato.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar JobCandidate com ID={Id}.", id);
                AddLog($"Erro inesperado ao eliminar JobCandidate com ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao eliminar",
                    detail: "Ocorreu um erro ao eliminar o candidato.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}