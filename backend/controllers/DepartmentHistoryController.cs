using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.services;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DepartmentHistoryController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentHistoryController> _logger;
        private readonly IAppLogService _appLog;
        public DepartmentHistoryController(AdventureWorksContext db, IMapper mapper, ILogger<DepartmentHistoryController> logger, IAppLogService appLog)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _appLog = appLog;
        }

        // GET: api/v1/departmenthistory
        [HttpGet]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<List<DepartmentHistoryDto>>> GetAll(CancellationToken ct)
        {
            _logger.LogInformation("Recebido pedido para obter o histórico de Departamentos.");
            await _appLog.InfoAsync("Recebido pedido para obter o histórico de Departamentos.");

            try
            {
                var histories = await GetAllHistoriesAsync(ct);

                if (histories.Count == 0)
                {
                    _logger.LogWarning("Consulta de DepartmentHistories retornou 0 registos.");
                    await _appLog.WarnAsync("Consulta de DepartmentHistories retornou 0 registos.");
                }

                var dto = _mapper.Map<List<DepartmentHistoryDto>>(histories);
                _logger.LogInformation("Pedido realizado com sucesso.");
                await _appLog.InfoAsync("Pedido realizado com sucesso.");

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
            await _appLog.ErrorAsync($"Erro inesperado no DepartmentHistory", ex);

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
            _logger.LogInformation("Pedido para obter DepartmentHistory. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}", businessEntityId, departmentId, shiftId, startDate);
            await _appLog.InfoAsync($"Pedido para obter DepartmentHistory: {businessEntityId}/{departmentId}/{shiftId}/{startDate:o}");

            try
            {
                var history = await GetDepartmentHistoryAsync(businessEntityId, departmentId, shiftId, startDate, ct);
                if (history is null)
                    return await HandleDepartmentHistoryNotFoundAsync(businessEntityId, departmentId, shiftId, startDate, ct);

                _logger.LogInformation("Pedido realizado com sucesso.");
                await _appLog.InfoAsync("Pedido realizado com sucesso.");
                return Ok(_mapper.Map<DepartmentHistoryDto>(history));
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedDepartmentHistoryErrorAsync(ex, ct);
            }
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
            _logger.LogWarning("DepartmentHistory não encontrado. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}", businessEntityId, departmentId, shiftId, startDate);
            await _appLog.WarnAsync("DepartmentHistory não encontrado.");

            return NotFound();
        }

        // POST: api/v1/departmenthistory
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] DepartmentHistoryDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Recebido pedido para criar DepartmentHistory.");
            await _appLog.InfoAsync("Recebido pedido para criar DepartmentHistory.");

            if (dto is null)
            {
                _logger.LogWarning("Pedido de create inválido: body nulo ou campos em branco.");
                await _appLog.WarnAsync("Pedido de create inválido: body nulo ou campos em branco.");
                return BadRequest(new { message = "Body é obrigatório" });
            }

            try
            {
                // 1) Validar DTO e FKs
                var validationError = await ValidateCreateDtoAsync(dto, ct);
                if (validationError is IActionResult error)
                    return error;


                var dateRule = await ValidateEndDateRuleAsync(dto.EndDate, dto.StartDate, "CREATE");
                if (dateRule is IActionResult dateErr)
                    return dateErr;


                var deptIdShort = (short)dto.DepartmentId;

                // 2) Validar duplicado
                if (await IsDuplicateHistoryAsync(dto, deptIdShort, ct))
                    return ConflictDuplicate(dto);



                // 3) Criar registo com transação e fechar movimentos abertos
                var history = await CreateHistoryTransactionalAsync(dto, deptIdShort, ct);

                // 4) Logging final + retorno
                _logger.LogInformation("DepartmentHistory criado com sucesso. BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}", history.BusinessEntityID, history.DepartmentID, history.ShiftID, history.StartDate);
                await _appLog.InfoAsync($"DepartmentHistory criado: {history.BusinessEntityID}/{history.DepartmentID}/{history.ShiftID}/{history.StartDate:o}");

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
            {
                _logger.LogWarning($"Nenhum employee encontrado para BEID={dto.BusinessEntityID}");
                await _appLog.WarnAsync($"Nenhum employee encontrado para BEID={dto.BusinessEntityID}");
                return NotFound();
            }
            if (dto.DepartmentId < short.MinValue || dto.DepartmentId > short.MaxValue)
            {
                _logger.LogWarning("Pedido inválido: Departamento Inválido (fora do intervalo de short).");
                await _appLog.WarnAsync("Pedido inválido: Departamento Inválido (fora do intervalo de short).");
                return BadRequest();
            }
            var deptIdShort = (short)dto.DepartmentId;

            if (!await _db.Departments.AnyAsync(d => d.DepartmentID == deptIdShort, ct))
            {
                _logger.LogWarning($"Departamento {dto.DepartmentId} não encontrado");
                await _appLog.WarnAsync($"Departamento {dto.DepartmentId} não encontrado");
                return NotFound();
            }

            var minSqlDate = new DateTime(1753, 1, 1);
            if (dto.StartDate < minSqlDate)
            {
                _logger.LogWarning($"StartDate({dto.StartDate}) inválida para SQL Server datetime");
                await _appLog.WarnAsync($"StartDate({dto.StartDate}) inválida para SQL Server datetime");
                return BadRequest();
            }
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
                .Where(dh => dh.BusinessEntityID == dto.BusinessEntityID && dh.EndDate == null && dh.StartDate <= dto.StartDate)
                .ToListAsync(ct);

            if (openMovements.Count > 0)
            {
                _logger.LogInformation("Encontrados {Count} movimentos abertos para encerrar. BusinessEntityID={BEID}",
                    openMovements.Count, dto.BusinessEntityID);
                await _appLog.InfoAsync($"Encontrados {openMovements.Count} movimentos abertos para encerrar. BEID={dto.BusinessEntityID}");

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

        private async Task<IActionResult> HandleDatabaseWriteErrorAsync(DbUpdateException dbEx, CancellationToken ct)
        {
            _logger.LogError(dbEx, "Erro ao gravar DepartmentHistory.");
            await _appLog.ErrorAsync($"Erro ao gravar DepartmentHistory: {dbEx.Message}", dbEx);

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
            await _appLog.InfoAsync($"PATCH DepartmentHistory: {businessEntityId}/{departmentId}/{shiftId}/{startDate:o}");

            if (dto is null)
            {
                _logger.LogWarning("Pedido inválido: body nulo ou campos em branco.");
                await _appLog.WarnAsync("Pedido inválido: body nulo ou campos em branco.");
                return BadRequest(new { message = "Body é obrigatório" });
            }

            try
            {
                // 1. Obter registo
                var history = await GetDepartmentHistoryAsync(businessEntityId, departmentId, shiftId, startDate, ct);
                if (history is null)
                {
                    _logger.LogWarning(
                        "DepartmentHistory não encontrado. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}", businessEntityId, departmentId, shiftId, startDate);
                    await _appLog.WarnAsync("DepartmentHistory não encontrado.");

                    return NotFound();
                }

                // 2. Validar PATCH usando método já criado
                var validation = await ValidateEndDateRuleAsync(dto.EndDate, history.StartDate, "PATCH");
                if (validation is IActionResult err)
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

        private async Task<IActionResult?> ValidateEndDateRuleAsync(
            DateTime? endDate,
            DateTime startDate,
            string context
        )
        {

            if (!endDate.HasValue) return null;

            if (endDate.Value < startDate)
            {
                _logger.LogInformation(
                    "Validação falhou ({Context}): EndDate({End:o}) < StartDate({Start:o})",
                    context, endDate.Value, startDate);
                await _appLog.ErrorAsync("Pedido Falhou: EndDate não pode ser anterior ao StartDate", new DbUpdateException());

                var details = new ProblemDetails
                {
                    Title = "Movimento de Departamentos Inválido - Data de Fim anterior à Data de Início",
                    Detail = "Data Final não pode ser anterior à data inicial.",
                    Status = StatusCodes.Status409Conflict
                };

                // Usa ObjectResult para manter StatusCode coerente com ProblemDetails
                return new ObjectResult(details) { StatusCode = StatusCodes.Status409Conflict };
                // ou: return Conflict(details);
            }
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
            await _appLog.ErrorAsync($"Erro ao atualizar (PATCH) DepartmentHistory: {dbEx.Message}", dbEx);

            return Problem(
                title: "Erro ao atualizar",
                detail: "Erro ao atualizar DepartmentHistory.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

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
            await _appLog.InfoAsync("Received request to delete DepartmentHistory.");

            try
            {
                var history = await GetDepartmentHistoryAsync(businessEntityId, departmentId, shiftId, startDate, ct);
                if (history is null)
                {
                    _logger.LogWarning(
                        "DepartmentHistory não encontrado. BEID={BusinessEntityId}, DeptID={DepartmentId}, ShiftID={ShiftId}, StartDate={StartDate:o}", businessEntityId, departmentId, shiftId, startDate);
                    await _appLog.WarnAsync("DepartmentHistory não encontrado.");

                    return NotFound();
                }

                _db.DepartmentHistories.Remove(history);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                        "DepartmentHistory deleted successfully. BEID={BEID}, DeptID={DeptID}, ShiftID={ShiftID}, StartDate={StartDate:o}",
                        businessEntityId, departmentId, shiftId, startDate);

                await _appLog.InfoAsync("DepartmentHistory eliminado com sucesso.");
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
            await _appLog.ErrorAsync($"Database error while deleting DepartmentHistory: {dbEx.Message}", dbEx);

            return Problem(
                title: "Erro ao eliminar DepartmentHistory",
                detail: "Erro ao eliminar o registo.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        [HttpGet("departments/paged")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<PagedResult<PayHistoryDto>>> GetAllDepartmentsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            const int MaxPageSize = 200;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            // Base: só PayHistories
            var q = _db.DepartmentHistories
                .AsNoTracking()
                .Include(p => p.Employee) // apenas para projetar Person
                    .ThenInclude(e => e!.Person)
                .AsQueryable();

            // 🔎 Pesquisa simples, cobrindo:
            // - Nome (First/Last)
            // - ID do colaborador (numérico)
            // - Valor (rate) por string
            // - Data por string (YYYY-MM-DD ou formato corrente)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var like = $"%{s}%";
                var isNumeric = int.TryParse(s, out var idNumber);

                // Se tiveres collation CI_AI na BD, podes usar EF.Functions.Collate para acentos;
                // caso contrário, EF.Functions.Like simples fica dependente do collation da coluna.
                q = q.Where(p =>
                    EF.Functions.Like(p.Employee!.Person!.FirstName!, like) ||
                    EF.Functions.Like(p.Employee!.Person!.LastName!, like) ||
                    (isNumeric && p.BusinessEntityID == idNumber)
                );
            }

            q = q
                .OrderBy(p => p.Employee!.Person!.FirstName)
                .ThenBy(p => p.BusinessEntityID);

            // Total filtrado
            var totalCount = await q.CountAsync(ct);

            // Página
            var items = await q
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new DepartmentHistoryDto
                {
                    BusinessEntityID = p.BusinessEntityID,
                    DepartmentId = p.DepartmentID,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Dep = new Department
                    {
                        GroupName = p.Department!.GroupName,
                        Name = p.Department.Name,

                    },
                    ShiftID = p.ShiftID,
                    Person = new Person
                    {
                        BusinessEntityID = p.Employee!.BusinessEntityID,
                        FirstName = p.Employee.Person!.FirstName,
                        LastName = p.Employee.Person!.LastName
                    }
                })
                .ToListAsync(ct);

            var result = new PagedResult<DepartmentHistoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Cabeçalho opcional de paginação
            var paginationHeader = System.Text.Json.JsonSerializer.Serialize(new
            {
                result.TotalCount,
                result.PageNumber,
                result.PageSize,
                result.TotalPages,
                result.HasPrevious,
                result.HasNext
            });
            Response.Headers["X-Pagination"] = paginationHeader;

            return Ok(result);
        }
    }



}