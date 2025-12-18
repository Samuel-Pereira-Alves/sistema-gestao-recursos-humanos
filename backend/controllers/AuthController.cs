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
        public IActionResult Login([FromBody] SystemUsersDTO request)
        {

            _logger.LogInformation("Pedido de login recebido.");

            if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Pedido de login inválido: body nulo ou campos em branco.");
                return BadRequest("Pedido inválido");
            }

            try
            {
                // 1. Procurar utilizador
                var user = _db.SystemUsers.FirstOrDefault(u => u.Username == request.Username);
                if (user is null)
                {
                    _logger.LogWarning("Login falhou: utilizador não encontrado. Username={Username}",
                        request.Username);
                    return Unauthorized("Credenciais inválidas");
                }

                // 2. Validar password (NUNCA logar a password)
                var passwordOk = VerifyPassword(request.Password, user.PasswordHash);
                if (!passwordOk)
                {
                    _logger.LogWarning("Login falhou: password inválida. Username={Username}, SystemUserId={SystemUserId}.",
                        request.Username, user.SystemUserId);
                    return Unauthorized("Credenciais inválidas");
                }

                // 3. Procurar funcionário
                var employee = _db.Employees.FirstOrDefault(e => e.BusinessEntityID == user.BusinessEntityID);
                if (employee is null || !employee.CurrentFlag)
                {
                    _logger.LogWarning("Login falhou: funcionário inexistente ou inativo. Username={Username}, BusinessEntityID={BusinessEntityID}",
                        request.Username, user.BusinessEntityID);
                    return Unauthorized("Credenciais inválidas");
                }

                // 4. Gerar token JWT (NUNCA logar o token)
                var token = GenerateJwtToken(user);

                _logger.LogInformation("Login bem-sucedido. Username={Username}, Role={Role}, SystemUserId={SystemUserId}, BusinessEntityID={BusinessEntityID}",
                    request.Username, user.Role, user.SystemUserId, user.BusinessEntityID);

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
                _logger.LogError(ex,
                    "Erro inesperado no login. Username={Username}",
                    request?.Username);

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
