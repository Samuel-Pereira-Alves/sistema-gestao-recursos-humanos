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
    public class NotificationController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationController> _logger;
        private readonly IAppLogService _appLog;

        public NotificationController(AdventureWorksContext db, IMapper mapper, ILogger<NotificationController> logger, IAppLogService appLog)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _appLog = appLog;
        }

        private async Task<ActionResult> HandleUnexpectedNotificationErrorAsync(Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro inesperado em Notification");
            await _appLog.ErrorAsync("Erro inesperado em Notification", ex);
            return Problem(
                title: "Erro ao processar Notification",
                detail: "Ocorreu um erro ao processar a requisição de Notification.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        private async Task<ActionResult> HandleDatabaseWriteErrorAsync(DbUpdateException dbEx, string titulo, string detalhe, CancellationToken ct)
        {
            _logger.LogError(dbEx, titulo);
            await _appLog.ErrorAsync($"{titulo}: {dbEx.Message}", dbEx);
            return Problem(
                title: titulo,
                detail: detalhe,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // POST: api/v1/notification
        // Cria uma notificação (para o BusinessEntityID presente no body)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Recebido pedido para criar Notificação.");
            await _appLog.InfoAsync("Recebido pedido para criar Notificação.");

            var validation = await ValidateCreateDtoAsync(dto, ct);
            if (validation is IActionResult badReq) return badReq;

            try
            {
                var notification = await CreateAndSaveNotificationAsync(dto, ct);

                _logger.LogInformation("Notification criada com sucesso. ID={Id}, BEID={BEID}.",
                    notification.ID, notification.BusinessEntityID);
                await _appLog.InfoAsync($"Notification criada com sucesso. ID={notification.ID}, BEID={notification.BusinessEntityID}.");

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = notification.ID },
                    _mapper.Map<NotificationDto>(notification));
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleDatabaseWriteErrorAsync(
                    dbEx,
                    "Erro ao gravar Notification",
                    "Ocorreu um erro ao persistir a notificação.",
                    ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedNotificationErrorAsync(ex, ct);
            }
        }
        private async Task<IActionResult?> ValidateCreateDtoAsync(NotificationDto? dto, CancellationToken ct)
        {
            if (dto is null)
            {
                const string msg = "Body não enviado no pedido.";
                _logger.LogWarning(msg);
                await _appLog.WarnAsync(msg);
                return BadRequest(new { message = "Body é obrigatório" });
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                const string msg = "Mensagem inválida (vazia ou nula).";
                _logger.LogWarning(msg);
                await _appLog.WarnAsync(msg);
                return BadRequest(new { message = "Message é obrigatório" });
            }

            if (dto.BusinessEntityID <= 0)
            {
                var msg = $"BusinessEntityID inválido: {dto.BusinessEntityID}.";
                _logger.LogWarning(msg);
                await _appLog.WarnAsync(msg);
                return BadRequest(new { message = "BusinessEntityID deve ser um inteiro positivo" });
            }

            var exists = await _db.Employees.AnyAsync(e => e.BusinessEntityID == dto.BusinessEntityID, ct);
            if (!exists)
            {
                _logger.LogWarning($"Employee não encontrado. BEID={dto.BusinessEntityID}");
                await _appLog.WarnAsync($"Employee não encontrado. BEID={dto.BusinessEntityID}");
                return NotFound(new { message = "Employee não encontrado", businessEntityId = dto.BusinessEntityID });
            }
            return null;
        }

        private async Task<Notification> CreateAndSaveNotificationAsync(NotificationDto dto, CancellationToken ct)
        {
            var notification = _mapper.Map<Notification>(dto);
            notification.CreatedAt = DateTime.UtcNow;
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(ct);
            return notification;
        }

        // POST: api/v1/notification/{role}
        // Cria 1 notificação por utilizador com a role indicada
        [HttpPost("{role}")]
        public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto, CancellationToken ct)
        {
            var msgReq = $"Recebida requisição para criar notificações para a role '{role}'.";
            _logger.LogInformation(msgReq);
            await _appLog.InfoAsync(msgReq);

            if (string.IsNullOrWhiteSpace(role))
            {
                const string msg = "Role ausente ou inválida.";
                _logger.LogWarning(msg);
                await _appLog.WarnAsync(msg);
                return BadRequest(new { message = "Role é obrigatória" });
            }

            if (dto is null || string.IsNullOrWhiteSpace(dto.Message))
            {
                const string msg = "Body/Mensagem inválidos.";
                _logger.LogWarning(msg);
                await _appLog.WarnAsync(msg);
                return BadRequest(new { message = "Body e Message são obrigatórios" });
            }

            try
            {
                var roleLower = role.ToLowerInvariant();

                var users = await _db.SystemUsers
                    .Where(u => u.Role.ToLower() == roleLower)
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (users.Count == 0)
                {
                    var msg = $"Nenhum utilizador encontrado para a role '{role}'.";
                    _logger.LogWarning(msg);
                    await _appLog.WarnAsync(msg);
                    return NotFound(new { message = $"No users found for role '{role}'" });
                }

                _logger.LogInformation("Encontrados {Count} utilizadores para a role '{Role}'.", users.Count, role);
                await _appLog.InfoAsync($"Encontrados {users.Count} utilizadores para a role '{role}'");

                var now = DateTime.UtcNow;
                var notifs = users.Select(u => new Notification
                {
                    Message = dto.Message,
                    BusinessEntityID = u.BusinessEntityID,
                    CreatedAt = now,
                    Type = dto.Type
                }).ToList();

                _db.Notifications.AddRange(notifs);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Criadas {Count} notificações para a role '{Role}'.", notifs.Count, role);
                await _appLog.InfoAsync($"Criadas {notifs.Count} notificações para a role '{role}'");

                var createdDtos = _mapper.Map<List<NotificationDto>>(notifs);
                return Created(string.Empty, createdDtos);
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleDatabaseWriteErrorAsync(
                    dbEx,
                    $"Erro ao gravar notificações para a role '{role}'",
                    "Erro ao gravar as notificações.",
                    ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedNotificationErrorAsync(ex, ct);
            }
        }

        // === Get by BusinessEntity ===
        // GET: api/v1/notification/by-entity/{businessEntityId}
        [HttpGet("by-entity/{businessEntityId:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<List<NotificationDto>>> GetByBusinessEntityID(int businessEntityId, CancellationToken ct)
        {
            var msgReq = $"Recebida requisição para obter notificações para BusinessEntityID={businessEntityId}.";
            _logger.LogInformation(msgReq);
            await _appLog.InfoAsync(msgReq);

            if (businessEntityId <= 0)
            {
                var msg = $"BusinessEntityID inválido: {businessEntityId}.";
                _logger.LogWarning(msg);
                await _appLog.InfoAsync(msg);
                return BadRequest(new { message = "BusinessEntityID deve ser inteiro positivo" });
            }

            try
            {
                var notifications = await _db.Notifications
                    .Where(n => n.BusinessEntityID == businessEntityId)
                    .OrderByDescending(n => n.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(ct);

                var info = $"Encontradas {notifications.Count} notificações para BEID={businessEntityId}.";
                _logger.LogInformation(info);
                await _appLog.InfoAsync(info);

                var dto = _mapper.Map<List<NotificationDto>>(notifications);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedNotificationErrorAsync(ex, ct);
            }
        }

        // === Get by Id ===
        // GET: api/v1/notification/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<NotificationDto>> GetById(int id, CancellationToken ct)
        {
            var msgReq = $"Recebida requisição para obter Notification com ID={id}.";
            _logger.LogInformation(msgReq);
            await _appLog.InfoAsync(msgReq);

            try
            {
                var notification = await _db.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.ID == id, ct);

                if (notification is null)
                {
                    var msg = $"Notification não encontrada para ID={id}.";
                    _logger.LogWarning(msg);
                    await _appLog.WarnAsync(msg);
                    return NotFound();
                }
                _logger.LogInformation($"Notification encontrada para ID={id}.");
                await _appLog.InfoAsync($"Notification encontrada para ID={id}.");
                return Ok(_mapper.Map<NotificationDto>(notification));
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedNotificationErrorAsync(ex, ct);
            }
        }

        // === Delete by BusinessEntity ===
        // DELETE: api/v1/notification/by-entity/{businessEntityId}
        [HttpDelete("by-entity/{businessEntityId:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> DeleteByBusinessEntityID(int businessEntityId, CancellationToken ct)
        {
            var msgReq = $"Recebida requisição para eliminar notificações para BusinessEntityID={businessEntityId}.";
            _logger.LogInformation(msgReq);
            await _appLog.InfoAsync(msgReq);

            if (businessEntityId <= 0)
            {
                var msg = $"BusinessEntityID inválido: {businessEntityId}.";
                _logger.LogWarning(msg);
                await _appLog.WarnAsync(msg);
                return BadRequest(new { message = "BusinessEntityID deve ser inteiro positivo" });
            }

            try
            {
                var notifications = await _db.Notifications
                    .Where(n => n.BusinessEntityID == businessEntityId)
                    .ToListAsync(ct);

                if (notifications.Count == 0)
                {
                    var msg = $"Nenhuma Notification encontrada para BEID={businessEntityId}.";
                    _logger.LogWarning(msg);
                    await _appLog.WarnAsync(msg);
                    return NotFound();
                }

                _db.Notifications.RemoveRange(notifications);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation($"Notificações eliminadas com sucesso para BEID={businessEntityId}.");
                await _appLog.InfoAsync($"Notificações eliminadas com sucesso para BEID={businessEntityId}.");

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleDatabaseWriteErrorAsync(
                    dbEx,
                    "Erro ao eliminar Notifications",
                    "Erro ao eliminar as notificações.",
                    ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedNotificationErrorAsync(ex, ct);
            }
        }

        // === Delete by Id ===
        // DELETE: api/v1/notification/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> DeleteById(int id, CancellationToken ct)
        {
            var msgReq = $"Recebida requisição para eliminar Notification com ID={id}.";
            _logger.LogInformation(msgReq);
            await _appLog.InfoAsync(msgReq);

            try
            {
                var notification = await _db.Notifications.FindAsync([id], ct);
                if (notification is null)
                {
                    var msg = $"Notification não encontrada para ID={id}.";
                    _logger.LogWarning(msg);
                    await _appLog.WarnAsync(msg);
                    return NotFound();
                }

                _db.Notifications.Remove(notification);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation($"Notification eliminada com sucesso para ID={id}.");
                await _appLog.InfoAsync($"Notification eliminada com sucesso para ID={id}.");

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                return await HandleDatabaseWriteErrorAsync(
                    dbEx,
                    "Erro ao eliminar Notification",
                    "Erro ao eliminar a notificação.",
                    ct);
            }
            catch (Exception ex)
            {
                return await HandleUnexpectedNotificationErrorAsync(ex, ct);
            }
        }
    }
}