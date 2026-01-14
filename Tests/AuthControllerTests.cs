using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.Tests.Utils;
using Microsoft.AspNetCore.Http;

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
                ["Jwt:Key"] = "chave-para-testes-1234-sistema-rh"
            };

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

        private Employee SeedEmployee(AdventureWorksContext ctx, int businessEntityId)
        {
            var emp = new Employee
            {
                BusinessEntityID = businessEntityId,
                NationalIDNumber = "123456789",
                LoginID = "adventure-works\\samuel",
                JobTitle = "Developer",
                BirthDate = new DateTime(1990, 01, 01),
                MaritalStatus = "S",
                Gender = "M",
                HireDate = new DateTime(2020, 01, 01),
                SalariedFlag = true,
                VacationHours = 10,
                SickLeaveHours = 0,
                CurrentFlag = true,
                ModifiedDate = DateTime.UtcNow
            };

            ctx.Employees.Add(emp);
            ctx.SaveChanges();
            return emp;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithTokenAndPayloadAsync()
        {
            // Arrange
            var ctx = BuildContext();
            var user = SeedUser(ctx);
            SeedEmployee(ctx, user.BusinessEntityID);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config, MapperMockFactory.CreateLoggerMockAuth().Object);

            var request = new SystemUsersDTO { Username = user.Username, Password = "P@ssw0rd" };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // Act
            var result = await controller.Login(request, ct);

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
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var ctx = BuildContext();
            SeedUser(ctx);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config, MapperMockFactory.CreateLoggerMockAuth().Object);

            var request = new SystemUsersDTO { Username = "samuel", Password = "password-errada" };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var ct = cts.Token;

            // Act
            var result = await controller.Login(request, ct);

            // Assert
            var unauthorized = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode); // 401
        }

        [Fact]
        public async Task Login_UserWithoutRole_DoesNotIncludeRoleClaim()
        {
            // Arrange
            var ctx = BuildContext();
            var user = SeedUser(ctx, role: string.Empty);
            SeedEmployee(ctx, user.BusinessEntityID);
            var config = BuildConfig();
            var controller = new AuthController(ctx, config, MapperMockFactory.CreateLoggerMockAuth().Object);

            var request = new SystemUsersDTO { Username = user.Username, Password = "P@ssw0rd" };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // Act
            var result = await controller.Login(request, ct);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
        {
            var ctx = BuildContext();
            var config = BuildConfig();
            var controller = new AuthController(ctx, config, MapperMockFactory.CreateLoggerMockAuth().Object);

            var dto = new SystemUsersDTO { Username = "naoexiste", Password = "qualquer" };
            var result = await controller.Login(dto, CancellationToken.None);

            var unauthorized = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        }
    }
}