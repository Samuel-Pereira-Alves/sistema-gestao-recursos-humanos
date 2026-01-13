using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    [Route("api/v1")]
    public class AuthController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AdventureWorksContext db,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        // Small helper to register a message into DB logs (callers decide when to save)
        private void AddLog(string message) =>
            _db.Logs.Add(new Log { Message = message, Date = DateTime.UtcNow });

        [AllowAnonymous]
        // POST: api/v1/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SystemUsersDTO request, CancellationToken ct)
        {
            _logger.LogInformation("Pedido de login recebido.");
            AddLog("Pedido de login recebido.");
            await _db.SaveChangesAsync(ct);

            if (request is null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Pedido de login inválido: body nulo ou campos em branco.");
                AddLog("Pedido de login inválido: body nulo ou campos em branco.");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Pedido inválido",
                    detail: "Credenciais em falta.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                // 1) Procurar utilizador (async + cancel)
                var user = await _db.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

                // 2) Validar credenciais (mensagem neutra)
                if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login falhou para Username={Username}.", request.Username);
                    AddLog($"Login falhou para Username={request.Username}.");
                    await _db.SaveChangesAsync(ct);

                    // 401 sem indicar se foi user ou password
                    return Problem(
                        title: "Não autorizado",
                        detail: "Credenciais inválidas.",
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                // 3) Verificar funcionário ativo
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.BusinessEntityID == user.BusinessEntityID, ct);

                if (employee is null || !employee.CurrentFlag)
                {
                    _logger.LogWarning("Login falhou: funcionário inexistente ou inativo. Username={Username}, BusinessEntityID={BEID}.",
                        request.Username, user.BusinessEntityID);
                    AddLog($"Login falhou: funcionário inexistente ou inativo. Username={request.Username}, BusinessEntityID={user.BusinessEntityID}.");
                    await _db.SaveChangesAsync(ct);

                    return Problem(
                        title: "Não autorizado",
                        detail: "Credenciais inválidas.",
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                // 4) Gerar JWT (não logar o token)
                var token = GenerateJwtToken(user);
                _logger.LogInformation("Login bem-sucedido. Username={Username}, Role={Role}, SystemUserId={SystemUserId}, BusinessEntityID={BEID}",
                    request.Username, user.Role, user.SystemUserId, user.BusinessEntityID);
                AddLog($"Login bem-sucedido. Username={request.Username}, Role={user.Role}, SystemUserId={user.SystemUserId}, BusinessEntityID={user.BusinessEntityID}");
                await _db.SaveChangesAsync(ct);

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
                _logger.LogError(ex, "Erro inesperado no login. Username={Username}", request?.Username);
                AddLog($"Erro inesperado no login. Username={request?.Username}");
                await _db.SaveChangesAsync(ct);

                return Problem(
                    title: "Erro ao processar o login",
                    detail: "Ocorreu um erro ao processar o login.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static bool VerifyPassword(string password, string storedHash)
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
            {
                throw new InvalidOperationException("JWT key is missing in configuration.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}