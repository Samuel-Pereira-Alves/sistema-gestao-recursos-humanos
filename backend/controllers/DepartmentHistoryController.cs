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
    public class DepartmentHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;

        private readonly ILogger<DepartmentHistoryController> _logger;
        public DepartmentHistoryController(AdventureWorksContext db, IMapper mapper, ILogger<DepartmentHistoryController> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
        }

        // // GET: api/v1/departmenthistory
        // [HttpGet]
        // [Authorize(Roles ="admin, employee")]
        // public async Task<IActionResult> GetAll()
        // {
        //     var histories = await _db.DepartmentHistories
        //         .Include(dh => dh.Department)
        //         .ToListAsync();

        //     var dto = _mapper.Map<List<DepartmentHistoryDto>>(histories);
        //     return Ok(dto);
        // }


        [HttpGet]
        [Authorize(Roles = "admin, employee")]

        [HttpGet]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetAll()
        {
            string messageRequest = "Recebido pedido para obter o historico de Departmentos.";
            _logger.LogInformation(messageRequest);
            _db.Logs.Add(new Log { Message = messageRequest, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                var histories = await _db.DepartmentHistories
                    .Include(dh => dh.Department)
                    .ToListAsync();

                if (histories.Count == 0)
                {
                    string messageNoRecords = "Consulta de DepartmentHistories retornou 0 registos.";
                    _logger.LogWarning(messageNoRecords);
                    _db.Logs.Add(new Log { Message = messageNoRecords, Date = DateTime.Now });
                    await _db.SaveChangesAsync();
                }

                var dto = _mapper.Map<List<DepartmentHistoryDto>>(histories);

                string messageSuccess = "Consulta de DepartmentHistories concluída com sucesso.";
                _logger.LogInformation(messageSuccess);
                _db.Logs.Add(new Log { Message = messageSuccess, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Ok(dto);
            }
            catch (DbUpdateException dbEx)
            {
                string messageDbError = "Erro de base de dados ao obter DepartmentHistories.";
                _logger.LogError(dbEx, messageDbError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageDbError} Detalhes: {dbEx.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao consultar DepartmentHistories.");
            }
            catch (Exception ex)
            {
                string messageUnexpectedError = "Erro inesperado ao obter DepartmentHistories.";
                _logger.LogError(ex, messageUnexpectedError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageUnexpectedError} Detalhes: {ex.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter DepartmentHistories.");
            }
        }

        [HttpGet("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Get(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            string messageRequest =
                $"Pedido para obter DepartmentHistory recebido. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
            _logger.LogInformation(messageRequest);
            _db.Logs.Add(new Log { Message = messageRequest, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                var history = await _db.DepartmentHistories
                    .Include(dh => dh.Department)
                    .FirstOrDefaultAsync(dh =>
                        dh.BusinessEntityID == businessEntityId &&
                        dh.DepartmentID == departmentId &&
                        dh.ShiftID == shiftId &&
                        dh.StartDate == startDate);

                if (history is null)
                {
                    string messageNotFound =
                        $"DepartmentHistory não encontrado. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                    _logger.LogWarning(messageNotFound);
                    _db.Logs.Add(new Log { Message = messageNotFound, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                var dto = _mapper.Map<DepartmentHistoryDto>(history);

                string messageSuccess =
                    $"DepartmentHistory obtido com sucesso. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogInformation(messageSuccess);
                _db.Logs.Add(new Log { Message = messageSuccess, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Ok(dto);
            }
            catch (DbUpdateException dbEx)
            {
                string messageDbError =
                    $"Erro de base de dados ao obter DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogError(dbEx, messageDbError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageDbError} Detalhes: {dbEx.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Erro ao consultar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                string messageUnexpectedError =
                    $"Erro inesperado ao obter DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogError(ex, messageUnexpectedError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageUnexpectedError} Detalhes: {ex.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Ocorreu um erro ao obter DepartmentHistory.");
            }
        }


        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(DepartmentHistoryDto dto)
        {
            string messageRequest =
                $"Recebido pedido para criar DepartmentHistory. BusinessEntityID={dto?.BusinessEntityID}, DepartmentId={dto?.DepartmentId}, ShiftID={dto?.ShiftID}, StartDate={dto?.StartDate}.";
            _logger.LogInformation(messageRequest);
            _db.Logs.Add(new Log { Message = messageRequest, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            if (dto is null)
            {
                string messageNoBody = "Body não enviado no pedido de criação de DepartmentHistory.";
                _logger.LogWarning(messageNoBody);
                _db.Logs.Add(new Log { Message = messageNoBody, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return BadRequest(new { message = "Body é obrigatório" });
            }

            try
            {
                // 1) Validar Employee (FK)
                var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID);
                if (!employeeExists)
                {
                    string messageEmployeeNotFound =
                        $"Criação falhou: Employee não encontrado. BusinessEntityID={dto.BusinessEntityID}.";
                    _logger.LogWarning(messageEmployeeNotFound);
                    _db.Logs.Add(new Log { Message = messageEmployeeNotFound, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });
                }

                // 2) Validar Department (FK)
                if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                {
                    string messageDeptOutOfRange =
                        $"Criação falhou: DepartmentId fora do intervalo de short. DepartmentId={dto.DepartmentId}.";
                    _logger.LogWarning(messageDeptOutOfRange);
                    _db.Logs.Add(new Log { Message = messageDeptOutOfRange, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });
                }

                var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
                if (!deptExists)
                {
                    string messageDeptNotFound =
                        $"Criação falhou: Department não encontrado. DepartmentId={dto.DepartmentId}.";
                    _logger.LogWarning(messageDeptNotFound);
                    _db.Logs.Add(new Log { Message = messageDeptNotFound, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });
                }

                // 3) Validar StartDate no range de SQL Server datetime
                var minSqlDate = new DateTime(1753, 1, 1);
                if (dto.StartDate < minSqlDate)
                {
                    string messageStartDateInvalid =
                        $"Criação falhou: StartDate inválida para SQL Server datetime. StartDate={dto.StartDate}.";
                    _logger.LogWarning(messageStartDateInvalid);
                    _db.Logs.Add(new Log { Message = messageStartDateInvalid, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });
                }

                // 4) Evitar duplicado de PK composta
                var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                    dh.BusinessEntityID == dto.BusinessEntityID &&
                    dh.DepartmentID == (short)dto.DepartmentId &&
                    dh.ShiftID == dto.ShiftID &&
                    dh.StartDate == dto.StartDate);

                if (exists)
                {
                    string messageDup =
                        $"Criação falhou: DepartmentHistory duplicado. BusinessEntityID={dto.BusinessEntityID}, DepartmentId={dto.DepartmentId}, ShiftID={dto.ShiftID}, StartDate={dto.StartDate}.";
                    _logger.LogWarning(messageDup);
                    _db.Logs.Add(new Log { Message = messageDup, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return Conflict(new
                    {
                        message = "Registo de DepartmentHistory já existe",
                        businessEntityId = dto.BusinessEntityID,
                        departmentId = dto.DepartmentId,
                        shiftId = dto.ShiftID,
                        startDate = dto.StartDate
                    });
                }

                // 4.1) Encerrar movimentos abertos anteriores (EndDate=null)
                var lastMovements = await _db.DepartmentHistories
                    .Where(dh => dh.BusinessEntityID == dto.BusinessEntityID && dh.EndDate == null)
                    .ToListAsync();

                if (lastMovements.Count > 0)
                {
                    string messageClosing =
                        $"Encontrados {lastMovements.Count} movimentos anteriores com EndDate=null para encerrar. BusinessEntityID={dto.BusinessEntityID}.";
                    _logger.LogInformation(messageClosing);
                    _db.Logs.Add(new Log { Message = messageClosing, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    var now = DateTime.Now;
                    foreach (var movement in lastMovements)
                    {
                        movement.EndDate = dto.StartDate;
                        movement.ModifiedDate = now;
                    }
                }

                // 5) Mapear e inserir
                var history = _mapper.Map<DepartmentHistory>(dto);
                history.ModifiedDate = DateTime.Now;

                _db.DepartmentHistories.Add(history);
                await _db.SaveChangesAsync();

                string messageSuccess =
                    $"DepartmentHistory criado com sucesso. BusinessEntityID={history.BusinessEntityID}, DepartmentID={history.DepartmentID}, ShiftID={history.ShiftID}, StartDate={history.StartDate}.";
                _logger.LogInformation(messageSuccess);
                _db.Logs.Add(new Log { Message = messageSuccess, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(Get),
                    new
                    {
                        businessEntityId = history.BusinessEntityID,
                        departmentId = history.DepartmentID,
                        shiftId = history.ShiftID,
                        startDate = history.StartDate
                    },
                    _mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                string messageDbError =
                    $"Erro ao gravar DepartmentHistory. BusinessEntityID={dto?.BusinessEntityID}, DepartmentId={dto?.DepartmentId}, ShiftID={dto?.ShiftID}, StartDate={dto?.StartDate}.";
                _logger.LogError(dbEx, messageDbError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageDbError} Detalhes: {dbEx.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                string messageUnexpectedError =
                    $"Erro inesperado ao criar DepartmentHistory. BusinessEntityID={dto?.BusinessEntityID}, DepartmentId={dto?.DepartmentId}, ShiftID={dto?.ShiftID}, StartDate={dto?.StartDate}.";
                _logger.LogError(ex, messageUnexpectedError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageUnexpectedError} Detalhes: {ex.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar DepartmentHistory.");
            }
        }


        // PUT: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        // [HttpPut("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        // public async Task<IActionResult> Update(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        // {
        //     var history = await _db.DepartmentHistories
        //         .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
        //                                 && dh.DepartmentID == departmentId
        //                                 && dh.ShiftID == shiftId
        //                                 && dh.StartDate == startDate);

        //     if (history == null) return NotFound();

        //     _mapper.Map(dto, history);
        //     history.ModifiedDate = DateTime.Now;

        //     _db.Entry(history).State = EntityState.Modified;
        //     await _db.SaveChangesAsync();

        //     return NoContent();
        // }

        
        [HttpPatch("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Patch(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        {
            string messageRequest =
                $"Recebido pedido para atualizar (PATCH) DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
            _logger.LogInformation(messageRequest);
            _db.Logs.Add(new Log { Message = messageRequest, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            if (dto is null)
            {
                string messageNoBody = "Body não enviado no PATCH de DepartmentHistory.";
                _logger.LogWarning(messageNoBody);
                _db.Logs.Add(new Log { Message = messageNoBody, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return BadRequest(new { message = "Body é obrigatório" });
            }

            try
            {
                var history = await _db.DepartmentHistories
                    .FirstOrDefaultAsync(dh =>
                        dh.BusinessEntityID == businessEntityId &&
                        dh.DepartmentID == departmentId &&
                        dh.ShiftID == shiftId &&
                        dh.StartDate == startDate);

                if (history is null)
                {
                    string messageNotFound =
                        $"PATCH falhou: DepartmentHistory não encontrado. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                    _logger.LogWarning(messageNotFound);
                    _db.Logs.Add(new Log { Message = messageNotFound, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // Aplicar alterações permitidas (PATCH parcial)
                if (dto.EndDate.HasValue)
                {
                    // Validação simples: EndDate >= StartDate
                    if (dto.EndDate.Value < history.StartDate)
                    {
                        string messageInvalidEndDate =
                            $"PATCH inválido: EndDate anterior ao StartDate. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}, EndDate={dto.EndDate}.";
                        _logger.LogWarning(messageInvalidEndDate);
                        _db.Logs.Add(new Log { Message = messageInvalidEndDate, Date = DateTime.Now });
                        await _db.SaveChangesAsync();

                        return BadRequest(new { message = "EndDate não pode ser anterior ao StartDate", endDate = dto.EndDate });
                    }

                    history.EndDate = dto.EndDate;
                }

                history.ModifiedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                string messageSuccess =
                    $"PATCH de DepartmentHistory concluído com sucesso. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogInformation(messageSuccess);
                _db.Logs.Add(new Log { Message = messageSuccess, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Ok(_mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                string messageDbError =
                    $"Erro ao atualizar (PATCH) DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogError(dbEx, messageDbError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageDbError} Detalhes: {dbEx.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                string messageUnexpectedError =
                    $"Erro inesperado no PATCH de DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogError(ex, messageUnexpectedError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageUnexpectedError} Detalhes: {ex.Message}",
                    Date = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao atualizar DepartmentHistory.");
            }
        }



        [HttpPost("{businessEntityId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateByEmployee(int businessEntityId, [FromBody] DepartmentHistoryDto dto)
        {
            string messageRequest =
                $"Received request to create DepartmentHistory by Employee. BusinessEntityID={businessEntityId}, DepartmentId={dto?.DepartmentId}, ShiftID={dto?.ShiftID}, StartDate={dto?.StartDate}.";
            _logger.LogInformation(messageRequest);
            _db.Logs.Add(new Log { Message = messageRequest, Date = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            if (dto is null)
            {
                string messageNoBody = "Creation failed: request body is null.";
                _logger.LogWarning(messageNoBody);
                _db.Logs.Add(new Log { Message = messageNoBody, Date = DateTime.UtcNow });
                await _db.SaveChangesAsync();

                return BadRequest(new { message = "Body is required" });
            }

            try
            {
                // 1) Validate Employee (FK)
                var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == businessEntityId);
                if (!employeeExists)
                {
                    string messageEmployeeNotFound =
                        $"Creation failed: Employee not found. BusinessEntityID={businessEntityId}.";
                    _logger.LogWarning(messageEmployeeNotFound);
                    _db.Logs.Add(new Log { Message = messageEmployeeNotFound, Date = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    return NotFound(new { message = "Employee não encontrado", businessEntityId });
                }

                // 2) Validate Department (FK)
                if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                {
                    string messageDeptOutOfRange =
                        $"Creation failed: DepartmentId out of short range. DepartmentId={dto.DepartmentId}.";
                    _logger.LogWarning(messageDeptOutOfRange);
                    _db.Logs.Add(new Log { Message = messageDeptOutOfRange, Date = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });
                }

                var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
                if (!deptExists)
                {
                    string messageDeptNotFound =
                        $"Creation failed: Department not found. DepartmentId={dto.DepartmentId}.";
                    _logger.LogWarning(messageDeptNotFound);
                    _db.Logs.Add(new Log { Message = messageDeptNotFound, Date = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });
                }

                // 3) Ensure StartDate is within SQL Server datetime range
                var minSqlDate = new DateTime(1753, 1, 1);
                if (dto.StartDate < minSqlDate)
                {
                    string messageStartDateInvalid =
                        $"Creation failed: StartDate invalid for SQL Server datetime. StartDate={dto.StartDate}.";
                    _logger.LogWarning(messageStartDateInvalid);
                    _db.Logs.Add(new Log { Message = messageStartDateInvalid, Date = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });
                }

                // 4) Map DTO → Model and set BusinessEntityID
                var history = _mapper.Map<DepartmentHistory>(dto);
                history.BusinessEntityID = businessEntityId;
                history.DepartmentID = (short)dto.DepartmentId;
                history.ModifiedDate = DateTime.UtcNow;

                // 5) Avoid duplicate of composite PK
                var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                    dh.BusinessEntityID == history.BusinessEntityID &&
                    dh.DepartmentID == history.DepartmentID &&
                    dh.ShiftID == history.ShiftID &&
                    dh.StartDate == history.StartDate);

                if (exists)
                {
                    string messageDup =
                        $"Creation failed: duplicate DepartmentHistory. BusinessEntityID={history.BusinessEntityID}, DepartmentID={history.DepartmentID}, ShiftID={history.ShiftID}, StartDate={history.StartDate}.";
                    _logger.LogWarning(messageDup);
                    _db.Logs.Add(new Log { Message = messageDup, Date = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    return Conflict(new
                    {
                        message = "Registo de DepartmentHistory já existe",
                        businessEntityId = history.BusinessEntityID,
                        departmentId = history.DepartmentID,
                        shiftId = history.ShiftID,
                        startDate = history.StartDate
                    });
                }

                // 6) Insert
                _db.DepartmentHistories.Add(history);
                await _db.SaveChangesAsync();

                string messageSuccess =
                    $"DepartmentHistory created successfully. BusinessEntityID={history.BusinessEntityID}, DepartmentID={history.DepartmentID}, ShiftID={history.ShiftID}, StartDate={history.StartDate}.";
                _logger.LogInformation(messageSuccess);
                _db.Logs.Add(new Log { Message = messageSuccess, Date = DateTime.UtcNow });
                await _db.SaveChangesAsync();

                // 7) Response with Location to composite GET
                return CreatedAtAction(nameof(Get),
                    new
                    {
                        businessEntityId = history.BusinessEntityID,
                        departmentId = history.DepartmentID,
                        shiftId = history.ShiftID,
                        startDate = history.StartDate.ToString("o") // ISO 8601
                    },
                    _mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                string messageDbError =
                    $"Error persisting DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={dto?.DepartmentId}, ShiftID={dto?.ShiftID}, StartDate={dto?.StartDate}.";
                _logger.LogError(dbEx, messageDbError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageDbError} Details: {dbEx.Message}",
                    Date = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                string messageUnexpectedError =
                    $"Unexpected error creating DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={dto?.DepartmentId}, ShiftID={dto?.ShiftID}, StartDate={dto?.StartDate}.";
                _logger.LogError(ex, messageUnexpectedError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageUnexpectedError} Details: {ex.Message}",
                    Date = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar DepartmentHistory.");
            }
        }



        [HttpDelete("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            string messageRequest =
                $"Received request to delete DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
            _logger.LogInformation(messageRequest);
            _db.Logs.Add(new Log { Message = messageRequest, Date = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            try
            {
                var history = await _db.DepartmentHistories
                    .FirstOrDefaultAsync(dh =>
                        dh.BusinessEntityID == businessEntityId &&
                        dh.DepartmentID == departmentId &&
                        dh.ShiftID == shiftId &&
                        dh.StartDate == startDate);

                if (history is null)
                {
                    string messageNotFound =
                        $"Delete failed: DepartmentHistory not found. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                    _logger.LogWarning(messageNotFound);
                    _db.Logs.Add(new Log { Message = messageNotFound, Date = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                _db.DepartmentHistories.Remove(history);
                await _db.SaveChangesAsync();

                string messageSuccess =
                    $"DepartmentHistory deleted successfully. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogInformation(messageSuccess);
                _db.Logs.Add(new Log { Message = messageSuccess, Date = DateTime.UtcNow });
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                string messageDbError =
                    $"Database error while deleting DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogError(dbEx, messageDbError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageDbError} Details: {dbEx.Message}",
                    Date = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                string messageUnexpectedError =
                    $"Unexpected error while deleting DepartmentHistory. BusinessEntityID={businessEntityId}, DepartmentID={departmentId}, ShiftID={shiftId}, StartDate={startDate}.";
                _logger.LogError(ex, messageUnexpectedError);

                _db.Logs.Add(new Log
                {
                    Message = $"{messageUnexpectedError} Details: {ex.Message}",
                    Date = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar DepartmentHistory.");
            }
        }

    }
}