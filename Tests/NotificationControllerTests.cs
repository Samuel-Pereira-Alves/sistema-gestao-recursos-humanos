
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class NotificationControllerTests
    {
        // --------------------------- Helpers ---------------------------------

        private AdventureWorksContext BuildContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            return new AdventureWorksContext(options);
        }

        private void SeedNotification(AdventureWorksContext ctx, int id, int businessEntityId, string message = "Hello")
        {
            ctx.Notifications.Add(new Notification
            {
                ID = id,
                BusinessEntityID = businessEntityId,
                Message = message
            });
            ctx.SaveChanges();
        }

        private Mock<IMapper> CreateNotificationMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Map<List<NotificationDto>>(List<Notification>)
            m.Setup(x => x.Map<List<NotificationDto>>(It.IsAny<List<Notification>>()))
             .Returns((List<Notification> src) => src?.Select(n => new NotificationDto
             {
                 ID = n.ID,
                 BusinessEntityID = n.BusinessEntityID,
                 Message = n.Message
             }).ToList() ?? new List<NotificationDto>());

            // Map<NotificationDto>(Notification)
            m.Setup(x => x.Map<NotificationDto>(It.IsAny<Notification>()))
             .Returns((Notification n) => new NotificationDto
             {
                 ID = n.ID,
                 BusinessEntityID = n.BusinessEntityID,
                 Message = n.Message
             });

            // Map<Notification>(NotificationDto)
            m.Setup(x => x.Map<Notification>(It.IsAny<NotificationDto>()))
             .Returns((NotificationDto d) => new Notification
             {
                 ID = d.ID,
                 BusinessEntityID = d.BusinessEntityID,
                 Message = d.Message
             });

            return m;
        }

        private Mock<IWebHostEnvironment> CreateEnvMock(string root)
        {
            var env = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            env.SetupGet(x => x.WebRootPath).Returns(root);
            env.SetupGet(x => x.ContentRootPath).Returns(root);
            return env;
        }

        // --------------------------- GET BY ENTITY ----------------------------

        [Fact]
        public async Task GetByBusinessEntityID_ReturnsOk_WithList_WhenFound()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 10, businessEntityId: 100, message: "Msg A");
            SeedNotification(ctx, id: 11, businessEntityId: 100, message: "Msg B");
            SeedNotification(ctx, id: 12, businessEntityId: 200, message: "Msg C");

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.GetByBusinessEntityID(100);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<NotificationDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.All(list, d => Assert.Equal(100, d.BusinessEntityID));
        }

        [Fact]
        public async Task GetByBusinessEntityID_ReturnsBadRequest_WhenInvalidId()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.GetByBusinessEntityID(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // --------------------------- GET BY ID --------------------------------

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 42, businessEntityId: 500, message: "Ping");

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.GetById(42);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<NotificationDto>(ok.Value);
            Assert.Equal(42, dto.ID);
            Assert.Equal(500, dto.BusinessEntityID);
            Assert.Equal("Ping", dto.Message);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.GetById(999);
            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- CREATE (BODY) ----------------------------

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto
            {
                // ID será atribuído pelo InMemory, podes deixar 0
                BusinessEntityID = 123,
                Message = "Nova mensagem"
            };

            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(NotificationController.GetById), created.ActionName);

            var idFromRoute = Assert.IsAssignableFrom<int>(created!.RouteValues!["id"]!);

            var body = Assert.IsType<NotificationDto>(created.Value);
            Assert.Equal(idFromRoute, body.ID);
            Assert.Equal(123, body.BusinessEntityID);
            Assert.Equal("Nova mensagem", body.Message);

            var saved = await ctx.Notifications.FirstOrDefaultAsync(n => n.ID == idFromRoute);
            Assert.NotNull(saved);
            Assert.Equal(123, saved!.BusinessEntityID);
            Assert.Equal("Nova mensagem", saved!.Message);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenMessageMissing()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto
            {
                BusinessEntityID = 123,
                Message = "" // inválida
            };

            var result = await controller.Create(dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Message is required.", bad.Value);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenBusinessEntityIDInvalid()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto
            {
                BusinessEntityID = 0, // inválido
                Message = "Olá"
            };

            var result = await controller.Create(dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("BusinessEntityID must be a positive integer.", bad.Value);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenBodyNull()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.Create(null!);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Body is required.", bad.Value);
        }

        // --------------------------- CREATE FOR ROLE --------------------------

        [Fact]
        public async Task CreateForRole_ReturnsCreated_ForUsersInRole()
        {
            var ctx = BuildContext();

            // Seed de dois utilizadores com o mesmo role
            ctx.SystemUsers.AddRange(
                new SystemUser { SystemUserId = 1, BusinessEntityID = 700, Role = "HR" },
                new SystemUser { SystemUserId = 2, BusinessEntityID = 701, Role = "hr" } // case-insensitive
            );
            await ctx.SaveChangesAsync();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto
            {
                Message = "Aviso para HR"
            };

            var result = await controller.CreateForRole("HR", dto);

            var created = Assert.IsType<CreatedResult>(result);
            var body = Assert.IsType<List<NotificationDto>>(created.Value);
            Assert.Equal(2, body.Count);
            Assert.All(body, d => Assert.Equal("Aviso para HR", d.Message));

            // Confirma que foram persistidas 2 notificações no DB
            var dbCount = await ctx.Notifications.CountAsync();
            Assert.Equal(2, dbCount);

            var beids = await ctx.Notifications.Select(n => n.BusinessEntityID).OrderBy(x => x).ToListAsync();
            Assert.Equal(new List<int> { 700, 701 }, beids);
        }

        [Fact]
        public async Task CreateForRole_ReturnsNotFound_WhenNoUsers()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto { Message = "Aviso geral" };

            var result = await controller.CreateForRole("DEV", dto);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No users found for role 'DEV'.", notFound.Value);
        }

        [Fact]
        public async Task CreateForRole_ReturnsBadRequest_WhenRoleMissing()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto { Message = "Aviso" };

            var result = await controller.CreateForRole("", dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Role is required.", bad.Value);
        }

        [Fact]
        public async Task CreateForRole_ReturnsBadRequest_WhenBodyNull()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.CreateForRole("HR", null!);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Body is required.", bad.Value);
        }

        [Fact]
        public async Task CreateForRole_ReturnsBadRequest_WhenMessageMissing()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto { Message = "" };

            var result = await controller.CreateForRole("HR", dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Message is required.", bad.Value);
        }

        // --------------------------- DELETE BY ENTITY -------------------------

        [Fact]
        public async Task DeleteByBusinessEntityID_RemovesEntities_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            // duas notificações para a mesma entidade
            SeedNotification(ctx, id: 1, businessEntityId: 999, message: "A");
            SeedNotification(ctx, id: 2, businessEntityId: 999, message: "B");

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.DeleteByBusinessEntityID(999);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(0, await ctx.Notifications.CountAsync(n => n.BusinessEntityID == 999));
        }

        [Fact]
        public async Task DeleteByBusinessEntityID_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.DeleteByBusinessEntityID(12345);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteByBusinessEntityID_ReturnsBadRequest_WhenInvalidId()
        {
            var ctx = BuildContext();
            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.DeleteByBusinessEntityID(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // --------------------------- DELETE BY ID -----------------------------

        [Fact]
        public async Task DeleteById_RemovesEntity_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 42, businessEntityId: 1000, message: "Del me");

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.DeleteById(42);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await ctx.Notifications.FirstOrDefaultAsync(x => x.ID == 42));
        }

        [Fact]
        public async Task DeleteById_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.DeleteById(777);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
