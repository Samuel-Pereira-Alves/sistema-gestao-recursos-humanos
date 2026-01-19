using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.services;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PayHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<PayHistoryController> _logger;
        private readonly IAppLogService _appLog;

        public PayHistoryController(
            AdventureWorksContext db,
            IMapper mapper,
            ILogger<PayHistoryController> logger,
            IAppLogService appLog)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _appLog = appLog;
        }


        // GET: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpGet("{businessEntityId:int}/{rateChangeDate}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<PayHistoryDto>> Get(
            int businessEntityId,
            DateTime rateChangeDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Iniciando procura de PayHistory para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}",
                businessEntityId, rateChangeDate);

            await _appLog.InfoAsync(
                $"Procura PayHistory: BEID={businessEntityId}, RateChangeDate={rateChangeDate:o}");
            await _db.SaveChangesAsync(ct); // mantém como no teu código original

            try
            {
                // Sanitização simples dos parâmetros de paginação vindos pela query
                const int MaxPageSize = 200;
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > MaxPageSize) pageSize = MaxPageSize;

                var history = await GetPayHistoryAsync(businessEntityId, rateChangeDate, ct);

                // ---- Paginação via query (metadados) ----
                // Como o recurso é singular: totalCount = 1 se encontrou, senão 0.
                int totalCount = history is null ? 0 : 1;

                // TotalPages calculado com base em pageSize
                int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

                // Em um recurso singular, prev/next serão sempre false
                bool hasPrevious = pageNumber > 1 && totalPages > 0;
                bool hasNext = pageNumber < totalPages;

                var paginationHeader = System.Text.Json.JsonSerializer.Serialize(new
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPrevious = hasPrevious,
                    HasNext = hasNext
                });
                Response.Headers["X-Pagination"] = paginationHeader;

                // Link header (self/prev/next) utilizando os parâmetros da query
                string BuildLink(int pn) => Url.ActionLink(null, null, new
                {
                    businessEntityId,
                    rateChangeDate,
                    pageNumber = pn,
                    pageSize
                })!;
                var links = new List<string> { $"<{BuildLink(pageNumber)}>; rel=\"self\"" };
                if (hasPrevious) links.Add($"<{BuildLink(pageNumber - 1)}>; rel=\"prev\"");
                if (hasNext) links.Add($"<{BuildLink(pageNumber + 1)}>; rel=\"next\"");
                Response.Headers["Link"] = string.Join(", ", links);

                if (history is null)
                {
                    _logger.LogWarning(
                        "Nenhum registo encontrado para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}",
                        businessEntityId, rateChangeDate);
                    await _appLog.WarnAsync("Nenhum registo encontrado.");
                    await _db.SaveChangesAsync(ct); // mantém como no teu original
                    return NotFound();
                }

                // (Opcional) ETag condicional (não altera contrato)
                var etagRaw = $"{businessEntityId}:{rateChangeDate:O}";
                var etag = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(etagRaw)));
                if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString() == etag)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
                Response.Headers.ETag = etag;

                return Ok(_mapper.Map<PayHistoryDto>(history));
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedPayHistoryErrorAsync(ex, ct);
            }
        }

        private async Task<PayHistory?> GetPayHistoryAsync(
            int businessEntityId,
            DateTime rateChangeDate,
            CancellationToken ct)
        {
            return await _db.PayHistories
                .FirstOrDefaultAsync(ph =>
                    ph.BusinessEntityID == businessEntityId &&
                    ph.RateChangeDate == rateChangeDate, ct);
        }

        private async Task<ActionResult> HandleUnexpectedPayHistoryErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro inesperado no PayHistory");
            await _appLog.ErrorAsync("Erro inesperado no PayHistory", ex);
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro ao processar o PayHistory",
                detail: "Ocorreu um erro ao processar o PayHistory.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        private async Task<ActionResult> HandleDatabasePayHistoryErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro inesperado de base de dados ao processar PayHistory");
            await _appLog.ErrorAsync("Erro inesperado de base de dados ao processar PayHistory", ex);
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro inesperado de base de dados ao processar PayHistory",
                detail: "Erro inesperado de base de dados ao processar PayHistory",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        private async Task<ActionResult> HandleConcurrencyPayHistoryErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro de concorrência ao processar PayHistory");
            await _appLog.ErrorAsync("Erro de concorrência ao processar PayHistory", ex);
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro de concorrência ao processar PayHistory",
                detail: "Erro de concorrência ao processar PayHistory",
                statusCode: StatusCodes.Status409Conflict);
        }

        // POST: api/v1/payhistory
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] PayHistoryDto dto, CancellationToken ct)
        {
            _logger.LogInformation("A iniciar criação de PayHistory para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", dto.BusinessEntityID, dto.RateChangeDate);
            await _appLog.InfoAsync("Criação de PayHistory iniciada.");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
                return BadRequest(new { message = "Body é obrigatório" });

            try
            {
                var history = _mapper.Map<PayHistory>(dto);
                history.ModifiedDate = DateTime.UtcNow;

                _db.PayHistories.Add(history);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("PayHistory criado com sucesso para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", history.BusinessEntityID, history.RateChangeDate);
                await _appLog.InfoAsync("PayHistory criado com sucesso.");
                await _db.SaveChangesAsync(ct);

                return CreatedAtAction(nameof(Get),
                    new { businessEntityId = history.BusinessEntityID, rateChangeDate = history.RateChangeDate },
                    _mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                var exists = await _db.PayHistories.AnyAsync(ph =>
                ph.BusinessEntityID == dto.BusinessEntityID &&
                ph.RateChangeDate == dto.RateChangeDate, ct);

                if (exists)
                {
                    return Conflict(new ProblemDetails
                    {
                        Title = "Registo duplicado",
                        Detail = "Já existe um registo para este colaborador nesta data.",
                        Status = StatusCodes.Status409Conflict
                    });
                }
                return await HandleDatabasePayHistoryErrorAsync(dbEx, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedPayHistoryErrorAsync(ex, ct);
            }
        }

        // PATCH: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpPatch("{businessEntityId:int}/{rateChangeDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Patch(int businessEntityId, DateTime rateChangeDate, [FromBody] PayHistoryDto dto, CancellationToken ct)
        {
            _logger.LogInformation("A iniciar PATCH para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
            await _appLog.InfoAsync("PATCH PayHistory iniciado.");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
                return BadRequest(new { message = "Body é obrigatório" });

            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId && ph.RateChangeDate == rateChangeDate, ct);

            if (history is null)
                return NotFound();

            try
            {
                if (dto.Rate != default(decimal)) history.Rate = dto.Rate;
                if (dto.PayFrequency != default(byte)) history.PayFrequency = dto.PayFrequency;
                history.ModifiedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("PATCH concluído com sucesso para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
                await _appLog.InfoAsync("PATCH PayHistory concluído.");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                return await HandleConcurrencyPayHistoryErrorAsync(dbEx, ct);
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleDatabasePayHistoryErrorAsync(dbEx, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedPayHistoryErrorAsync(ex, ct);
            }
        }

        // DELETE: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpDelete("{businessEntityId:int}/{rateChangeDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int businessEntityId, DateTime rateChangeDate, CancellationToken ct)
        {
            _logger.LogInformation("A iniciar DELETE para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
            await _appLog.InfoAsync("DELETE PayHistory iniciado.");
            await _db.SaveChangesAsync(ct);

            var payhistory = await GetPayHistoryAsync(businessEntityId, rateChangeDate, ct);

            if (payhistory is null)
                return NotFound();

            try
            {
                _db.PayHistories.Remove(payhistory);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("DELETE concluído com sucesso para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
                await _appLog.InfoAsync("DELETE PayHistory concluído.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                return await HandleConcurrencyPayHistoryErrorAsync(dbEx, ct);
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleDatabasePayHistoryErrorAsync(dbEx, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedPayHistoryErrorAsync(ex, ct);
            }
        }
    }
}