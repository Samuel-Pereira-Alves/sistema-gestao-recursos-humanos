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
    public class PayHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<PayHistoryController> _logger;

        public PayHistoryController(AdventureWorksContext db, IMapper mapper, ILogger<PayHistoryController> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
        }

        private void AddLog(string message) =>
            _db.Logs.Add(new Log { Message = message, Date = DateTime.UtcNow });

        // GET: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpGet("{businessEntityId:int}/{rateChangeDate}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<PayHistoryDto>> Get(int businessEntityId, DateTime rateChangeDate, CancellationToken ct)
        {
            _logger.LogInformation("Iniciando procura de PayHistory para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
            AddLog($"Procura PayHistory: BEID={businessEntityId}, RateChangeDate={rateChangeDate:o}");
            await _db.SaveChangesAsync(ct);

            try
            {
                var history = await _db.PayHistories
                    .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId && ph.RateChangeDate == rateChangeDate, ct);

                if (history is null)
                {
                    _logger.LogWarning("Nenhum registo encontrado para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
                    AddLog("Nenhum registo encontrado.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                return Ok(_mapper.Map<PayHistoryDto>(history));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter PayHistory.");
                AddLog($"Erro ao obter PayHistory: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao consultar", detail: "Ocorreu um erro ao obter PayHistory.", statusCode: 500);
            }
        }

        // POST: api/v1/payhistory
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] PayHistoryDto dto, CancellationToken ct)
        {
            _logger.LogInformation("A iniciar criação de PayHistory para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", dto.BusinessEntityID, dto.RateChangeDate);
            AddLog("Criação de PayHistory iniciada.");
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
                AddLog("PayHistory criado com sucesso.");
                await _db.SaveChangesAsync(ct);

                return CreatedAtAction(nameof(Get),
                    new { businessEntityId = history.BusinessEntityID, rateChangeDate = history.RateChangeDate },
                    _mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao criar PayHistory.");
                AddLog($"Erro ao criar PayHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao criar", detail: "Erro ao persistir PayHistory.", statusCode: 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar PayHistory.");
                AddLog($"Erro inesperado ao criar PayHistory: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro inesperado", detail: "Ocorreu um erro ao criar PayHistory.", statusCode: 500);
            }
        }

        // PATCH: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpPatch("{businessEntityId:int}/{rateChangeDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Patch(int businessEntityId, DateTime rateChangeDate, [FromBody] PayHistoryDto dto, CancellationToken ct)
        {
            _logger.LogInformation("A iniciar PATCH para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
            AddLog("PATCH PayHistory iniciado.");
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
                AddLog("PATCH PayHistory concluído.");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Conflito de concorrência ao atualizar PayHistory.");
                AddLog($"Conflito de concorrência: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Conflito de concorrência", detail: "Erro ao atualizar PayHistory.", statusCode: 409);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao atualizar PayHistory.");
                AddLog($"Erro ao atualizar PayHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao atualizar", detail: "Erro ao persistir alterações.", statusCode: 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no PATCH de PayHistory.");
                AddLog($"Erro inesperado no PATCH: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro inesperado", detail: "Ocorreu um erro ao atualizar PayHistory.", statusCode: 500);
            }
        }

        // DELETE: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpDelete("{businessEntityId:int}/{rateChangeDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int businessEntityId, DateTime rateChangeDate, CancellationToken ct)
        {
            _logger.LogInformation("A iniciar DELETE para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
            AddLog("DELETE PayHistory iniciado.");
            await _db.SaveChangesAsync(ct);

            var payhistory = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId && ph.RateChangeDate == rateChangeDate, ct);

            if (payhistory is null)
                return NotFound();

            try
            {
                _db.PayHistories.Remove(payhistory);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("DELETE concluído com sucesso para BEID={BusinessEntityId}, RateChangeDate={RateChangeDate:o}", businessEntityId, rateChangeDate);
                AddLog("DELETE PayHistory concluído.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Conflito de concorrência ao apagar PayHistory.");
                AddLog($"Conflito ao apagar PayHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Conflito de concorrência", detail: "Erro ao apagar PayHistory.", statusCode: 409);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao apagar PayHistory.");
                AddLog($"Erro ao apagar PayHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao apagar", detail: "Erro ao eliminar PayHistory.", statusCode: 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no DELETE de PayHistory.");
                AddLog($"Erro inesperado no DELETE: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro inesperado", detail: "Ocorreu um erro ao eliminar PayHistory.", statusCode: 500);
            }
        }
    }
}