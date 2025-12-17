using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class AuthControllerTests
    {
        private AdventureWorksContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AdventureWorksContext(options);
        }

        private IConfiguration BuildConfig()
        {
            var dict = new Dictionary<string, string>
            {
                ["Jwt:Issuer"] = "IssuerTeste",
                ["Jwt:Audience"] = "AudienceTeste",
            };

            dict["Jwt:Key"] = "chave-para-testes-1234-sistema-rh";

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict!)
                .Build();
        }

        private SystemUser SeedUser(
            AdventureWorksContext ctx,
            string username = "samuel",
            string password = "P@ssw0rd",
            string role = "admin",
            int systemUserId = 1,
            int businessEntityId = 42)
        {
            var user = new SystemUser
            {
                SystemUserId = systemUserId,
                BusinessEntityID = businessEntityId,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            };

            ctx.SystemUsers.Add(user);
            ctx.SaveChanges();
            return user;
        }

        [Fact]
        public void Login_WithValidCredentials_ReturnsOkWithTokenAndPayload()
        {
            // Arrange
            var ctx = BuildContext();
            var user = SeedUser(ctx);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config);

            var request = new SystemUsersDTO { Username = user.Username, Password = "P@ssw0rd" };

            // Act
            var result = controller.Login(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            // Extrair propriedades do objeto anônimo { token, role, systemUserId, businessEntityId }
            var token = ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value) as string;
            var role = ok.Value!.GetType().GetProperty("role")!.GetValue(ok.Value) as string;
            var systemUserId = (int)ok.Value!.GetType().GetProperty("systemUserId")!.GetValue(ok.Value)!;
            var businessEntityId = (int)ok.Value!.GetType().GetProperty("businessEntityId")!.GetValue(ok.Value)!;

            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.Equal(user.Role, role);
            Assert.Equal(user.SystemUserId, systemUserId);
            Assert.Equal(user.BusinessEntityID, businessEntityId);

            // Validar claims dentro do JWT
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token!);
            Assert.Contains(parsed.Claims, c => c.Type == "SystemUserId" && c.Value == user.SystemUserId.ToString());
            Assert.Contains(parsed.Claims, c => c.Type == "BusinessEntityID" && c.Value == user.BusinessEntityID.ToString());
            Assert.Contains(parsed.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == user.Role);
        }

        [Fact]
        public void Login_WithInvalidUsername_ReturnsUnauthorized()
        {
            // Arrange
            var ctx = BuildContext();
            SeedUser(ctx);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config);

            var request = new SystemUsersDTO { Username = "user-inexistente", Password = "P@ssw0rd" };

            // Act
            var result = controller.Login(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Credenciais inválidas", unauthorized.Value);
        }

        [Fact]
        public void Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var ctx = BuildContext();
            SeedUser(ctx);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config);

            var request = new SystemUsersDTO { Username = "samuel", Password = "password-errada" };

            // Act
            var result = controller.Login(request);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Credenciais inválidas", unauthorized.Value);
        }

        [Fact]
        public void Login_UserWithoutRole_DoesNotIncludeRoleClaim()
        {
            // Arrange
            var ctx = BuildContext();
            var user = SeedUser(ctx, role: string.Empty);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config);

            var request = new SystemUsersDTO { Username = user.Username, Password = "P@ssw0rd" };

            // Act
            var result = controller.Login(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);

            var token = ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value) as string;
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token!);

            // Claim de role não deve existir
            Assert.DoesNotContain(parsed.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role);

            // O campo role no payload deve ser nulo ou vazio
            var roleProp = ok.Value!.GetType().GetProperty("role")!.GetValue(ok.Value) as string;
            Assert.True(string.IsNullOrEmpty(roleProp));
        }
    }
}
