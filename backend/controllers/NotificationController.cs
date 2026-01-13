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
    public class NotificationController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(AdventureWorksContext db, IMapper mapper, ILogger<NotificationController> logger)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
        }

        private void AddLog(string message) =>
            _db.Logs.Add(new Log { Message = message, Date = DateTime.UtcNow });

        // POST: api/v1/notification
        // Creates a single notification (for a specific BusinessEntityID from the body)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Recebido pedido para criar Notificação.");
            AddLog("Recebido pedido para criar Notificação.");
            await _db.SaveChangesAsync(ct);

            if (dto is null)
            {
                _logger.LogWarning("Body não enviado no pedido.");
                AddLog("Body não enviado no pedido.");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "Body é obrigatório.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                _logger.LogWarning("Mensagem inválida (vazia ou nula).");
                AddLog("Mensagem inválida (vazia ou nula).");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "Message é obrigatório.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (dto.BusinessEntityID <= 0)
            {
                _logger.LogWarning("BusinessEntityID inválido: {BusinessEntityID}", dto.BusinessEntityID);
                AddLog($"BusinessEntityID inválido: {dto.BusinessEntityID}");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "BusinessEntityID deve ser um inteiro positivo.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var notification = _mapper.Map<Notification>(dto);
                notification.CreatedAt = DateTime.UtcNow;

                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Notification criada com sucesso. ID={Id}, BEID={BEID}", notification.ID, notification.BusinessEntityID);
                AddLog($"Notification criada com sucesso. ID={notification.ID}, BEID={notification.BusinessEntityID}");
                await _db.SaveChangesAsync(ct);

                return CreatedAtAction(nameof(GetById),
                    new { id = notification.ID },
                    _mapper.Map<NotificationDto>(notification));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao gravar Notification. BEID={BEID}", dto.BusinessEntityID);
                AddLog($"Erro ao gravar Notification. BEID={dto.BusinessEntityID}: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao gravar", detail: "Erro ao gravar a notificação.", statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar Notification. BEID={BEID}", dto.BusinessEntityID);
                AddLog($"Erro inesperado ao criar Notification. BEID={dto.BusinessEntityID}: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao criar", detail: "Ocorreu um erro ao criar a notificação.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // POST: api/v1/notification/{role}
        // Creates one notification per user that has the given role
        [HttpPost("{role}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para criar notificações para a role '{Role}'.", role);
            AddLog($"Criar notificações para role '{role}'.");
            await _db.SaveChangesAsync(ct);

            if (string.IsNullOrWhiteSpace(role))
            {
                _logger.LogWarning("Role ausente ou inválida.");
                AddLog("Role ausente ou inválida.");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "Role é obrigatória.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (dto is null)
            {
                _logger.LogWarning("Body ausente na requisição.");
                AddLog("Body ausente na requisição.");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "Body é obrigatório.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                _logger.LogWarning("Mensagem inválida (vazia ou nula).");
                AddLog("Mensagem inválida (vazia ou nula).");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "Message é obrigatório.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var roleLower = role.ToLower();
                var users = await _db.SystemUsers
                    .Where(u => u.Role != null && u.Role.ToLower() == roleLower)
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (users.Count == 0)
                {
                    _logger.LogWarning("Nenhum utilizador encontrado para a role '{Role}'.", role);
                    AddLog($"Nenhum utilizador para role '{role}'.");
                    await _db.SaveChangesAsync(ct);
                    return NotFound(new { message = $"No users found for role '{role}'." });
                }

                _logger.LogInformation("Encontrados {Count} utilizadores para a role '{Role}'.", users.Count, role);
                AddLog($"Encontrados {users.Count} utilizadores para role '{role}'.");
                await _db.SaveChangesAsync(ct);

                var nowUtc = DateTime.UtcNow;
                var notifications = users
                    .Select(u => new Notification
                    {
                        Message = dto.Message,
                        BusinessEntityID = u.BusinessEntityID,
                        CreatedAt = nowUtc
                    })
                    .ToList();

                _db.Notifications.AddRange(notifications);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Criadas {Count} notificações para a role '{Role}'.", notifications.Count, role);
                AddLog($"Criadas {notifications.Count} notificações para role '{role}'.");
                await _db.SaveChangesAsync(ct);

                var createdDtos = _mapper.Map<List<NotificationDto>>(notifications);
                // 201 (collection created) – não há um único Location, mas mantemos Created para refletir criação
                return Created("/api/v1/notification", createdDtos);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao gravar notificações para a role '{Role}'.", role);
                AddLog($"Erro ao gravar notificações para role '{role}': {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao gravar", detail: "Erro ao gravar as notificações.", statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar notificações para a role '{Role}'.", role);
                AddLog($"Erro inesperado ao criar notificações para role '{role}': {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao criar", detail: "Ocorreu um erro ao criar as notificações.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // GET: api/v1/notification/by-entity/{businessEntityId}
        [HttpGet("by-entity/{businessEntityId:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<List<NotificationDto>>> GetByBusinessEntityID(int businessEntityId, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter notificações para BEID={BEID}.", businessEntityId);
            AddLog($"Obter notificações para BEID={businessEntityId}.");
            await _db.SaveChangesAsync(ct);

            if (businessEntityId <= 0)
            {
                _logger.LogWarning("BusinessEntityID inválido: {BEID}", businessEntityId);
                AddLog($"BusinessEntityID inválido: {businessEntityId}");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "BusinessEntityID deve ser um inteiro positivo.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var notifications = await _db.Notifications
                    .Where(n => n.BusinessEntityID == businessEntityId)
                    .OrderByDescending(n => n.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(ct);

                _logger.LogInformation("Encontradas {Count} notificações para BEID={BEID}.", notifications.Count, businessEntityId);
                AddLog($"Encontradas {notifications.Count} notificações para BEID={businessEntityId}");
                await _db.SaveChangesAsync(ct);

                return Ok(_mapper.Map<List<NotificationDto>>(notifications));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter notificações para BEID={BEID}.", businessEntityId);
                AddLog($"Erro ao obter notificações para BEID={businessEntityId}: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao consultar", detail: "Ocorreu um erro ao obter as notificações.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // GET: api/v1/notification/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<ActionResult<NotificationDto>> GetById(int id, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para obter Notification com ID={Id}.", id);
            AddLog($"Obter Notification ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var notification = await _db.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.ID == id, ct);

                if (notification is null)
                {
                    _logger.LogWarning("Notification não encontrada para ID={Id}.", id);
                    AddLog($"Notification não encontrada ID={id}");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                return Ok(_mapper.Map<NotificationDto>(notification));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter Notification com ID={Id}.", id);
                AddLog($"Erro ao obter Notification ID={id}: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao consultar", detail: "Ocorreu um erro ao obter a notificação.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE: api/v1/notification/by-entity/{businessEntityId}
        [HttpDelete("by-entity/{businessEntityId:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> DeleteByBusinessEntityID(int businessEntityId, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para eliminar notificações para BEID={BEID}.", businessEntityId);
            AddLog($"Eliminar notificações para BEID={businessEntityId}.");
            await _db.SaveChangesAsync(ct);

            if (businessEntityId <= 0)
            {
                _logger.LogWarning("BusinessEntityID inválido: {BEID}.", businessEntityId);
                AddLog($"BusinessEntityID inválido: {businessEntityId}");
                await _db.SaveChangesAsync(ct);
                return Problem(title: "Pedido inválido", detail: "BusinessEntityID deve ser um inteiro positivo.", statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var notifications = await _db.Notifications
                    .Where(n => n.BusinessEntityID == businessEntityId)
                    .ToListAsync(ct);

                if (notifications.Count == 0)
                {
                    _logger.LogWarning("Nenhuma notificação encontrada para BEID={BEID}.", businessEntityId);
                    AddLog($"Nenhuma notificação para BEID={businessEntityId}");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _db.Notifications.RemoveRange(notifications);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Notificações eliminadas com sucesso para BEID={BEID}.", businessEntityId);
                AddLog($"Notificações eliminadas para BEID={businessEntityId}");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao eliminar notificações para BEID={BEID}.", businessEntityId);
                AddLog($"Erro ao eliminar notificações para BEID={businessEntityId}: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao eliminar", detail: "Erro ao eliminar as notificações.", statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar notificações para BEID={BEID}.", businessEntityId);
                AddLog($"Erro inesperado ao eliminar notificações para BEID={businessEntityId}: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao eliminar", detail: "Ocorreu um erro ao eliminar as notificações.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // DELETE: api/v1/notification/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> DeleteById(int id, CancellationToken ct)
        {
            _logger.LogInformation("Recebida requisição para eliminar Notification com ID={Id}.", id);
            AddLog($"Eliminar Notification ID={id}.");
            await _db.SaveChangesAsync(ct);

            try
            {
                var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.ID == id, ct);
                if (notification is null)
                {
                    _logger.LogWarning("Notification não encontrada para ID={Id}.", id);
                    AddLog($"Notification não encontrada ID={id}");
                    await _db.SaveChangesAsync(ct);
                    return NotFound();
                }

                _db.Notifications.Remove(notification);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Notification eliminada com sucesso. ID={Id}.", id);
                AddLog($"Notification eliminada ID={id}");
                await _db.SaveChangesAsync(ct);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao eliminar Notification ID={Id}.", id);
                AddLog($"Erro ao eliminar Notification ID={id}: {dbEx.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao eliminar", detail: "Erro ao eliminar a notificação.", statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar Notification ID={Id}.", id);
                AddLog($"Erro inesperado ao eliminar Notification ID={id}: {ex.Message}");
                await _db.SaveChangesAsync(ct);

                return Problem(title: "Erro ao eliminar", detail: "Ocorreu um erro ao eliminar a notificação.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}