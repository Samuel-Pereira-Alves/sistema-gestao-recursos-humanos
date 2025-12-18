using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.Tests.Utils;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class NotificationControllerTests
    {
        private AdventureWorksContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

        [Fact]
        public async Task GetByBusinessEntityID_ReturnsOk_WithList_WhenFound()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 10, businessEntityId: 100, message: "Msg A");
            SeedNotification(ctx, id: 11, businessEntityId: 100, message: "Msg B");
            SeedNotification(ctx, id: 12, businessEntityId: 200, message: "Msg C");

            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.GetByBusinessEntityID(100);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<NotificationDto>>(ok.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetByBusinessEntityID_ReturnsBadRequest_WhenInvalidId()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.GetByBusinessEntityID(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 42, businessEntityId: 500, message: "Ping");

            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

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
            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.GetById(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object,  MapperMockFactory.CreateLoggerMockNotification().Object);

            var dto = new NotificationDto
            {
                BusinessEntityID = 123,
                Message = "Nova mensagem"
            };

            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(NotificationController.GetById), created.ActionName);


            var body = Assert.IsType<NotificationDto>(created.Value);
            Assert.Equal(123, body.BusinessEntityID);
            Assert.Equal("Nova mensagem", body.Message);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenMessageMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var dto = new NotificationDto
            {
                BusinessEntityID = 123,
                Message = ""
            };

            var result = await controller.Create(dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Message is required.", bad.Value);
        }

        [Fact]
        public async Task DeleteByBusinessEntityID_RemovesEntities_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 1, businessEntityId: 999, message: "A");
            SeedNotification(ctx, id: 2, businessEntityId: 999, message: "B");

            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.DeleteByBusinessEntityID(999);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(0, await ctx.Notifications.CountAsync(n => n.BusinessEntityID == 999));
        }

        [Fact]
        public async Task DeleteByBusinessEntityID_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.DeleteByBusinessEntityID(12345);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteById_RemovesEntity_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            SeedNotification(ctx, id: 42, businessEntityId: 1000, message: "Del me");

            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.DeleteById(42);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await ctx.Notifications.FirstOrDefaultAsync(x => x.ID == 42));
        }

        [Fact]
        public async Task DeleteById_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreateNotificationMapperMock();
            var controller = new NotificationController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockNotification().Object);

            var result = await controller.DeleteById(777);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
