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

        // // GET: api/v1/payhistory
        // [HttpGet]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> GetAll()
        // {
        //     var histories = await _db.PayHistories.ToListAsync();
        //     var dto = _mapper.Map<List<PayHistoryDto>>(histories);
        //     return Ok(dto);
        // }

        // GET: api/v1/payhistory/{businessEntityId}
        // [HttpGet("{businessEntityId}")]
        // public async Task<IActionResult> GetAllByEmployee(int businessEntityId)
        // {
        //     var histories = await _db.PayHistories
        //         .Where(ph => ph.BusinessEntityID == businessEntityId)
        //         .OrderByDescending(ph => ph.RateChangeDate)
        //         .ToListAsync();

        //     var dtos = _mapper.Map<List<PayHistoryDto>>(histories);
        //     return Ok(dtos);
        // }


        // GET: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpGet("{businessEntityId}/{rateChangeDate}")]
        public async Task<IActionResult> Get(int businessEntityId, DateTime rateChangeDate)
        {

            _logger.LogInformation("Inicia procura de histórico de pagamento para o ID BusinessEntityId={BusinessEntityId} com a data RateChangeDate={RateChangeDate}.",
                businessEntityId, rateChangeDate);

            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                         && ph.RateChangeDate == rateChangeDate);

            if (history == null)
            {
                _logger.LogWarning("Nenhum registo encontrado para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return NotFound();
            }

            _logger.LogInformation("Registo Encontrado para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                businessEntityId, rateChangeDate);

            var dto = _mapper.Map<PayHistoryDto>(history);
            return Ok(dto);
        }

       
        // POST: api/v1/payhistory
        [HttpPost]
        public async Task<IActionResult> Create(PayHistoryDto dto)
        {
            _logger.LogInformation(
                "A iniciar a criação de um registo de pagamento para o BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                dto.BusinessEntityID, dto.RateChangeDate);

            try
            {
                var history = _mapper.Map<PayHistory>(dto);
                history.ModifiedDate = DateTime.UtcNow;

                _db.PayHistories.Add(history);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Registo de Pagamento criado com sucesso."
                    );

                return CreatedAtAction(nameof(Get),
                    new { businessEntityId = history.BusinessEntityID, rateChangeDate = history.RateChangeDate },
                    _mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Erro ao criar o registo para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    dto.BusinessEntityID, dto.RateChangeDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao criar registo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao criar o registo para o BusinessEntityId={BusinessEntityId}.",
                    dto.BusinessEntityID);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro inesperado no servidor.");
            }
        }

        // PUT: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        // [HttpPut("{businessEntityId}/{rateChangeDate}")]
        // public async Task<IActionResult> Update(int businessEntityId, DateTime rateChangeDate, PayHistoryDto dto)
        // {
        //     var history = await _db.PayHistories
        //         .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
        //                                 && ph.RateChangeDate == rateChangeDate);

        //     if (history == null) return NotFound();

        //     _mapper.Map(dto, history);
        //     history.ModifiedDate = DateTime.Now;

        //     _db.Entry(history).State = EntityState.Modified;
        //     await _db.SaveChangesAsync();

        //     return NoContent();
        // }

        
        // PATCH: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpPatch("{businessEntityId}/{rateChangeDate}")]
        public async Task<IActionResult> Patch(int businessEntityId, DateTime rateChangeDate, PayHistoryDto dto)
        {
            _logger.LogInformation(
                "A iniciar atualização dos dados para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                businessEntityId, rateChangeDate);

            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                         && ph.RateChangeDate == rateChangeDate);

            if (history == null)
            {
                _logger.LogWarning(
                    "PATCH falhou: registo não encontrado para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return NotFound();
            }

            try
            {
                if (dto.Rate != default(decimal)) history.Rate = dto.Rate;
                if (dto.PayFrequency != default(byte)) history.PayFrequency = dto.PayFrequency;

                history.ModifiedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "PATCH concluído com sucesso para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);

                return Ok(_mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex,
                    "Erro ao atualizar o registo do BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return StatusCode(StatusCodes.Status409Conflict, "Conflito de concorrência ao atualizar o registo.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Erro ao atualizar os dados na BD para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar o registo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado no PATCH para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro inesperado no servidor.");
            }
        }

        // DELETE: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpDelete("{businessEntityId}/{rateChangeDate}")]
        public async Task<IActionResult> Delete(int businessEntityId, DateTime rateChangeDate)
        {
            _logger.LogInformation(
                "A iniciar o DELETE para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                businessEntityId, rateChangeDate);

            var payhistory = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                         && ph.RateChangeDate == rateChangeDate);

            if (payhistory == null)
            {
                _logger.LogWarning(
                    "O DELETE falhou: registo não encontrado para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return NotFound();
            }

            try
            {
                _db.PayHistories.Remove(payhistory);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "DELETE concluído com sucesso para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex,
                    "Conflito de concorrência ao apagar BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return StatusCode(StatusCodes.Status409Conflict, "Conflito de concorrência ao apagar o registo.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Erro ao apagar o registo da BD para o BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao apagar o registo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado no DELETE para BusinessEntityId={BusinessEntityId} na data RateChangeDate={RateChangeDate}.",
                    businessEntityId, rateChangeDate);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro inesperado no servidor.");
            }
        }

    }
}