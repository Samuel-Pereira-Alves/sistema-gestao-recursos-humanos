
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
        private readonly IWebHostEnvironment _env;

        public NotificationController(AdventureWorksContext db, IMapper mapper, IWebHostEnvironment env)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
        }

        // POST: api/v1/notification
        // Creates a single notification (for a specific BusinessEntityID from the body)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NotificationDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("Message is required.");
            if (dto.BusinessEntityID <= 0) return BadRequest("BusinessEntityID must be a positive integer.");

            var notification = _mapper.Map<Notification>(dto);

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // Return the resource by ID
            return CreatedAtAction(nameof(GetById),
                new { id = notification.ID },
                _mapper.Map<NotificationDto>(notification));
        }

        // POST: api/v1/notification/{role}
        // Creates one notification per user that has the given role
        [HttpPost("{role}")]
        public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto)
        {
            if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role is required.");
            if (dto is null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("Message is required.");

            // Fetch users matching role (case-insensitive)
            var users = await _db.SystemUsers
                .Where(u => u.Role.ToLower() == role.ToLower()).ToListAsync();

            if (users.Count == 0)
            {
                return NotFound($"No users found for role '{role}'.");
            }

            // Create notifications for each user
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

            var createdDtos = _mapper.Map<List<NotificationDto>>(created);
            return Created("", createdDtos);
        }

        // GET: api/v1/notification/by-entity/{businessEntityId}
        [HttpGet("by-entity/{businessEntityId}")]
        public async Task<IActionResult> GetByBusinessEntityID(int businessEntityId)
        {
            if (businessEntityId <= 0) return BadRequest("BusinessEntityID must be a positive integer.");

            var notifications = await _db.Notifications
                .Where(notif => notif.BusinessEntityID == businessEntityId)
                .ToListAsync();

            var dto = _mapper.Map<List<NotificationDto>>(notifications);
            return Ok(dto);
        }

        // GET: api/v1/notification/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(notif => notif.ID == id);

            if (notification == null) return NotFound();

            var dto = _mapper.Map<NotificationDto>(notification);
            return Ok(dto);
        }

        // DELETE: api/v1/notification/by-entity/{businessEntityId}
        [HttpDelete("by-entity/{businessEntityId}")]
        public async Task<IActionResult> DeleteByBusinessEntityID(int businessEntityId)
        {
            if (businessEntityId <= 0) return BadRequest("BusinessEntityID must be a positive integer.");

            var notifications = await _db.Notifications
                .Where(notif => notif.BusinessEntityID == businessEntityId)
                .ToListAsync();

            if (notifications.Count == 0) return NotFound();

            _db.Notifications.RemoveRange(notifications);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // // DELETE: api/v1/notification/{id}
        // [HttpDelete("{id:int}")]
        // public async Task<IActionResult> DeleteById(int id)
        // {
        //     var notification = await _db.Notifications.FindAsync(id);
        //     if (notification == null) return NotFound();

        //     _db.Notifications.Remove(notification);
        //     await _db.SaveChangesAsync();
        //     return NoContent();
        // }
    }
}