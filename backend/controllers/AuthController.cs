using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/")]
    public class AuthController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AdventureWorksContext db, IConfiguration config, ILogger<AuthController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }


        [AllowAnonymous]
        // POST: api/v1/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SystemUsersDTO request)
        {
            string message1 = "Pedido de login recebido.";
            _logger.LogInformation(message1);
            _db.Logs.Add(new Log { Message = message1, Date = DateTime.Now });
            await _db.SaveChangesAsync();

            if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                string message2 = "Pedido de login inválido: body nulo ou campos em branco.";
                _logger.LogWarning(message2);
                _db.Logs.Add(new Log { Message = message2, Date = DateTime.Now });
                await _db.SaveChangesAsync();
                return BadRequest("Pedido inválido");
            }

            try
            {
                // 1. Procurar utilizador
                var user = _db.SystemUsers.FirstOrDefault(u => u.Username == request.Username);
                if (user is null)
                {
                    string message3 = $"Login falhou: utilizador não encontrado. Username={request.Username}.";
                    _logger.LogWarning(message3);
                    _db.Logs.Add(new Log { Message = message3, Date = DateTime.Now });
                    await _db.SaveChangesAsync();
                    return Unauthorized("Credenciais inválidas");
                }

                // 2. Validar password (NUNCA logar a password)
                var passwordOk = VerifyPassword(request.Password, user.PasswordHash);
                if (!passwordOk)
                {
                    string message4 = $"Login falhou: password inválida. Username={request.Username}, SystemUserId={user.SystemUserId}.";
                    _logger.LogWarning(message4);
                    _db.Logs.Add(new Log { Message = message4, Date = DateTime.Now });
                    await _db.SaveChangesAsync();
                    return Unauthorized("Credenciais inválidas");
                }

                // 3. Procurar funcionário
                var employee = _db.Employees.FirstOrDefault(e => e.BusinessEntityID == user.BusinessEntityID);
                if (employee is null || !employee.CurrentFlag)
                {
                    string message5 = $"Login falhou: funcionário inexistente ou inativo. Username={request.Username}, BusinessEntityID={user.BusinessEntityID}.";
                    _logger.LogWarning(message5);
                    _db.Logs.Add(new Log { Message = message5, Date = DateTime.Now });
                    await _db.SaveChangesAsync();
                    return Unauthorized("Credenciais inválidas");
                }

                // 4. Gerar token JWT (NUNCA logar o token)
                var token = GenerateJwtToken(user);

                string message6 = $"Login bem-sucedido. Username={request.Username}, Role={user.Role}, SystemUserId={user.SystemUserId}, BusinessEntityID={user.BusinessEntityID}";
                _logger.LogInformation(message6);
                _db.Logs.Add(new Log { Message = message6, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    token,
                    role = user.Role,
                    systemUserId = user.SystemUserId,
                    businessEntityId = user.BusinessEntityID
                });
            }
            catch (Exception ex)
            {
                string message7 = $"Erro inesperado no login. Username={request?.Username}";
                _logger.LogError(ex, message7);
                _db.Logs.Add(new Log { Message = message7, Date = DateTime.Now });
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao processar o login.");
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        private string GenerateJwtToken(SystemUser user)
        {
            var claims = new List<Claim>
            {
                new Claim("SystemUserId", user.SystemUserId.ToString()),
                new Claim("BusinessEntityID", user.BusinessEntityID.ToString())
            };

            if (!string.IsNullOrEmpty(user.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            var keyString = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString))
                throw new InvalidOperationException("JWT key is missing in configuration.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
