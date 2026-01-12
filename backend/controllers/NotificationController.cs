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

        // POST: api/v1/notification
        // Creates a single notification (for a specific BusinessEntityID from the body)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto)
        {
            string msg1 = "Recebido pedido para criar Notificação.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            if (dto is null)
            {
                string msg2 = "Body não enviado no pedido.";
                _logger.LogWarning(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("Body is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                string msg3 = "Mensagem inválida (vazia ou nula).";
                _logger.LogWarning(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("Message is required.");
            }

            if (dto.BusinessEntityID <= 0)
            {
                string msg4 = $"BusinessEntityID inválido: {dto.BusinessEntityID}.";
                _logger.LogWarning(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("BusinessEntityID must be a positive integer.");
            }

            try
            {
                var notification = _mapper.Map<Notification>(dto);
                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();

                string msg5 = $"Notification criada com sucesso. ID={notification.ID}, BusinessEntityID={notification.BusinessEntityID}.";
                _logger.LogInformation(msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById),
                    new { id = notification.ID },
                    _mapper.Map<NotificationDto>(notification));
            }
            catch (DbUpdateException dbEx)
            {
                string msg6 = $"Erro ao gravar Notification para BusinessEntityID={dto.BusinessEntityID}.";
                _logger.LogError(dbEx, msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar a notificação.");
            }
            catch (Exception ex)
            {
                string msg7 = $"Erro inesperado ao criar Notification para BusinessEntityID={dto.BusinessEntityID}.";
                _logger.LogError(ex, msg7);
                _db.Logs.Add(new Log { Message = msg7, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar a notificação.");
            }
        }

        // POST: api/v1/notification/{role}
        // // Creates one notification per user that has the given role
        [HttpPost("{role}")]
        public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para criar notificações para a role '{role}'.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            // 2) Validações
            if (string.IsNullOrWhiteSpace(role))
            {
                string msg2 = "Role ausente ou inválida.";
                _logger.LogWarning(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("Role is required.");
            }

            if (dto is null)
            {
                string msg3 = "Body ausente na requisição.";
                _logger.LogWarning(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("Body is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                string msg4 = "Mensagem inválida (vazia ou nula).";
                _logger.LogWarning(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("Message is required.");
            }

            try
            {
                // 3) Obter utilizadores pela role (case-insensitive)
                var roleLower = role.ToLower();
                var users = await _db.SystemUsers
                    .Where(u => u.Role.ToLower() == roleLower)
                    .ToListAsync();

                if (users.Count == 0)
                {
                    string msg5 = $"Nenhum utilizador encontrado para a role '{role}'.";
                    _logger.LogWarning(msg5);
                    _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                    await _db.SaveChangesAsync();
                    return NotFound($"No users found for role '{role}'.");
                }

                string msg6 = $"Encontrados {users.Count} utilizadores para a role '{role}'.";
                _logger.LogInformation(msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 4) Criar notificações para cada utilizador
                var created = new List<Notification>();
                foreach (var user in users)
                {
                    var notif = new Notification
                    {
                        Message = dto.Message,
                        BusinessEntityID = user.BusinessEntityID,
                        CreatedAt = DateTime.UtcNow
                    };
                    created.Add(notif);
                    _db.Notifications.Add(notif);
                }

                await _db.SaveChangesAsync();

                string msg7 = $"Criadas {created.Count} notificações para a role '{role}'.";
                _logger.LogInformation(msg7);
                _db.Logs.Add(new Log { Message = msg7, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 5) Retorno
                var createdDtos = _mapper.Map<List<NotificationDto>>(created);
                return Created("", createdDtos);
            }
            catch (DbUpdateException dbEx)
            {
                string msg8 = $"Erro ao gravar notificações para a role '{role}'.";
                _logger.LogError(dbEx, msg8);
                _db.Logs.Add(new Log { Message = msg8, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar as notificações.");
            }
            catch (Exception ex)
            {
                string msg9 = $"Erro inesperado ao criar notificações para a role '{role}'.";
                _logger.LogError(ex, msg9);
                _db.Logs.Add(new Log { Message = msg9, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar as notificações.");
            }
        }

        // GET: api/v1/notification/by-entity/{businessEntityId}
        [HttpGet("by-entity/{businessEntityId}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetByBusinessEntityID(int businessEntityId)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para obter notificações para BusinessEntityID={businessEntityId}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            // 2) Validação
            if (businessEntityId <= 0)
            {
                string msg2 = $"BusinessEntityID inválido: {businessEntityId}.";
                _logger.LogWarning(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return BadRequest("BusinessEntityID must be a positive integer.");
            }

            try
            {
                // 3) Consulta
                var notifications = await _db.Notifications
                    .Where(notif => notif.BusinessEntityID == businessEntityId)
                    .OrderByDescending(notif => notif.CreatedAt)
                    .ToListAsync();

                string msg3 = $"Encontradas {notifications.Count} notificações para BusinessEntityID={businessEntityId}.";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 4) Mapeamento e retorno
                var dto = _mapper.Map<List<NotificationDto>>(notifications);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                string msg4 = $"Erro ao obter notificações para BusinessEntityID={businessEntityId}.";
                _logger.LogError(ex, msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter as notificações.");
            }
        }

        // GET: api/v1/notification/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetById(int id)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para obter Notification com ID={id}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Consulta
                var notification = await _db.Notifications
                    .FirstOrDefaultAsync(notif => notif.ID == id);

                // 3) Not found
                if (notification == null)
                {
                    string msg2 = $"Notification não encontrada para ID={id}.";
                    _logger.LogWarning(msg2);
                    _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // 4) Encontrada
                string msg3 = $"Notification encontrada para ID={id}.";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 5) Mapeamento e retorno
                var dto = _mapper.Map<NotificationDto>(notification);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                string msg4 = $"Erro ao obter Notification com ID={id}.";
                _logger.LogError(ex, msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter a notificação.");
            }
        }
        // DELETE: api/v1/notification/by-entity/{businessEntityId}
        [HttpDelete("by-entity/{businessEntityId}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> DeleteByBusinessEntityID(int businessEntityId)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para eliminar notificações para BusinessEntityID={businessEntityId}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            // 2) Validação
            if (businessEntityId <= 0)
            {
                string msg2 = $"BusinessEntityID inválido: {businessEntityId}.";
                _logger.LogWarning(msg2);
                _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return BadRequest("BusinessEntityID must be a positive integer.");
            }

            try
            {
                // 3) Obter notificações
                var notifications = await _db.Notifications
                    .Where(notif => notif.BusinessEntityID == businessEntityId)
                    .ToListAsync();

                if (notifications.Count == 0)
                {
                    string msg3 = $"Nenhuma notificação encontrada para BusinessEntityID={businessEntityId}.";
                    _logger.LogWarning(msg3);
                    _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                string msg4 = $"Encontradas {notifications.Count} notificações para BusinessEntityID={businessEntityId}. A eliminar…";
                _logger.LogInformation(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 4) Eliminar e persistir
                _db.Notifications.RemoveRange(notifications);
                await _db.SaveChangesAsync();

                string msg5 = $"Notificações eliminadas com sucesso para BusinessEntityID={businessEntityId}.";
                _logger.LogInformation(msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                string msg6 = $"Erro ao eliminar notificações para BusinessEntityID={businessEntityId}.";
                _logger.LogError(dbEx, msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar as notificações.");
            }
            catch (Exception ex)
            {
                string msg7 = $"Erro inesperado ao eliminar notificações para BusinessEntityID={businessEntityId}.";
                _logger.LogError(ex, msg7);
                _db.Logs.Add(new Log { Message = msg7, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar as notificações.");
            }
        }
        // DELETE: api/v1/notification/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> DeleteById(int id)
        {
            // 1) Pedido recebido
            string msg1 = $"Recebida requisição para eliminar Notification com ID={id}.";
            _logger.LogInformation(msg1);
            _db.Logs.Add(new Log { Message = msg1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            try
            {
                // 2) Procurar a notificação
                var notification = await _db.Notifications.FindAsync(id);

                // 3) Não encontrada
                if (notification == null)
                {
                    string msg2 = $"Notification não encontrada para ID={id}.";
                    _logger.LogWarning(msg2);
                    _db.Logs.Add(new Log { Message = msg2, Date = DateTime.Now });
                    await _db.SaveChangesAsync();

                    return NotFound();
                }

                // 4) Encontrada — a eliminar
                string msg3 = $"Notification encontrada para ID={id}. A eliminar...";
                _logger.LogInformation(msg3);
                _db.Logs.Add(new Log { Message = msg3, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                // 5) Remover e persistir
                _db.Notifications.Remove(notification);
                await _db.SaveChangesAsync();

                string msg4 = $"Notification eliminada com sucesso para ID={id}.";
                _logger.LogInformation(msg4);
                _db.Logs.Add(new Log { Message = msg4, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                string msg5 = $"Erro ao eliminar Notification com ID={id}.";
                _logger.LogError(dbEx, msg5);
                _db.Logs.Add(new Log { Message = msg5, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar a notificação.");
            }
            catch (Exception ex)
            {
                string msg6 = $"Erro inesperado ao eliminar Notification com ID={id}.";
                _logger.LogError(ex, msg6);
                _db.Logs.Add(new Log { Message = msg6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar a notificação.");
            }
        }

    }
}