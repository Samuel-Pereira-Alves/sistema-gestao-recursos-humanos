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
            // 1) Pedido recebido
            string msg1 = "Recebida requisição para obter todos os JobCandidates.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Consulta
                var candidates = await _db.JobCandidates
                    .OrderByDescending(c => c.ModifiedDate)
                    .ToListAsync();

                // 3) Resultado
                string msg2 = $"Encontrados {candidates.Count} JobCandidates.";
                _logger.LogInformation(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 4) Mapeamento e retorno
                var dto = _mapper.Map<List<JobCandidateDto>>(candidates);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                // 5) Erro
                string msg3 = "Erro ao obter lista de JobCandidates.";
                _logger.LogError(ex, msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter os candidatos.");
            }
        }

        // GET: api/v1/jobcandidate/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Get(int id)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para obter JobCandidate com ID={id}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Consulta
                var candidate = await _db.JobCandidates
                    .FirstOrDefaultAsync(jc => jc.JobCandidateId == id);

                // 3) Não encontrado
                if (candidate == null)
                {
                    string msg2 = $"JobCandidate não encontrado para ID={id}.";
                    _logger.LogWarning(msg2);
                    _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // 4) Encontrado
                string msg3 = $"JobCandidate encontrado para ID={id}.";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 5) Mapeamento e retorno
                var dto = _mapper.Map<JobCandidateDto>(candidate);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                // 6) Erro
                string msg4 = $"Erro ao obter JobCandidate com ID={id}.";
                _logger.LogError(ex, msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

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
            // 1) Pedido recebido
            string msg1 = "Recebida requisição para upload de CV e criação de JobCandidate.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync(ct);

            // 2) Validação do ficheiro
            var cv = form.Cv;
            if (cv is null || cv.Length == 0)
            {
                string msg2 = "Nenhum ficheiro enviado.";
                _logger.LogWarning(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);

                return BadRequest(new { message = "Nenhum ficheiro enviado." });
            }

            var ext = Path.GetExtension(cv.FileName);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                string msg3 = $"Ficheiro inválido: extensão {ext}. Esperado .pdf.";
                _logger.LogWarning("Ficheiro inválido: extensão {Ext}. Esperado .pdf.", ext);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);

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
                string msg4 = "Conteúdo inválido: ficheiro não é um PDF válido.";
                _logger.LogWarning(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);

                return BadRequest(new { message = "Conteúdo inválido: o ficheiro não é um PDF válido." });
            }

            // 3) Extrair texto + construir XML
            string resumeXml = string.Empty;
            try
            {
                _logger.LogInformation("A processar PDF para extração de texto.");
                _db.Logs.Add(new Log { Message = "A processar PDF para extração de texto.", Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);

                using var parseStream = new MemoryStream(pdfBytes, writable: false);
                var text = PdfTextExtractor.ExtractAllText(parseStream);
                var resumeData = ResumeParser.ParseFromText(text);

                if (resumeData != null)
                    resumeXml = AdventureWorksResumeXmlBuilder.Build(resumeData);

                _logger.LogInformation("Extração de texto concluída (XML construído? {HasXml}).", !string.IsNullOrEmpty(resumeXml));
                _db.Logs.Add(new Log { Message = $"Extração de texto concluída (XML construído? {!string.IsNullOrEmpty(resumeXml)}).", Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                string msg5 = "Falha ao processar PDF para extração de texto.";
                _logger.LogWarning(ex, msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);
                // Continua mesmo assim (XML poderá ir vazio)
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

                string msg6 = $"Ficheiro guardado com sucesso: {safeFileName}.";
                _logger.LogInformation("Ficheiro guardado com sucesso: {File}.", safeFileName);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                string msg7 = $"Erro ao guardar ficheiro {safeFileName}.";
                _logger.LogError(ex, "Erro ao guardar ficheiro {File}.", safeFileName);
                _db.Logs.Add(new Log { Message = msg7, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao guardar o ficheiro",
                               detail: ex.Message,
                               statusCode: StatusCodes.Status500InternalServerError);
            }

            var now = DateTime.UtcNow;
            var relativeUrl = $"/uploads/cv/{safeFileName}";
            var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

            // 5) Construir entidade
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

                PasswordHash = "DevOnly!234", // Evitar em produção
                Role = "employee"
            };

            _db.JobCandidates.Add(candidate);

            try
            {
                await _db.SaveChangesAsync(ct);

                string msg8 = $"JobCandidate criado com sucesso. ID={candidate.JobCandidateId}, ficheiro={safeFileName}.";
                _logger.LogInformation("JobCandidate criado com sucesso. ID={ID}, ficheiro={File}.", candidate.JobCandidateId, safeFileName);
                _db.Logs.Add(new Log { Message = msg8, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(fullPath); } catch { /* ignore */ }

                string msg9 = $"Erro ao gravar JobCandidate. Ficheiro removido: {safeFileName}.";
                _logger.LogError(ex, "Erro ao gravar JobCandidate. Ficheiro removido: {File}.", safeFileName);
                _db.Logs.Add(new Log { Message = msg9, Date = DateTime.Now });
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao persistir o candidato",
                               detail: ex.Message,
                               statusCode: StatusCodes.Status500InternalServerError);
            }

            // 6) Resposta
            var result = new
            {
                jobCandidateId = candidate.JobCandidateId,
                fileUrl = absoluteUrl
            };

            string msg10 = $"Upload e criação concluídos. ID={candidate.JobCandidateId}.";
            _logger.LogInformation(msg10);
            _db.Logs.Add(new Log { Message = msg10, Date = DateTime.Now });
            await _db.SaveChangesAsync(ct);

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
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para eliminar JobCandidate com ID={id}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Procurar o candidato
                var jobcandidate = await _db.JobCandidates.FindAsync(id);

                // 3) Não encontrado
                if (jobcandidate == null)
                {
                    string msg2 = $"JobCandidate não encontrado para ID={id}.";
                    _logger.LogWarning(msg2);
                    _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // 4) Encontrado — a eliminar
                string msg3 = $"JobCandidate encontrado para ID={id}. A eliminar...";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 5) Remover e persistir
                _db.JobCandidates.Remove(jobcandidate);
                await _db.SaveChangesAsync();

                string msg4 = $"JobCandidate eliminado com sucesso. ID={id}.";
                _logger.LogInformation(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                string msg5 = $"Erro ao eliminar JobCandidate com ID={id}.";
                _logger.LogError(dbEx, msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar o candidato.");
            }
            catch (Exception ex)
            {
                string msg6 = $"Erro inesperado ao eliminar JobCandidate com ID={id}.";
                _logger.LogError(ex, msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar o candidato.");
            }
        }
    }
}