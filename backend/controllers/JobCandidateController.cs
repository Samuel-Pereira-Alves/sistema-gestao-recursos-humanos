using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.models.tools;
using sistema_gestao_recursos_humanos.backend.services;
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
        private readonly IAppLogService _appLog;

        public JobCandidateController(
            AdventureWorksContext db,
            IMapper mapper,
            IWebHostEnvironment env,
            ILogger<JobCandidateController> logger,
            IAppLogService appLog)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
            _logger = logger;
            _appLog = appLog;
        }

        // -----------------------
        // Helpers
        // -----------------------
        private static bool IsPdf(byte[] bytes)
        {
            // Assinatura "%PDF"
            return bytes.Length > 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;
        }

        // -----------------------
        // GET: api/v1/jobcandidate
        // -----------------------
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<List<JobCandidateDto>>> GetAll(CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter todos os JobCandidates.");
            await _appLog.InfoAsync("Recebida requisição para obter todos os JobCandidates.");

            try
            {
                var candidates = await GetAllJobCandidatesAsync(ct);

                _logger.LogInformation("Encontrados {Count} JobCandidates.", candidates.Count);
                await _appLog.InfoAsync($"Encontrados {candidates.Count} JobCandidates.");
                await _db.SaveChangesAsync(ct);

                var dto = _mapper.Map<List<JobCandidateDto>>(candidates);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedJobCandidateErrorAsync(ex, ct);
            }
        }
        private async Task<List<JobCandidate>> GetAllJobCandidatesAsync(CancellationToken ct)
        {
            return await _db.JobCandidates
                .AsNoTracking()
                .OrderByDescending(c => c.ModifiedDate)
                .ToListAsync(ct);
        }
        private async Task<ActionResult> HandleUnexpectedJobCandidateErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro ao processar JobCandidates.");
            await _appLog.ErrorAsync("Erro ao processar JobCandidates.", ex);

            return Problem(
                title: "Erro ao processar candidatos",
                detail: "Ocorreu um erro ao processar candidatos.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // -----------------------
        // GET: api/v1/jobcandidate/{id}
        // -----------------------
        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<JobCandidateDto>> Get(int id, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter JobCandidate com ID={Id}.", id);
            await _appLog.InfoAsync($"Recebida requisição para obter JobCandidate com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var candidate = await GetJobCandidateByIdAsync(id, ct);
                if (candidate is null)
                    return await HandleJobCandidateNotFoundAsync(id, ct);

                _logger.LogInformation("JobCandidate encontrado para ID={Id}.", id);
                await _appLog.InfoAsync($"JobCandidate encontrado para ID={id}.");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<JobCandidateDto>(candidate));
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedJobCandidateErrorAsync(ex, ct);
            }
        }
        private async Task<JobCandidate?> GetJobCandidateByIdAsync(int id, CancellationToken ct)
        {
            return await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == id, ct);
        }
        private async Task<ActionResult> HandleJobCandidateNotFoundAsync(int id, CancellationToken ct)
        {
            _logger.LogWarning("JobCandidate não encontrado para ID={Id}.", id);
            await _appLog.WarnAsync($"JobCandidate não encontrado para ID={id}.");
            await _db.SaveChangesAsync(ct);
            return NotFound();
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
            // 0) Logging inicial
            _logger.LogInformation("Recebida requisição para upload de CV e criação de JobCandidate.");
            await _appLog.InfoAsync("Recebida requisição para upload de CV e criação de JobCandidate.");
            await _db.SaveChangesAsync(ct);

            // 1) Validação do ficheiro (null/empty + extensão .pdf)
            var fileValidationError = await ValidateCvFileAsync(form.Cv, ct);
            if (fileValidationError is IActionResult badFile) return badFile;

            // 2) Ler bytes do PDF
            var pdfBytes = await ReadPdfBytesAsync(form.Cv!, ct);

            // 2.1) Validar conteúdo (assinatura PDF)
            var contentValidationError = await ValidatePdfContentAsync(pdfBytes, ct);
            if (contentValidationError is IActionResult badContent) return badContent;

            // 3) Extração de texto + XML (best-effort, com logs internos)
            var resumeXml = await ExtractResumeXmlBestEffortAsync(pdfBytes, ct);

            // 4) Guardar ficheiro em disco
            var (safeFileName, fullPath, saveFileError) = await TrySaveCvToDiskAsync(pdfBytes, ct);
            if (saveFileError is IActionResult saveErr) return saveErr;

            // 5) Construir entidade e persistir
            var now = DateTime.UtcNow;
            var candidate = BuildJobCandidateFromForm(form, resumeXml, pdfBytes, now);
            _db.JobCandidates.Add(candidate);

            var persistError = await TryPersistJobCandidateAsync(candidate, safeFileName!, fullPath!, ct);
            if (persistError is IActionResult persistErr) return persistErr;

            // 6) Resposta
            var result = new { jobCandidateId = candidate.JobCandidateId };

            _logger.LogInformation("Upload e criação concluídos. ID={ID}.", candidate.JobCandidateId);
            await _appLog.InfoAsync($"Upload e criação concluídos. ID={candidate.JobCandidateId}.");
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(Get), new { id = candidate.JobCandidateId }, result);
        }

        private async Task<IActionResult?> ValidateCvFileAsync(IFormFile? cv, CancellationToken ct)
        {
            if (cv is null || cv.Length == 0)
            {
                _logger.LogWarning("Nenhum ficheiro enviado.");
                await _appLog.WarnAsync("Nenhum ficheiro enviado.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Nenhum ficheiro enviado." });
            }

            var ext = Path.GetExtension(cv.FileName);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ficheiro inválido: extensão {Ext}. Esperado .pdf.", ext);
                await _appLog.WarnAsync($"Ficheiro inválido: extensão {ext}. Esperado .pdf.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "O ficheiro deve ser um PDF (.pdf)." });
            }

            return null;
        }
        private static async Task<byte[]> ReadPdfBytesAsync(IFormFile cv, CancellationToken ct)
        {
            await using var ms = new MemoryStream();
            await cv.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        private async Task<IActionResult?> ValidatePdfContentAsync(byte[] pdfBytes, CancellationToken ct)
        {
            if (!IsPdf(pdfBytes))
            {
                _logger.LogWarning("Conteúdo inválido: ficheiro não é um PDF válido.");
                await _appLog.WarnAsync("Conteúdo inválido: ficheiro não é um PDF válido.");
                await _db.SaveChangesAsync(ct);
                return BadRequest(new { message = "Conteúdo inválido: o ficheiro não é um PDF válido." });
            }
            return null;
        }
        private async Task<string> ExtractResumeXmlBestEffortAsync(byte[] pdfBytes, CancellationToken ct)
        {
            string resumeXml = string.Empty;
            try
            {
                _logger.LogInformation("A processar PDF para extração de texto.");
                await _appLog.InfoAsync("A processar PDF para extração de texto.");
                await _db.SaveChangesAsync(ct);

                using var parseStream = new MemoryStream(pdfBytes, writable: false);
                var text = PdfTextExtractor.ExtractAllText(parseStream);
                var resumeData = ResumeParser.ParseFromText(text);
                if (resumeData != null)
                    resumeXml = AdventureWorksResumeXmlBuilder.Build(resumeData);

                _logger.LogInformation("Extração concluída (XML construído? {HasXml}).", !string.IsNullOrEmpty(resumeXml));
                await _appLog.InfoAsync($"Extração de texto concluída (XML construído? {!string.IsNullOrEmpty(resumeXml)}).");
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Prossegue mesmo sem XML
                _logger.LogWarning(ex, "Falha ao processar PDF para extração de texto.");
                await _appLog.WarnAsync("Falha ao processar PDF para extração de texto.");
                await _db.SaveChangesAsync(ct);
            }

            return resumeXml;
        }
        private async Task<(string? safeFileName, string? fullPath, IActionResult? error)> TrySaveCvToDiskAsync(byte[] pdfBytes, CancellationToken ct)
        {
            var baseRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsRoot = Path.Combine(baseRoot, "uploads", "cv");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            try
            {
                await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes, ct);
                _logger.LogInformation("Ficheiro guardado com sucesso: {File}.", safeFileName);
                await _appLog.InfoAsync($"Ficheiro guardado com sucesso: {safeFileName}.");
                await _db.SaveChangesAsync(ct);
                return (safeFileName, fullPath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao guardar ficheiro {File}.", safeFileName);
                await _appLog.ErrorAsync($"Erro ao guardar ficheiro {safeFileName}.", ex);

                var problem = Problem(
                    title: "Erro ao guardar o ficheiro",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);

                return (null, null, problem);
            }
        }
        private static JobCandidate BuildJobCandidateFromForm(
            JobCandidateCreateForm form,
            string resumeXml,
            byte[] pdfBytes,
            DateTime now)
        {
            return new JobCandidate
            {
                BusinessEntityId = null,
                Resume = resumeXml,
                CvFileBytes = pdfBytes,
                ModifiedDate = now,
                BirthDate = form.BirthDate,
                NationalIDNumber = form.NationalIDNumber,
                MaritalStatus = form.MaritalStatus,
                Gender = form.Gender,
                Email = form.Email,
                FirstName = form.FirstName,
                LastName = form.LastName,
                PasswordHash = "DevOnly!234",  // TODO: Em produção, gerar uma password temporária segura
                Role = "employee"
            };
        }
        private async Task<IActionResult?> TryPersistJobCandidateAsync(
            JobCandidate candidate,
            string safeFileName,
            string fullPath,
            CancellationToken ct)
        {
            try
            {
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("JobCandidate criado com sucesso. ID={ID}, ficheiro={File}.", candidate.JobCandidateId, safeFileName);
                await _appLog.InfoAsync($"JobCandidate criado com sucesso. ID={candidate.JobCandidateId}, ficheiro={safeFileName}.");
                await _db.SaveChangesAsync(ct);

                return null;
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(fullPath); } catch { /* ignore */ }
                _logger.LogError(ex, "Erro ao gravar JobCandidate. Ficheiro removido: {File}.", safeFileName);
                await _appLog.ErrorAsync($"Erro ao gravar JobCandidate. Ficheiro removido: {safeFileName}.", ex);

                return Problem(
                    title: "Erro ao persistir o candidato",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // -----------------------
        // GET: api/v1/jobcandidate/{id}/cv
        // -----------------------
        [HttpGet("{id:int}/cv")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetCv(int id, CancellationToken ct)
        {
            _logger.LogInformation($"Recebida requisição para CV={id}.");
            await _appLog.InfoAsync($"Recebida requisição para CV={id}.");
            await _db.SaveChangesAsync(ct);

            var candidate = await _db.JobCandidates.FirstOrDefaultAsync(c => c.JobCandidateId == id, ct);
            if (candidate is null || candidate.CvFileBytes is null)
            {
                _logger.LogWarning("CV não encontrado para ID={Id}.", id);
                await _appLog.WarnAsync($"CV não encontrado para ID={id}.");
                await _db.SaveChangesAsync(ct);
                return NotFound();
            }

            _logger.LogInformation($"Requisição concluída com sucesso.");
            await _appLog.InfoAsync($"Requisição concluída com sucesso.");
            await _db.SaveChangesAsync(ct);

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
            await _appLog.InfoAsync($"Recebida requisição para eliminar JobCandidate com ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                // 1) Obter candidato via helper
                var jobCandidate = await GetJobCandidateByIdAsync(id, ct);
                if (jobCandidate is null)
                    return await HandleJobCandidateNotFoundAsync(id, ct);

                // 2) Log de encontrado
                _logger.LogInformation("JobCandidate encontrado para ID={Id}. A eliminar…", id);
                await _appLog.InfoAsync($"JobCandidate encontrado para ID={id}. A eliminar…");

                // 3) Remover e gravar
                _db.JobCandidates.Remove(jobCandidate);
                await _db.SaveChangesAsync(ct);

                // 4) Log de sucesso
                _logger.LogInformation("JobCandidate eliminado com sucesso. ID={Id}.", id);
                await _appLog.InfoAsync($"JobCandidate eliminado com sucesso. ID={id}.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleJobCandidateDeleteDbErrorAsync(dbEx, id, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedJobCandidateErrorAsync(ex, ct);
            }
        }
        private async Task<IActionResult> HandleJobCandidateDeleteDbErrorAsync(
            DbUpdateException dbEx,
            int id,
            CancellationToken ct)
        {
            _logger.LogError(dbEx, "Erro ao eliminar JobCandidate com ID={Id}.", id);
            await _appLog.ErrorAsync($"Erro ao eliminar JobCandidate com ID={id}.", dbEx);

            return Problem(
                title: "Erro ao eliminar",
                detail: "Erro ao eliminar o candidato.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}