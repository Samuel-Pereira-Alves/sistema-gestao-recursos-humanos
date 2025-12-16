using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.backend.models.tools;

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

        // GET: api/v1/notification
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await _db.Notifications.ToListAsync();
            var dto = _mapper.Map<List<NotificationDto>>(notifications);
            return Ok(dto);
        }

        // GET: api/v1/notification/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(notif => notif.ID == id);

            if (notification == null) return NotFound();

            var dto = _mapper.Map<NotificationDto>(notification);
            return Ok(dto);
        }

        // POST: api/v1/notification
        [HttpPost]
        public async Task<IActionResult> Create(NotificationDto dto)
        {
            var notification = _mapper.Map<Notification>(dto);

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new { id = notification.ID },
                _mapper.Map<NotificationDto>(notification));
        }

        // DELETE: api/v1/notification/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _db.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();

            return NoContent();
        }


        // POST: api/v1/notification/{role}
        [HttpPost("{role}")]
        public async Task<IActionResult> CreateForRole(string role, [FromBody] NotificationDto dto)
        {
            if (string.IsNullOrWhiteSpace(role))
                return BadRequest("Role is required.");

            var notification = _mapper.Map<Notification>(dto);

            var users = await _db.SystemUsers.Where(u => u.Role == role.ToLower()).ToListAsync();
            foreach (var user in users)
            {
                var notif = new Notification
                {
                    Message = dto.Message,
                    BusinessEntityID = user.BusinessEntityID
                };
                _db.Notifications.Add(notif);
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new { id = notification.ID },
                _mapper.Map<NotificationDto>(notification));
        }

    }
}