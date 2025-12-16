
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class AuthControllerTests
    {
        // --- Helpers ---------------------------------------------------------

        private AdventureWorksContext BuildContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            // Ajuste aqui caso o seu AdventureWorksContext tenha construtor diferente.
            return new AdventureWorksContext(options);
        }

        private IConfiguration BuildConfig(bool withKey = true)
        {
            var dict = new Dictionary<string, string>
            {
                ["Jwt:Issuer"] = "sgrh-api",
                ["Jwt:Audience"] = "sgrh-client",
            };

            if (withKey)
                dict["Jwt:Key"] = "super-secret-test-key-1234567890"; 

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict!)
                .Build();
        }

        private SystemUser SeedUser(
            AdventureWorksContext ctx,
            string username = "samuel",
            string password = "P@ssw0rd",
            string role = "Admin",
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

        // --- Tests -----------------------------------------------------------

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
        public void Login_WithoutJwtKey_ThrowsInvalidOperationException()
        {
            // Arrange
            var ctx = BuildContext();
            var user = SeedUser(ctx);
            var config = BuildConfig(withKey: false); // Intencional: sem chave JWT
            var controller = new AuthController(ctx, config);

            var request = new SystemUsersDTO { Username = user.Username, Password = "P@ssw0rd" };

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() => controller.Login(request));
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
