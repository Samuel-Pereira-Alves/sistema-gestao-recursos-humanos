
using AutoMapper;
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

        // // POST: api/v1/notification
        // // Creates a single notification (for a specific BusinessEntityID from the body)
        // [HttpPost]
        // public async Task<IActionResult> Create([FromBody] NotificationDto dto)
        // {
        //     if (dto is null) return BadRequest("Body is required.");
        //     if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("Message is required.");
        //     if (dto.BusinessEntityID <= 0) return BadRequest("BusinessEntityID must be a positive integer.");

        //     var notification = _mapper.Map<Notification>(dto);

        //     _db.Notifications.Add(notification);
        //     await _db.SaveChangesAsync();

        //     // Return the resource by ID
        //     return CreatedAtAction(nameof(GetById),
        //         new { id = notification.ID },
        //         _mapper.Map<NotificationDto>(notification));
        // }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto)
        {
            _logger.LogInformation("Recebido pedido para criar Notificação.");

            if (dto is null)
            {
                _logger.LogWarning("Body não enviado no pedido.");
                return BadRequest("Body is required.");
            }
            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                _logger.LogWarning("Mensagem inválida (vazia ou nula).");
                return BadRequest("Message is required.");
            }
            if (dto.BusinessEntityID <= 0)
            {
                _logger.LogWarning("BusinessEntityID inválido: {BusinessEntityID}.", dto.BusinessEntityID);
                return BadRequest("BusinessEntityID must be a positive integer.");
            }

            try
            {
                var notification = _mapper.Map<Notification>(dto);
                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Notification criada com sucesso. ID={ID}, BusinessEntityID={BusinessEntityID}.",
                    notification.ID, notification.BusinessEntityID);

                return CreatedAtAction(nameof(GetById),
                    new { id = notification.ID },
                    _mapper.Map<NotificationDto>(notification));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Erro ao gravar Notification para BusinessEntityID={BusinessEntityID}.", dto.BusinessEntityID);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar a notificação.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado ao criar Notification para BusinessEntityID={BusinessEntityID}.", dto.BusinessEntityID);

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar a notificação.");
            }
        }

        // // POST: api/v1/notification/{role}
        // // Creates one notification per user that has the given role
        // [HttpPost("{role}")]
        // public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto)
        // {
        //     if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role is required.");
        //     if (dto is null) return BadRequest("Body is required.");
        //     if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("Message is required.");

        //     // Fetch users matching role (case-insensitive)
        //     var users = await _db.SystemUsers
        //         .Where(u => u.Role.ToLower() == role.ToLower()).ToListAsync();

        //     if (users.Count == 0)
        //     {
        //         return NotFound($"No users found for role '{role}'.");
        //     }

        //     // Create notifications for each user
        //     var created = new List<Notification>();
        //     foreach (var user in users)
        //     {
        //         var notif = new Notification
        //         {
        //             Message = dto.Message,
        //             BusinessEntityID = user.BusinessEntityID
        //         };
        //         created.Add(notif);
        //         _db.Notifications.Add(notif);
        //     }
        //     await _db.SaveChangesAsync();

        //     var createdDtos = _mapper.Map<List<NotificationDto>>(created);
        //     return Created("", createdDtos);
        // }

        // POST: api/v1/notification/{role}
        // // Creates one notification per user that has the given role
        [HttpPost("{role}")]
        public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto)
        {
            _logger.LogInformation("Recebida requisição para criar notificações para a role '{Role}'.", role);

            // Validações
            if (string.IsNullOrWhiteSpace(role))
            {
                _logger.LogWarning("Role ausente ou inválida.");
                return BadRequest("Role is required.");
            }
            if (dto is null)
            {
                _logger.LogWarning("Body ausente na requisição.");
                return BadRequest("Body is required.");
            }
            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                _logger.LogWarning("Mensagem inválida (vazia ou nula).");
                return BadRequest("Message is required.");
            }

            try
            {

                var users = await _db.SystemUsers
                    .Where(u => u.Role.ToLower() == role.ToLower())
                    .ToListAsync();

                if (users.Count == 0)
                {
                    _logger.LogWarning("Nenhum utilizador encontrado para a role '{Role}'.", role);
                    return NotFound($"No users found for role '{role}'.");
                }

                _logger.LogInformation("Encontrados {UserCount} utilizadores para a role '{Role}'.", users.Count, role);


                var created = new List<Notification>();
                foreach (var user in users)
                {
                    var notif = new Notification
                    {
                        Message = dto.Message,
                        BusinessEntityID = user.BusinessEntityID
                    };
                    created.Add(notif);
                    _db.Notifications.Add(notif);
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Criadas {NotificationCount} notificações para a role '{Role}'.", created.Count, role);

                var createdDtos = _mapper.Map<List<NotificationDto>>(created);
                return Created("", createdDtos);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao gravar notificações para a role '{Role}'.", role);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao gravar as notificações.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar notificações para a role '{Role}'.", role);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao criar as notificações.");
            }
        }

        // GET: api/v1/notification/by-entity/{businessEntityId}
        // [HttpGet("by-entity/{businessEntityId}")]
        // public async Task<IActionResult> GetByBusinessEntityID(int businessEntityId)
        // {
        //     if (businessEntityId <= 0) return BadRequest("BusinessEntityID must be a positive integer.");

        //     var notifications = await _db.Notifications
        //         .Where(notif => notif.BusinessEntityID == businessEntityId)
        //         .ToListAsync();

        //     var dto = _mapper.Map<List<NotificationDto>>(notifications);
        //     return Ok(dto);
        // }

        // GET: api/v1/notification/by-entity/{businessEntityId}

        [HttpGet("by-entity/{businessEntityId}")]
        public async Task<IActionResult> GetByBusinessEntityID(int businessEntityId)
        {
            _logger.LogInformation("Recebida requisição para obter notificações para BusinessEntityID={BusinessEntityID}.", businessEntityId);

            // Validação
            if (businessEntityId <= 0)
            {
                _logger.LogWarning("BusinessEntityID inválido: {BusinessEntityID}.", businessEntityId);
                return BadRequest("BusinessEntityID must be a positive integer.");
            }

            try
            {
                var notifications = await _db.Notifications
                    .Where(notif => notif.BusinessEntityID == businessEntityId)
                    .ToListAsync();

                _logger.LogInformation("Encontradas {Count} notificações para BusinessEntityID={BusinessEntityID}.", notifications.Count, businessEntityId);

                var dto = _mapper.Map<List<NotificationDto>>(notifications);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter notificações para BusinessEntityID={BusinessEntityID}.", businessEntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter as notificações.");
            }
        }

        // GET: api/v1/notification/{id}
        // [HttpGet("{id}")]
        // public async Task<IActionResult> GetById(int id)
        // {
        //     var notification = await _db.Notifications
        //         .FirstOrDefaultAsync(notif => notif.ID == id);

        //     if (notification == null) return NotFound();

        //     var dto = _mapper.Map<NotificationDto>(notification);
        //     return Ok(dto);
        // }

        // GET: api/v1/notification/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Recebida requisição para obter Notification com ID={ID}.", id);

            try
            {
                var notification = await _db.Notifications
                    .FirstOrDefaultAsync(notif => notif.ID == id);

                if (notification == null)
                {
                    _logger.LogWarning("Notification não encontrada para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("Notification encontrada para ID={ID}.", id);

                var dto = _mapper.Map<NotificationDto>(notification);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter Notification com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao obter a notificação.");
            }
        }

        // DELETE: api/v1/notification/by-entity/{businessEntityId}
        // [HttpDelete("by-entity/{businessEntityId}")]
        // public async Task<IActionResult> DeleteByBusinessEntityID(int businessEntityId)
        // {
        //     if (businessEntityId <= 0) return BadRequest("BusinessEntityID must be a positive integer.");

        //     var notifications = await _db.Notifications
        //         .Where(notif => notif.BusinessEntityID == businessEntityId)
        //         .ToListAsync();

        //     if (notifications.Count == 0) return NotFound();

        //     _db.Notifications.RemoveRange(notifications);
        //     await _db.SaveChangesAsync();

        //     return NoContent();
        // }

        // DELETE: api/v1/notification/by-entity/{businessEntityId}
        [HttpDelete("by-entity/{businessEntityId}")]
        public async Task<IActionResult> DeleteByBusinessEntityID(int businessEntityId)
        {
            _logger.LogInformation("Recebida requisição para eliminar notificações para BusinessEntityID={BusinessEntityID}.", businessEntityId);

            if (businessEntityId <= 0)
            {
                _logger.LogWarning("BusinessEntityID inválido: {BusinessEntityID}.", businessEntityId);
                return BadRequest("BusinessEntityID must be a positive integer.");
            }

            try
            {
                var notifications = await _db.Notifications
                    .Where(notif => notif.BusinessEntityID == businessEntityId)
                    .ToListAsync();

                if (notifications.Count == 0)
                {
                    _logger.LogWarning("Nenhuma notificação encontrada para BusinessEntityID={BusinessEntityID}.", businessEntityId);
                    return NotFound();
                }

                _logger.LogInformation("Encontradas {Count} notificações para BusinessEntityID={BusinessEntityID}. A eliminar...", notifications.Count, businessEntityId);

                _db.Notifications.RemoveRange(notifications);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Notificações eliminadas com sucesso para BusinessEntityID={BusinessEntityID}.", businessEntityId);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao eliminar notificações para BusinessEntityID={BusinessEntityID}.", businessEntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar as notificações.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar notificações para BusinessEntityID={BusinessEntityID}.", businessEntityId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar as notificações.");
            }
        }

        // DELETE: api/v1/notification/{id}
        // [HttpDelete("{id:int}")]
        // public async Task<IActionResult> DeleteById(int id)
        // {
        //     var notification = await _db.Notifications.FindAsync(id);
        //     if (notification == null) return NotFound();

        //     _db.Notifications.Remove(notification);
        //     await _db.SaveChangesAsync();
        //     return NoContent();
        // }

        // DELETE: api/v1/notification/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            _logger.LogInformation("Recebida requisição para eliminar Notification com ID={ID}.", id);
            try
            {
                var notification = await _db.Notifications.FindAsync(id);

                if (notification == null)
                {
                    _logger.LogWarning("Notification não encontrada para ID={ID}.", id);
                    return NotFound();
                }

                _logger.LogInformation("Notification encontrada para ID={ID}. A eliminar...", id);

                _db.Notifications.Remove(notification);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Notification eliminada com sucesso para ID={ID}.", id);

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erro ao eliminar Notification com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao eliminar a notificação.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao eliminar Notification com ID={ID}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao eliminar a notificação.");
            }
        }
    }
}