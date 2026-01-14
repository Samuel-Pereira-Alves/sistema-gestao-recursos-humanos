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
                var histories = await GetAllHistoriesAsync(ct);

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
                return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
            }
        }

        private async Task<ActionResult> HandleUnexpectedDepartmentHistoryErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro inesperado no DepartmentHistory");
            AddLog($"Erro inesperado no DepartmentHistory");
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro ao processar o DepartmentHistory",
                detail: "Ocorreu um erro ao processar o DepartmentHistory.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        private async Task<List<DepartmentHistory>> GetAllHistoriesAsync(CancellationToken ct)
        {
            return await _db.DepartmentHistories
                .Include(dh => dh.Department)
                .AsNoTracking()
                .ToListAsync(ct);
        }


        [HttpGet("{businessEntityId:int}/{departmentId:int}/{shiftId:int}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<DepartmentHistoryDto>> Get(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            CancellationToken ct)
        {
            LogDepartmentHistoryGetRequest(businessEntityId, departmentId, shiftId, startDate);
            await _db.SaveChangesAsync(ct);

            try
            {
                var history = await GetDepartmentHistoryAsync(businessEntityId, departmentId, shiftId, startDate, ct);
                if (history is null)
                    return await HandleDepartmentHistoryNotFoundAsync(businessEntityId, departmentId, shiftId, startDate, ct);

                return Ok(_mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
            }
        }

        private void LogDepartmentHistoryGetRequest(int businessEntityId, short departmentId, byte shiftId, DateTime startDate)
        {
            _logger.LogInformation(
                "Pedido para obter DepartmentHistory. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                businessEntityId, departmentId, shiftId, startDate);

            AddLog($"Pedido para obter DepartmentHistory: {businessEntityId}/{departmentId}/{shiftId}/{startDate:o}");
        }

        private async Task<DepartmentHistory?> GetDepartmentHistoryAsync(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            CancellationToken ct)
        {
            return await _db.DepartmentHistories
                .Include(dh => dh.Department)
                .FirstOrDefaultAsync(dh =>
                    dh.BusinessEntityID == businessEntityId &&
                    dh.DepartmentID == departmentId &&
                    dh.ShiftID == shiftId &&
                    dh.StartDate == startDate, ct);
        }

        private async Task<ActionResult<DepartmentHistoryDto>> HandleDepartmentHistoryNotFoundAsync(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            CancellationToken ct)
        {
            _logger.LogWarning(
                "DepartmentHistory não encontrado. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
                businessEntityId, departmentId, shiftId, startDate);

            AddLog("DepartmentHistory não encontrado.");
            await _db.SaveChangesAsync(ct);

            return NotFound();
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
                // 1) Validar DTO e FKs
                var validationError = await ValidateCreateDtoAsync(dto, ct);
                if (validationError is IActionResult error)
                    return error;

                var deptIdShort = (short)dto.DepartmentId;

                // 2) Validar duplicado
                if (await IsDuplicateHistoryAsync(dto, deptIdShort, ct))
                    return ConflictDuplicate(dto);

                // 3) Criar registo com transação e fechar movimentos abertos
                var history = await CreateHistoryTransactionalAsync(dto, deptIdShort, ct);

                // 4) Logging final + retorno
                LogHistoryCreated(history);
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
                return await HandleDatabaseWriteErrorAsync(dbEx, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
            }
        }

        private async Task<IActionResult?> ValidateCreateDtoAsync(DepartmentHistoryDto dto, CancellationToken ct)
        {
            if (!await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID, ct))
                return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });

            if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
                return BadRequest(new { message = "DepartmentId fora do intervalo de short", departmentId = dto.DepartmentId });

            var deptIdShort = (short)dto.DepartmentId;

            if (!await _db.Departments.AnyAsync(d => d.DepartmentID == deptIdShort, ct))
                return NotFound(new { message = "Department não encontrado", departmentId = dto.DepartmentId });

            var minSqlDate = new DateTime(1753, 1, 1);
            if (dto.StartDate < minSqlDate)
                return BadRequest(new { message = "StartDate inválida para SQL Server datetime", startDate = dto.StartDate });

            return null;
        }

        private async Task<bool> IsDuplicateHistoryAsync(
            DepartmentHistoryDto dto,
            short deptIdShort,
            CancellationToken ct,
            int? overrideBusinessEntityId = null)
        {
            var beid = overrideBusinessEntityId ?? dto.BusinessEntityID;

            return await _db.DepartmentHistories.AnyAsync(dh =>
                dh.BusinessEntityID == beid &&
                dh.DepartmentID == deptIdShort &&
                dh.ShiftID == dto.ShiftID &&
                dh.StartDate == dto.StartDate, ct);
        }


        private IActionResult ConflictDuplicate(DepartmentHistoryDto dto)
        {
            return Conflict(new
            {
                message = "Registo de DepartmentHistory já existe",
                businessEntityId = dto.BusinessEntityID,
                departmentId = dto.DepartmentId,
                shiftId = dto.ShiftID,
                startDate = dto.StartDate
            });
        }

        private async Task<DepartmentHistory> CreateHistoryTransactionalAsync(
            DepartmentHistoryDto dto,
            short deptIdShort,
            CancellationToken ct)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var nowUtc = DateTime.UtcNow;

            // 1) Fechar movimentos abertos
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
                    movement.EndDate = dto.StartDate;
                    movement.ModifiedDate = nowUtc;
                }
            }

            // 2) Criar novo registo
            var history = _mapper.Map<DepartmentHistory>(dto);
            history.DepartmentID = deptIdShort;
            history.ModifiedDate = nowUtc;

            _db.DepartmentHistories.Add(history);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return history;
        }

        private void LogHistoryCreated(DepartmentHistory history)
        {
            _logger.LogInformation(
                "DepartmentHistory criado com sucesso. BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}",
                history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);

            AddLog($"DepartmentHistory criado: {history.BusinessEntityID}/{history.DepartmentID}/{history.ShiftID}/{history.StartDate:o}");
        }

        private async Task<IActionResult> HandleDatabaseWriteErrorAsync(DbUpdateException dbEx, CancellationToken ct)
        {
            _logger.LogError(dbEx, "Erro ao gravar DepartmentHistory.");
            AddLog($"Erro ao gravar DepartmentHistory: {dbEx.Message}");
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro ao gravar DepartmentHistory",
                detail: "Ocorreu um erro ao persistir o registo.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        [HttpPatch("{businessEntityId:int}/{departmentId:int}/{shiftId:int}/{startDate}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Patch(
            int businessEntityId,
            short departmentId,
            byte shiftId,
            DateTime startDate,
            [FromBody] DepartmentHistoryDto dto,
            CancellationToken ct)
        {
            _logger.LogInformation("Recebido PATCH para DepartmentHistory. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}", businessEntityId, departmentId, shiftId, startDate);
            AddLog($"PATCH DepartmentHistory: {businessEntityId}/{departmentId}/{shiftId}/{startDate:o}");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
                return BadRequest(new { message = "Body é obrigatório" });

            try
            {
                // 1. Obter registo
                var history = await GetDepartmentHistoryAsync(businessEntityId, departmentId, shiftId, startDate, ct);
                if (history is null)
                    return NotFound();

                // 2. Validar PATCH usando método já criado
                var validationError = ValidatePatch(dto, history);
                if (validationError is IActionResult err)
                    return err;

                // 3. Aplicar PATCH usando método já criado
                ApplyPatch(dto, history);

                // 4. Gravar alterações
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (DbUpdateException dbEx)
            {
                return await HandlePatchDatabaseErrorAsync(dbEx, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
            }
        }

        private IActionResult? ValidatePatch(DepartmentHistoryDto dto, DepartmentHistory existing)
        {
            if (dto.EndDate.HasValue && dto.EndDate.Value < existing.StartDate)
                return BadRequest(new { message = "EndDate não pode ser anterior ao StartDate", endDate = dto.EndDate });

            return null;
        }

        private void ApplyPatch(DepartmentHistoryDto dto, DepartmentHistory existing)
        {
            if (dto.EndDate.HasValue)
                existing.EndDate = dto.EndDate;

            existing.ModifiedDate = DateTime.UtcNow;
        }

        private async Task<IActionResult> HandlePatchDatabaseErrorAsync(DbUpdateException dbEx, CancellationToken ct)
        {
            _logger.LogError(dbEx, "Erro ao atualizar (PATCH) DepartmentHistory.");
            AddLog($"Erro ao atualizar (PATCH) DepartmentHistory: {dbEx.Message}");
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro ao atualizar",
                detail: "Erro ao atualizar DepartmentHistory.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // [HttpPost("{businessEntityId:int}")]
        // [Authorize(Roles = "admin")]
        // public async Task<IActionResult> CreateByEmployee(
        //     int businessEntityId,
        //     [FromBody] DepartmentHistoryDto dto,
        //     CancellationToken ct)
        // {
        //     _logger.LogInformation(
        //             "Received request to create DepartmentHistory by Employee. BEID={BusinessEntityId}, DepartmentId={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}",
        //             businessEntityId, dto?.DepartmentId, dto?.ShiftID, dto?.StartDate);

        //     AddLog($"CreateByEmployee DepartmentHistory: BEID={businessEntityId}");
        //     await _db.SaveChangesAsync(ct);

        //     if (dto is null)
        //         return BadRequest(new { message = "Body is required" });

        //     try
        //     {
        //         // 1) Validar FKs + DepartmentId range + StartDate
        //         var validationError = await ValidateCreateDtoAsync(dto, ct);
        //         if (validationError is IActionResult error)
        //             return error;

        //         var deptIdShort = (short)dto.DepartmentId;

        //         // 2) Verificar duplicado usando helper já criado
        //         if (await IsDuplicateHistoryAsync(dto, deptIdShort, ct, overrideBusinessEntityId: businessEntityId))
        //             return ConflictDuplicateForCreateByEmployee(businessEntityId, deptIdShort, dto);

        //         // 3) Criar registo (sem fechar movimentos abertos — comportamento original)
        //         var history = _mapper.Map<DepartmentHistory>(dto);
        //         _db.DepartmentHistories.Add(history);
        //         await _db.SaveChangesAsync(ct);

        //         _logger.LogInformation(
        //                 "DepartmentHistory created successfully (ByEmployee). BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}",
        //                 history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);

        //         AddLog("DepartmentHistory criado (ByEmployee).");
        //         await _db.SaveChangesAsync(ct);

        //         // 4) Resposta padrão CreatedAtAction
        //         return CreatedAtAction(nameof(Get),
        //             new
        //             {
        //                 businessEntityId = history.BusinessEntityID,
        //                 departmentId = history.DepartmentID,
        //                 shiftId = history.ShiftID,
        //                 startDate = history.StartDate
        //             },
        //             _mapper.Map<DepartmentHistoryDto>(history));
        //     }
        //     catch (DbUpdateException dbEx)
        //     {
        //         return await HandleCreateByEmployeeDatabaseErrorAsync(dbEx, ct);
        //     }
        //     catch (Exception ex)
        //     {
        //         return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
        //     }
        // }

        // private IActionResult ConflictDuplicateForCreateByEmployee(
        //     int businessEntityId,
        //     short departmentId,
        //     DepartmentHistoryDto dto)
        // {
        //     return Conflict(new
        //     {
        //         message = "Registo de DepartmentHistory já existe",
        //         businessEntityId,
        //         departmentId,
        //         shiftId = dto.ShiftID,
        //         startDate = dto.StartDate
        //     });
        // }

        // private async Task<IActionResult> HandleCreateByEmployeeDatabaseErrorAsync(DbUpdateException dbEx, CancellationToken ct)
        // {
        //     _logger.LogError(dbEx, "Erro ao gravar DepartmentHistory (ByEmployee).");
        //     AddLog($"Erro ao gravar DepartmentHistory (ByEmployee): {dbEx.Message}");
        //     await _db.SaveChangesAsync(ct);

        //     return Problem(
        //         title: "Erro ao gravar DepartmentHistory",
        //         detail: "Ocorreu um erro ao persistir o registo.",
        //         statusCode: StatusCodes.Status500InternalServerError);
        // }

        [HttpDelete("{businessEntityId:int}/{departmentId:int}/{shiftId:int}/{startDate}")]
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
                var history = await GetDepartmentHistoryAsync(businessEntityId, departmentId, shiftId, startDate, ct);
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
                return await HandleDeleteDbErrorAsync(dbEx, ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
            }
        }

        private async Task<IActionResult> HandleDeleteDbErrorAsync(DbUpdateException dbEx, CancellationToken ct)
        {
            _logger.LogError(dbEx, "Database error while deleting DepartmentHistory.");
            AddLog($"Database error while deleting DepartmentHistory: {dbEx.Message}");
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro ao eliminar DepartmentHistory",
                detail: "Erro ao eliminar o registo.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
//527