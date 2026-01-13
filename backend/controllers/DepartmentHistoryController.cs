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

        private void AddLog(string message) =>
            _db.Logs.Add(new Log { Message = message, Date = DateTime.UtcNow });

        // GET: api/v1/departmenthistory
        [HttpGet]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<List<DepartmentHistoryDto>>> GetAll(CancellationToken ct)
        {
            _logger.LogInformation("Recebido pedido para obter o histórico de Departamentos.");
            AddLog("Recebido pedido para obter o histórico de Departamentos.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var histories = await _db.DepartmentHistories
                    .Include(dh => dh.Department)
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (histories.Count == 0)
                {
                    _logger.LogWarning("Consulta de DepartmentHistories retornou 0 registos.");
                    AddLog("Consulta de DepartmentHistories retornou 0 registos.");
                    await _db.SaveChangesAsync(ct);
                }

                var dto = _mapper.Map<List<DepartmentHistoryDto>>(histories);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter DepartmentHistories.");
                AddLog($"Erro ao obter DepartmentHistories: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao consultar",
                    detail: "Ocorreu um erro ao obter DepartmentHistories.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // GET: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpGet("{businessEntityId:int}/{departmentId:short}/{shiftId:byte}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<DepartmentHistoryDto>> Get(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "Pedido para obter DepartmentHistory. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                businessEntityId, departmentId, shiftId, startDate);

            AddLog($"Pedido para obter DepartmentHistory: {businessEntityId}/{departmentId}/{shiftId}/{startDate:o}");
            await _db.SaveChangesAsync(ct);

            try
            {
                var history = await _db.DepartmentHistories
                    .Include(dh => dh.Department)
                    .FirstOrDefaultAsync(dh =>
                        dh.BusinessEntityID == businessEntityId &&
                        dh.DepartmentID == departmentId &&
                        dh.ShiftID == shiftId &&
                        dh.StartDate == startDate, ct);

                if (history is null)
                {
                    _logger.LogWarning("DepartmentHistory não encontrado. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                        businessEntityId, departmentId, shiftId, startDate);
                    AddLog("DepartmentHistory não encontrado.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                return Ok(_mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter DepartmentHistory.");
                AddLog($"Erro ao obter DepartmentHistory: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao consultar",
                    detail: "Ocorreu um erro ao obter DepartmentHistory.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // POST: api/v1/departmenthistory
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] DepartmentHistoryDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Recebido pedido para criar DepartmentHistory.");
            AddLog("Recebido pedido para criar DepartmentHistory.");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
                return BadRequest(new { message = "Body é obrigatório" });

            try
            {
                // 1) Validate FKs
                if (!await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID, ct))
                    return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });

                if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                    return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });

                var deptIdShort = (short)dto.DepartmentId;
                if (!await _db.Departments.AnyAsync(d => d.DepartmentID == deptIdShort, ct))
                    return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

                // 2) Validate StartDate (SQL Server datetime lower bound)
                var minSqlDate = new DateTime(1753, 1, 1);
                if (dto.StartDate < minSqlDate)
                    return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });

                // 3) Avoid duplicate of composite PK
                var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                    dh.BusinessEntityID == dto.BusinessEntityID &&
                    dh.DepartmentID == deptIdShort &&
                    dh.ShiftID == dto.ShiftID &&
                    dh.StartDate == dto.StartDate, ct);

                if (exists)
                    return Conflict(new
                    {
                        message = "Registo de DepartmentHistory já existe",
                        businessEntityId = dto.BusinessEntityID,
                        departmentId = dto.DepartmentId,
                        shiftId = dto.ShiftID,
                        startDate = dto.StartDate
                    });

                // 4) Transaction: close open movements + insert new one
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                // 4.1) Close previous open movements (EndDate == null)
                var nowUtc = DateTime.UtcNow;
                var openMovements = await _db.DepartmentHistories
                    .Where(dh => dh.BusinessEntityID == dto.BusinessEntityID && dh.EndDate == null)
                    .ToListAsync(ct);

                if (openMovements.Count > 0)
                {
                    _logger.LogInformation("Encontrados {Count} movimentos abertos para encerrar. BusinessEntityID={BEID}",
                        openMovements.Count, dto.BusinessEntityID);
                    AddLog($"Encontrados {openMovements.Count} movimentos abertos para encerrar. BEID={dto.BusinessEntityID}");

                    foreach (var movement in openMovements)
                    {
                        movement.EndDate = dto.StartDate;   // encerra no início do novo
                        movement.ModifiedDate = nowUtc;
                    }
                }

                // 4.2) Create new history
                var history = _mapper.Map<DepartmentHistory>(dto);
                history.DepartmentID = deptIdShort;   // ensure short cast
                history.ModifiedDate = nowUtc;

                _db.DepartmentHistories.Add(history);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("DepartmentHistory criado com sucesso. BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}",
                    history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);
                AddLog($"DepartmentHistory criado: {history.BusinessEntityID}/{history.DepartmentID}/{history.ShiftID}/{history.StartDate:o}");
                await _db.SaveChangesAsync(ct);

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
                _logger.LogError(dbEx, "Erro ao gravar DepartmentHistory.");
                AddLog($"Erro ao gravar DepartmentHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao gravar DepartmentHistory",
                    detail: "Ocorreu um erro ao persistir o registo.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar DepartmentHistory.");
                AddLog($"Erro inesperado ao criar DepartmentHistory: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao criar",
                    detail: "Ocorreu um erro ao criar DepartmentHistory.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // PATCH: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpPatch("{businessEntityId:int}/{departmentId:short}/{shiftId:byte}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Patch(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            [FromBody] DepartmentHistoryDto dto,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "Recebido PATCH para DepartmentHistory. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                businessEntityId, departmentId, shiftId, startDate);
            AddLog($"PATCH DepartmentHistory: {businessEntityId}/{departmentId}/{shiftId}/{startDate:o}");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
                return BadRequest(new { message = "Body é obrigatório" });

            try
            {
                var history = await _db.DepartmentHistories.FirstOrDefaultAsync(dh =>
                    dh.BusinessEntityID == businessEntityId &&
                    dh.DepartmentID == departmentId &&
                    dh.ShiftID == shiftId &&
                    dh.StartDate == startDate, ct);

                if (history is null)
                    return NotFound();

                if (dto.EndDate.HasValue && dto.EndDate.Value < history.StartDate)
                    return BadRequest(new { message = "EndDate não pode ser anterior ao StartDate", endDate = dto.EndDate });

                if (dto.EndDate.HasValue)
                    history.EndDate = dto.EndDate;

                history.ModifiedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                var outDto = _mapper.Map<DepartmentHistoryDto>(history);
                return Ok(outDto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao atualizar (PATCH) DepartmentHistory.");
                AddLog($"Erro ao atualizar (PATCH) DepartmentHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao atualizar",
                    detail: "Erro ao atualizar DepartmentHistory.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no PATCH de DepartmentHistory.");
                AddLog($"Erro inesperado no PATCH de DepartmentHistory: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao atualizar",
                    detail: "Ocorreu um erro ao atualizar DepartmentHistory.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // POST: api/v1/departmenthistory/{businessEntityId}
        [HttpPost("{businessEntityId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateByEmployee(
            int businessEntityId,
            [FromBody] DepartmentHistoryDto dto,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "Received request to create DepartmentHistory by Employee. BEID={BusinessEntityId}, DepartmentId={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                businessEntityId, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);
            AddLog($"CreateByEmployee DepartmentHistory: BEID={businessEntityId}");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
                return BadRequest(new { message = "Body is required" });

            try
            {
                // 1) Validate Employee (FK)
                if (!await _db.Employees.AnyAsync(e => e.BusinessEntityID == businessEntityId, ct))
                    return NotFound(new { message = "Employee não encontrado", businessEntityId });

                // 2) Validate Department (FK)
                if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                    return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });

                var deptIdShort = (short)dto.DepartmentId;
                if (!await _db.Departments.AnyAsync(d => d.DepartmentID == deptIdShort, ct))
                    return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

                // 3) Ensure StartDate is within SQL Server datetime range
                var minSqlDate = new DateTime(1753, 1, 1);
                if (dto.StartDate < minSqlDate)
                    return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });

                // 4) Map DTO → Model and set BusinessEntityID
                var history = _mapper.Map<DepartmentHistory>(dto);
                history.BusinessEntityID = businessEntityId;
                history.DepartmentID = deptIdShort;
                history.ModifiedDate = DateTime.UtcNow;

                // 5) Avoid duplicate of composite PK
                var exists = await _db.DepartmentHistories.AnyAsync(dh =>
                    dh.BusinessEntityID == history.BusinessEntityID &&
                    dh.DepartmentID == history.DepartmentID &&
                    dh.ShiftID == history.ShiftID &&
                    dh.StartDate == history.StartDate, ct);

                if (exists)
                    return Conflict(new
                    {
                        message = "Registo de DepartmentHistory já existe",
                        businessEntityId = history.BusinessEntityID,
                        departmentId = history.DepartmentID,
                        shiftId = history.ShiftID,
                        startDate = history.StartDate
                    });

                // 6) Insert
                _db.DepartmentHistories.Add(history);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "DepartmentHistory created successfully (ByEmployee). BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}",
                    history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);
                AddLog("DepartmentHistory criado (ByEmployee).");
                await _db.SaveChangesAsync(ct);

                // 7) Response with Location to composite GET
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
                _logger.LogError(dbEx, "Erro ao gravar DepartmentHistory (ByEmployee).");
                AddLog($"Erro ao gravar DepartmentHistory (ByEmployee): {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao gravar DepartmentHistory",
                    detail: "Ocorreu um erro ao persistir o registo.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating DepartmentHistory (ByEmployee).");
                AddLog($"Unexpected error creating DepartmentHistory (ByEmployee): {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao criar",
                    detail: "Ocorreu um erro ao criar DepartmentHistory.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE: api/v1/departmenthistory/{businessEntityId}/{departmentId}/{shiftId}/{startDate}
        [HttpDelete("{businessEntityId:int}/{departmentId:short}/{shiftId:byte}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "Received request to delete DepartmentHistory. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                businessEntityId, departmentId, shiftId, startDate);
            AddLog("Received request to delete DepartmentHistory.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var history = await _db.DepartmentHistories.FirstOrDefaultAsync(dh =>
                    dh.BusinessEntityID == businessEntityId &&
                    dh.DepartmentID == departmentId &&
                    dh.ShiftID == shiftId &&
                    dh.StartDate == startDate, ct);

                if (history is null)
                    return NotFound();

                _db.DepartmentHistories.Remove(history);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "DepartmentHistory deleted successfully. BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}",
                    businessEntityId, departmentId, shiftId, startDate);
                AddLog("DepartmentHistory eliminado com sucesso.");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while deleting DepartmentHistory.");
                AddLog($"Database error while deleting DepartmentHistory: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao eliminar DepartmentHistory",
                    detail: "Erro ao eliminar o registo.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting DepartmentHistory.");
                AddLog($"Unexpected error while deleting DepartmentHistory: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao eliminar",
                    detail: "Ocorreu um erro ao eliminar DepartmentHistory.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}