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
    public class PayHistoryControllerTests
    {
        private AdventureWorksContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            return new AdventureWorksContext(options);
        }

        private void SeedPayHistory(
            AdventureWorksContext ctx,
            int businessEntityId,
            DateTime rateChangeDate,
            decimal rate,
            byte payFrequency)
        {
            ctx.PayHistories.Add(new PayHistory
            {
                BusinessEntityID = businessEntityId,
                RateChangeDate = rateChangeDate,
                Rate = rate,
                PayFrequency = payFrequency,
                ModifiedDate = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            var ctx = BuildContext();
            SeedPayHistory(ctx, 100, new DateTime(2021, 1, 1), 10.5m, 1);
            SeedPayHistory(ctx, 101, new DateTime(2022, 1, 1), 12.75m, 2);

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<PayHistoryDto>>(ok.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var result = await controller.Get(999, new DateTime(2020, 01, 01));

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto
            {
                BusinessEntityID = 300,
                RateChangeDate = new DateTime(2024, 06, 01),
                Rate = 40.00m,
                PayFrequency = 1
            };

            var before = DateTime.Now;
            var result = await controller.Create(dto);
            var after = DateTime.Now;

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(PayHistoryController.Get), created.ActionName);

            var saved = await ctx.PayHistories.FirstOrDefaultAsync(ph =>
                ph.BusinessEntityID == 300 && ph.RateChangeDate == dto.RateChangeDate);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            var changeDate = new DateTime(2024, 09, 15);
            SeedPayHistory(ctx, 500, changeDate, 60m, 1);

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto
            {
                Rate = 62.5m,
                PayFrequency = 2
            };

            var result = await controller.Patch(500, changeDate, dto);

            Assert.IsType<OkObjectResult>(result);

            var updated = await ctx.PayHistories.FirstOrDefaultAsync(ph =>
                ph.BusinessEntityID == 500 && ph.RateChangeDate == changeDate);
            Assert.NotNull(updated);
            Assert.Equal(62.5m, updated.Rate);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto { Rate = 99m };

            var result = await controller.Patch(999, new DateTime(2000, 01, 01), dto);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}