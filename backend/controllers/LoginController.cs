using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IConfiguration _config;

        public LoginController(AdventureWorksContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // POST: api/v1/login
        [HttpPost]
        public IActionResult Login([FromBody] LoginDTO request)
        {
            // 1. Procurar utilizador
            var user = _db.SystemUsers.FirstOrDefault(u => u.Username == request.Username);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized("Credenciais inválidas");
            }

            // 2. Gerar token JWT
            var token = GenerateJwtToken(user);

            // 3. Devolver token + permissões
            return Ok(new
            {
                token,
                role = user.Role,
                systemUserId = user.SystemUserId,
                businessEntityId = user.BusinessEntityID
            });
        }

        private bool VerifyPassword(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
                return false;

            return password == storedPassword;

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
