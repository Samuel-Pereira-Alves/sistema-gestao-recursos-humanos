
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;  // IWebHostEnvironment
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

        private void SeedNotification(AdventureWorksContext ctx, int id)
        {
            // Minimal seed; ajusta se a tua entidade tiver campos obrigatórios adicionais
            ctx.Notifications.Add(new Notification
            {
                ID = id
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
                 ID = n.ID
                 // adiciona mapeamentos de outros campos aqui, se necessário
             }).ToList() ?? new List<NotificationDto>());

            // Map<NotificationDto>(Notification)
            m.Setup(x => x.Map<NotificationDto>(It.IsAny<Notification>()))
             .Returns((Notification n) => new NotificationDto
             {
                 ID = n.ID
             });

            // Map<Notification>(NotificationDto)
            m.Setup(x => x.Map<Notification>(It.IsAny<NotificationDto>()))
             .Returns((NotificationDto d) => new Notification
             {
                 ID = d.ID
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

        // --------------------------- GET ALL ---------------------------------

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 1);
            SeedNotification(ctx, id: 2);

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<NotificationDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.Contains(list, d => d.ID == 1);
            Assert.Contains(list, d => d.ID == 2);
        }

        // --------------------------- GET BY ID --------------------------------

        [Fact]
        public async Task Get_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 10);

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.Get(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<NotificationDto>(ok.Value);
            Assert.Equal(10, dto.ID);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- CREATE ----------------------------------

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var dto = new NotificationDto
            {
                ID = 0 // se for identity, será atribuído pelo InMemory (ou mantém 0)
            };

            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(NotificationController.Get), created.ActionName);

            var idFromRoute = Assert.IsAssignableFrom<int>(created!.RouteValues!["id"]!);

            var body = Assert.IsType<NotificationDto>(created.Value);
            Assert.Equal(idFromRoute, body.ID);

            var saved = await ctx.Notifications.FirstOrDefaultAsync(n => n.ID == idFromRoute);
            Assert.NotNull(saved);
        }

        // --------------------------- DELETE ----------------------------------

        [Fact]
        public async Task Delete_RemovesEntity_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 42);

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.Delete(42);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await ctx.Notifications.FirstOrDefaultAsync(x => x.ID == 42));
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = CreateNotificationMapperMock();
            var env = CreateEnvMock(Path.GetTempPath());
            var controller = new NotificationController(ctx, mapper.Object, env.Object);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }
       }
}