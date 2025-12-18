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
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Recebido pedido para obter DepartmentHistories.");

            try
            {
                var histories = await _db.DepartmentHistories
                    .Include(dh => dh.Department)
                    .ToListAsync();

                if (histories.Count == 0)
                {
                    _logger.LogWarning("Consulta de DepartmentHistories retornou 0 registos.");
                }

                var dto = _mapper.Map<List<DepartmentHistoryDto>>(histories);

                _logger.LogInformation(
                    "Consulta de DepartmentHistories concluída com sucesso. Registos={Count}.",
                    dto?.Count ?? 0);

                return Ok(dto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro de base de dados ao obter DepartmentHistories.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao consultar DepartmentHistories.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao obter DepartmentHistories.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter DepartmentHistories.");
            }
        }

        // GET: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        // [HttpGet("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> Get(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        // {
        //     var history = await _db.DepartmentHistories
        //         .Include(dh => dh.Department)
        //         .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
        //                                 && dh.DepartmentID == departmentId
        //                                 && dh.ShiftID == shiftId
        //                                 && dh.StartDate == startDate);

        //     if (history == null) return NotFound();
        //     var dto = _mapper.Map<DepartmentHistoryDto>(history);
        //     return Ok(dto);
        // }

        [HttpGet("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Get(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            _logger.LogInformation(
                "Pedido para obter DepartmentHistory recebido. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                businessEntityId, departmentId, shiftId, startDate);

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
                    _logger.LogWarning(
                        "DepartmentHistory não encontrado. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                        businessEntityId, departmentId, shiftId, startDate);

                    return NotFound();
                }

                var dto = _mapper.Map<DepartmentHistoryDto>(history);

                _logger.LogInformation(
                    "DepartmentHistory obtido com sucesso. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado ao obter DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Ocorreu um erro ao obter DepartmentHistory.");
            }
        }

        // POST: api/v1/departmenthistory
        // [HttpPost]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> Create(DepartmentHistoryDto dto)
        // {
        //     // 1) Validar Employee (FK)
        //     var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID);
        //     if (!employeeExists)
        //         return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });

        //     // 2) Validar Department (FK)
        //     if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
        //         return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });

        //     var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
        //     if (!deptExists)
        //         return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

        //     // 3) Validar StartDate no range de SQL Server datetime
        //     var minSqlDate = new DateTime(1753, 1, 1);
        //     if (dto.StartDate < minSqlDate)
        //         return BadRequest(new { message = "StartDate inválida para SQL Server datetime", dto.StartDate });

        //     // 4) Evitar duplicado de PK composta (BusinessEntityID, DepartmentID, ShiftID, StartDate)
        //     var exists = await _db.DepartmentHistories.AnyAsync(dh =>
        //         dh.BusinessEntityID == dto.BusinessEntityID &&
        //         dh.DepartmentID == (short)dto.DepartmentId &&
        //         dh.ShiftID == dto.ShiftID &&
        //         dh.StartDate == dto.StartDate);

        //     if (exists)
        //         return Conflict(new
        //         {
        //             message = "Registo de DepartmentHistory já existe",
        //             businessEntityId = dto.BusinessEntityID,
        //             departmentId = dto.DepartmentId,
        //             shiftId = dto.ShiftID,
        //             startDate = dto.StartDate
        //         });

        //     var lastMovements = await _db.DepartmentHistories
        //                                 .Where(dh => dh.BusinessEntityID == dto.BusinessEntityID &&
        //                                 dh.EndDate == null
        //                                 ).ToListAsync();

        //     foreach (var movement in lastMovements)
        //     {
        //         movement.EndDate = DateTime.Now;
        //         movement.ModifiedDate = DateTime.Now;
        //     }
        //     // 5) Mapear e inserir
        //     var history = _mapper.Map<DepartmentHistory>(dto);
        //     history.ModifiedDate = DateTime.Now;

        //     _db.DepartmentHistories.Add(history);
        //     await _db.SaveChangesAsync();

        //     return CreatedAtAction(nameof(Get),
        //         new
        //         {
        //             businessEntityId = history.BusinessEntityID,
        //             departmentId = history.DepartmentID,
        //             shiftId = history.ShiftID,
        //             startDate = history.StartDate
        //         },
        //         _mapper.Map<DepartmentHistoryDto>(history));
        // }


        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(DepartmentHistoryDto dto)
        {
            _logger.LogInformation(
                "Recebido pedido para criar DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentId={DepartmentId}, ShiftID={ShiftID}, StartDate={StartDate}.",
                dto?.BusinessEntityID, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

            if (dto is null)
            {
                _logger.LogWarning("Body não enviado no pedido de criação de DepartmentHistory.");
                return BadRequest(new { message = "Body é obrigatório" });
            }

            try
            {
                // 1) Validar Employee (FK)
                var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID);
                if (!employeeExists)
                {
                    _logger.LogWarning("Criação falhou: Employee não encontrado. BusinessEntityID={BusinessEntityID}.", dto.BusinessEntityID);
                    return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });
                }

                // 2) Validar Department (FK)
                if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                {
                    _logger.LogWarning("Criação falhou: DepartmentId fora do intervalo de short. DepartmentId={DepartmentId}.", dto.DepartmentId);
                    return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });
                }

                var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
                if (!deptExists)
                {
                    _logger.LogWarning("Criação falhou: Department não encontrado. DepartmentId={DepartmentId}.", dto.DepartmentId);
                    return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });
                }

                // 3) Validar StartDate no range de SQL Server datetime
                var minSqlDate = new DateTime(1753, 1, 1);
                if (dto.StartDate < minSqlDate)
                {
                    _logger.LogWarning("Criação falhou: StartDate inválida para SQL Server datetime. StartDate={StartDate}.", dto.StartDate);
                    return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });
                }

                // 4) Evitar duplicado de PK composta (BusinessEntityID, DepartmentID, ShiftID, StartDate)
                var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                    dh.BusinessEntityID == dto.BusinessEntityID &&
                    dh.DepartmentID == (short)dto.DepartmentId &&
                    dh.ShiftID == dto.ShiftID &&
                    dh.StartDate == dto.StartDate);

                if (exists)
                {
                    _logger.LogWarning(
                        "Criação falhou: DepartmentHistory duplicado. BusinessEntityID={BusinessEntityID}, DepartmentId={DepartmentId}, ShiftID={ShiftID}, StartDate={StartDate}.",
                        dto.BusinessEntityID, dto.DepartmentId, dto.ShiftID, dto.StartDate);

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
                    _logger.LogInformation(
                        "Encontrados {Count} movimentos anteriores com EndDate=null para encerrar. BusinessEntityID={BusinessEntityID}.",
                        lastMovements.Count, dto.BusinessEntityID);

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

                _logger.LogInformation(
                    "DepartmentHistory criado com sucesso. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);

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
                _logger.LogError(dbEx,
                    "Erro ao gravar DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentId={DepartmentId}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    dto?.BusinessEntityID, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado ao criar DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentId={DepartmentId}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    dto?.BusinessEntityID, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

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

        // PATCH: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        // [HttpPatch("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> Patch(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        // {
        //     var history = await _db.DepartmentHistories
        //         .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
        //                                 && dh.DepartmentID == departmentId
        //                                 && dh.ShiftID == shiftId
        //                                 && dh.StartDate == startDate);

        //     if (history == null) return NotFound();

        //     if (dto.EndDate.HasValue) history.EndDate = dto.EndDate;

        //     history.ModifiedDate = DateTime.Now;
        //     await _db.SaveChangesAsync();

        //     return Ok(_mapper.Map<DepartmentHistoryDto>(history));
        // }

        [HttpPatch("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Patch(int businessEntityId, short departmentId, byte shiftId, DateTime startDate, DepartmentHistoryDto dto)
        {
            _logger.LogInformation(
                "Recebido pedido para atualizar (PATCH) DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                businessEntityId, departmentId, shiftId, startDate);

            if (dto is null)
            {
                _logger.LogWarning("Body não enviado no PATCH de DepartmentHistory.");
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
                    _logger.LogWarning(
                        "PATCH falhou: DepartmentHistory não encontrado. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                        businessEntityId, departmentId, shiftId, startDate);

                    return NotFound();
                }

                // Aplicar alterações permitidas (PATCH parcial)
                if (dto.EndDate.HasValue)
                {
                    // Validação simples: EndDate >= StartDate
                    if (dto.EndDate.Value < history.StartDate)
                    {
                        _logger.LogWarning(
                            "PATCH inválido: EndDate anterior ao StartDate. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}, EndDate={EndDate}.",
                            businessEntityId, departmentId, shiftId, startDate, dto.EndDate);

                        return BadRequest(new { message = "EndDate não pode ser anterior ao StartDate", endDate = dto.EndDate });
                    }

                    history.EndDate = dto.EndDate;
                }

                history.ModifiedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "PATCH de DepartmentHistory concluído com sucesso. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return Ok(_mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Erro ao atualizar (PATCH) DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado no PATCH de DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao atualizar DepartmentHistory.");
            }
        }

        // [HttpPost("{businessEntityId}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> CreateByEmployee(
        //     int businessEntityId,
        //     [FromBody] DepartmentHistoryDto dto)
        // {
        //     // 1) Validar existência do Employee (FK)
        //     var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == businessEntityId);
        //     if (!employeeExists)
        //         return NotFound(new { message = "Employee não encontrado", businessEntityId });

        //     // 2) Validar existência do Department (FK) — opcional mas recomendado
        //     if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
        //         return BadRequest(new { message = "DepartmentId fora do intervalo de short", dto.DepartmentId });

        //     var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
        //     if (!deptExists)
        //         return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

        //     // 3) Garantir StartDate dentro do range de datetime (>= 1753-01-01)
        //     var minSqlDate = new DateTime(1753, 1, 1);
        //     if (dto.StartDate < minSqlDate)
        //         return BadRequest(new { message = "StartDate inválida para SQL Server datetime", dto.StartDate });

        //     // 4) Mapear DTO → Modelo e fixar BusinessEntityID
        //     var history = _mapper.Map<DepartmentHistory>(dto);
        //     history.BusinessEntityID = businessEntityId;
        //     history.DepartmentID = (short)dto.DepartmentId;
        //     history.ModifiedDate = DateTime.Now;

        //     // 5) Evitar duplicado de PK composta (BusinessEntityID, DepartmentID, ShiftID, StartDate)
        //     var exists = await _db.DepartmentHistories.AnyAsync(dh =>
        //         dh.BusinessEntityID == history.BusinessEntityID &&
        //         dh.DepartmentID == history.DepartmentID &&
        //         dh.ShiftID == history.ShiftID &&
        //         dh.StartDate == history.StartDate);

        //     if (exists)
        //         return Conflict(new
        //         {
        //             message = "Registo de DepartmentHistory já existe",
        //             businessEntityId = history.BusinessEntityID,
        //             departmentId = history.DepartmentID,
        //             shiftId = history.ShiftID,
        //             startDate = history.StartDate
        //         });

        //     // 6) Inserir
        //     _db.DepartmentHistories.Add(history);
        //     await _db.SaveChangesAsync();

        //     // 7) Responder com Location para GET composto
        //     return CreatedAtAction(nameof(Get),
        //         new
        //         {
        //             businessEntityId = history.BusinessEntityID,
        //             departmentId = history.DepartmentID,
        //             shiftId = history.ShiftID,
        //             startDate = history.StartDate.ToString("o") // ISO 8601 para segurança
        //         },
        //         _mapper.Map<DepartmentHistoryDto>(history));
        // }

        [HttpPost("{businessEntityId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateByEmployee(int businessEntityId, [FromBody] DepartmentHistoryDto dto)
        {
            _logger.LogInformation(
                "Received request to create DepartmentHistory by Employee. BusinessEntityID={BusinessEntityID}, DepartmentId={DepartmentId}, ShiftID={ShiftID}, StartDate={StartDate}.",
                businessEntityId, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

            if (dto is null)
            {
                _logger.LogWarning("Creation failed: request body is null.");
                return BadRequest(new { message = "Body is required" });
            }

            try
            {
                // 1) Validate Employee (FK)
                var employeeExists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == businessEntityId);
                if (!employeeExists)
                {
                    _logger.LogWarning("Creation failed: Employee not found. BusinessEntityID={BusinessEntityID}.", businessEntityId);
                    return NotFound(new { message = "Employee não encontrado", businessEntityId });
                }

                // 2) Validate Department (FK)
                if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                {
                    _logger.LogWarning("Creation failed: DepartmentId out of short range. DepartmentId={DepartmentId}.", dto.DepartmentId);
                    return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });
                }

                var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentID == (short)dto.DepartmentId);
                if (!deptExists)
                {
                    _logger.LogWarning("Creation failed: Department not found. DepartmentId={DepartmentId}.", dto.DepartmentId);
                    return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });
                }

                // 3) Ensure StartDate is within SQL Server datetime range
                var minSqlDate = new DateTime(1753, 1, 1);
                if (dto.StartDate < minSqlDate)
                {
                    _logger.LogWarning("Creation failed: StartDate invalid for SQL Server datetime. StartDate={StartDate}.", dto.StartDate);
                    return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });
                }

                // 4) Map DTO → Model and set BusinessEntityID
                var history = _mapper.Map<DepartmentHistory>(dto);
                history.BusinessEntityID = businessEntityId;
                history.DepartmentID = (short)dto.DepartmentId;
                history.ModifiedDate = DateTime.UtcNow;

                // 5) Avoid duplicate of composite PK (BusinessEntityID, DepartmentID, ShiftID, StartDate)
                var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                    dh.BusinessEntityID == history.BusinessEntityID &&
                    dh.DepartmentID == history.DepartmentID &&
                    dh.ShiftID == history.ShiftID &&
                    dh.StartDate == history.StartDate);

                if (exists)
                {
                    _logger.LogWarning(
                        "Creation failed: duplicate DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                        history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);

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

                _logger.LogInformation(
                    "DepartmentHistory created successfully. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);

                // 7) Response with Location to composite GET
                return CreatedAtAction(nameof(Get),
                    new
                    {
                        businessEntityId = history.BusinessEntityID,
                        departmentId = history.DepartmentID,
                        shiftId = history.ShiftID,
                        startDate = history.StartDate.ToString("o") // ISO 8601 for safety
                    },
                    _mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Error persisting DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error creating DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar DepartmentHistory.");
            }
        }

        // [HttpDelete("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> Delete(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        // {
        //     var history = await _db.DepartmentHistories
        //         .FirstOrDefaultAsync(dh => dh.BusinessEntityID == businessEntityId
        //                                 && dh.DepartmentID == departmentId
        //                                 && dh.ShiftID == shiftId
        //                                 && dh.StartDate == startDate);

        //     if (history == null) return NotFound();

        //     _db.DepartmentHistories.Remove(history);
        //     await _db.SaveChangesAsync();

        //     return NoContent();
        // }

        [HttpDelete("{businessEntityId}/{departmentId}/{shiftId}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            _logger.LogInformation(
                "Received request to delete DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                businessEntityId, departmentId, shiftId, startDate);

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
                    _logger.LogWarning(
                        "Delete failed: DepartmentHistory not found. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                        businessEntityId, departmentId, shiftId, startDate);

                    return NotFound();
                }

                _db.DepartmentHistories.Remove(history);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "DepartmentHistory deleted successfully. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Database error while deleting DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar DepartmentHistory.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error while deleting DepartmentHistory. BusinessEntityID={BusinessEntityID}, DepartmentID={DepartmentID}, ShiftID={ShiftID}, StartDate={StartDate}.",
                    businessEntityId, departmentId, shiftId, startDate);

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar DepartmentHistory.");
            }
        }
    }
}