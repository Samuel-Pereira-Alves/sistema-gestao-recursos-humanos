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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SystemUsersDTO request, CancellationToken ct)
        {
            _logger.LogInformation("Pedido de login recebido.");
            AddLog("Pedido de login recebido.");
            await _db.SaveChangesAsync(ct);

            var validationError = ValidateLoginRequest(request);
            if (validationError is not null)
                return validationError;

            try
            {
                var user = await GetUserAsync(request.Username, ct);
                if (!await IsValidUserLoginAsync(user, request.Password, request.Username, ct))
                    return UnauthorizedProblem();

                var employee = await GetEmployeeAsync(user!.BusinessEntityID, ct);
                if (!await IsEmployeeActiveAsync(employee, request.Username, user.BusinessEntityID, ct))
                    return UnauthorizedProblem();

                var token = GenerateJwtToken(user);
                await RegisterSuccessfulLoginAsync(user, request.Username, ct);

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
                return await HandleUnexpectedLoginErrorAsync(ex, request.Username, ct);
            }
        }

        private IActionResult? ValidateLoginRequest(SystemUsersDTO request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Pedido de login inválido: body nulo ou campos em branco.");
                AddLog("Pedido de login inválido: body nulo ou campos em branco.");

                return Problem(
                    title: "Pedido inválido",
                    detail: "Credenciais em falta.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            return null;
        }

        private async Task<SystemUser?> GetUserAsync(string username, CancellationToken ct)
        {
            return await _db.SystemUsers
                .FirstOrDefaultAsync(u => u.Username == username, ct);
        }

        private async Task<bool> IsValidUserLoginAsync(SystemUser? user, string password, string username, CancellationToken ct)
        {
            if (user is null || !VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login falhou para Username={Username}.", username);
                AddLog($"Login falhou para Username={username}.");
                await _db.SaveChangesAsync(ct);
                return false;
            }
            return true;
        }

        private async Task<Employee?> GetEmployeeAsync(int businessEntityId, CancellationToken ct)
        {
            return await _db.Employees
                .FirstOrDefaultAsync(e => e.BusinessEntityID == businessEntityId, ct);
        }

        private async Task<bool> IsEmployeeActiveAsync(Employee? employee, string username, int businessEntityId, CancellationToken ct)
        {
            if (employee is null || !employee.CurrentFlag)
            {
                _logger.LogWarning(
                    "Login falhou: funcionário inexistente ou inativo. Username={Username}, BusinessEntityID={BEID}.",
                    username, businessEntityId);

                AddLog(
                    $"Login falhou: funcionário inexistente ou inativo. Username={username}, BusinessEntityID={businessEntityId}.");

                await _db.SaveChangesAsync(ct);
                return false;
            }
            return true;
        }
        private IActionResult UnauthorizedProblem()
        {
            return Problem(
                title: "Não autorizado",
                detail: "Credenciais inválidas.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        private async Task RegisterSuccessfulLoginAsync(SystemUser user, string username, CancellationToken ct)
        {
            _logger.LogInformation(
                "Login bem-sucedido. Username={Username}, Role={Role}, SystemUserId={SystemUserId}, BusinessEntityID={BEID}",
                username, user.Role, user.SystemUserId, user.BusinessEntityID);

            AddLog($"Login bem-sucedido. Username={username}, Role={user.Role}, SystemUserId={user.SystemUserId}, BusinessEntityID={user.BusinessEntityID}");

            await _db.SaveChangesAsync(ct);
        }

        private async Task<IActionResult> HandleUnexpectedLoginErrorAsync(Exception ex, string username, CancellationToken ct)
        {
            _logger.LogError(ex, "Erro inesperado no login. Username={Username}", username);
            AddLog($"Erro inesperado no login. Username={username}");
            await _db.SaveChangesAsync(ct);

            return Problem(
                title: "Erro ao processar o login",
                detail: "Ocorreu um erro ao processar o login.",
                statusCode: StatusCodes.Status500InternalServerError);
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