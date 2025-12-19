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
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Get(int businessEntityId, DateTime rateChangeDate)
        {

            string message = $"Inicia procura de histórico de pagamento para o ID BusinessEntityId={businessEntityId} com a data RateChangeDate=${rateChangeDate}.";
            _logger.LogInformation(message);
            _db.Logs.Add(new Log { Message = message, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                         && ph.RateChangeDate == rateChangeDate);

            if (history == null)
            {
                string message2 = $"Nenhum registo encontrado para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogWarning(message2);
                _db.Logs.Add(new Log { Message = message2, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return NotFound();
            }

            string message3 = $"Registo Encontrado para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
            _logger.LogInformation(message3);
            _db.Logs.Add(new Log { Message = message3, Date = DateTime.Now });
            await _db.SaveChangesAsync();
            var dto = _mapper.Map<PayHistoryDto>(history);
            return Ok(dto);
        }

       
        // POST: api/v1/payhistory
        [HttpPost]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Create(PayHistoryDto dto)
        {
            string message4 = $"A iniciar a criação de um registo de pagamento para o BusinessEntityId={dto.BusinessEntityID} na data RateChangeDate={dto.RateChangeDate}.";
            _logger.LogInformation(message4);
            _db.Logs.Add(new Log { Message = message4, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                var history = _mapper.Map<PayHistory>(dto);
                history.ModifiedDate = DateTime.UtcNow;

                _db.PayHistories.Add(history);
                await _db.SaveChangesAsync();

                string message5 = $"Registo de Pagamento criado com sucesso.";
                _logger.LogInformation(message5);
                _db.Logs.Add(new Log { Message = message5, Date = DateTime.Now });

                return CreatedAtAction(nameof(Get),
                    new { businessEntityId = history.BusinessEntityID, rateChangeDate = history.RateChangeDate },
                    _mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateException ex)
            {
                string message6 = $"Erro ao criar o registo para BusinessEntityId={dto.BusinessEntityID} na data RateChangeDate={dto.RateChangeDate}.";
                _logger.LogError(ex, message6);
                _db.Logs.Add(new Log { Message = message6, Date = DateTime.Now });
               
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao criar registo.");
            }
            catch (Exception ex)
            {
                string message7 = $"Erro ao criar o registo para o BusinessEntityId={dto.BusinessEntityID}.";
                _logger.LogError(ex, message7);
                _db.Logs.Add(new Log { Message = message7, Date = DateTime.Now });

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
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Patch(int businessEntityId, DateTime rateChangeDate, PayHistoryDto dto)
        {
            string message8 = $"A iniciar atualização dos dados para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
            _logger.LogInformation(message8);
            _db.Logs.Add(new Log { Message = message8, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            var history = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                         && ph.RateChangeDate == rateChangeDate);

            if (history == null)
            {
                string message9 = $"PATCH falhou: registo não encontrado para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogWarning(message9);
                _db.Logs.Add(new Log { Message = message9, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NotFound();
            }

            try
            {
                if (dto.Rate != default(decimal)) history.Rate = dto.Rate;
                if (dto.PayFrequency != default(byte)) history.PayFrequency = dto.PayFrequency;

                history.ModifiedDate = DateTime.UtcNow;
                string message10 = $"PATCH concluído com sucesso para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogWarning(message10);
                _db.Logs.Add(new Log { Message = message10, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Ok(_mapper.Map<PayHistoryDto>(history));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                string message11 = $"Erro ao atualizar o registo do BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogError(ex, message11);
                _db.Logs.Add(new Log { Message = message11, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status409Conflict, "Conflito de concorrência ao atualizar o registo.");
            }
            catch (DbUpdateException ex)
            {
                string message12 = $"Erro ao atualizar os dados na BD para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogError(ex, message12);
                _db.Logs.Add(new Log { Message = message12, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar o registo.");
            }
            catch (Exception ex)
            {
                string message13 = $"Erro inesperado no PATCH para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogError(ex, message13);
                _db.Logs.Add(new Log { Message = message13, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro inesperado no servidor.");
            }
        }

        // DELETE: api/v1/payhistory/{businessEntityId}/{rateChangeDate}
        [HttpDelete("{businessEntityId}/{rateChangeDate}")]
        [Authorize(Roles ="admin")]
        public async Task<IActionResult> Delete(int businessEntityId, DateTime rateChangeDate)
        {
            string message14 = $"A iniciar o DELETE para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
            _logger.LogInformation(message14);
            _db.Logs.Add(new Log { Message = message14, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            var payhistory = await _db.PayHistories
                .FirstOrDefaultAsync(ph => ph.BusinessEntityID == businessEntityId
                                         && ph.RateChangeDate == rateChangeDate);

            if (payhistory == null)
            {
                string message15 = $"O DELETE falhou: registo não encontrado para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogWarning(message15);
                _db.Logs.Add(new Log { Message = message15, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NotFound();
            }

            try
            {
                _db.PayHistories.Remove(payhistory);
                await _db.SaveChangesAsync();

                string message16 = $"DELETE concluído com sucesso para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogInformation(message16);
                _db.Logs.Add(new Log { Message = message16, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                string message17 = $"Conflito de concorrência ao apagar BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogError(ex, message17);
                _db.Logs.Add(new Log { Message = message17, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status409Conflict, "Conflito de concorrência ao apagar o registo.");
            }
            catch (DbUpdateException ex)
            {
                string message18 = $"Erro ao apagar o registo da BD para o BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogError(ex, message18);
                _db.Logs.Add(new Log { Message = message18, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao apagar o registo.");
            }
            catch (Exception ex)
            {
                string message19 = $"Erro inesperado no DELETE para BusinessEntityId={businessEntityId} na data RateChangeDate={rateChangeDate}.";
                _logger.LogError(ex, message19);
                _db.Logs.Add(new Log { Message = message19, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro inesperado no servidor.");
            }
        }

    }
}