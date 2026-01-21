
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.Tests.Utils;
using Microsoft.AspNetCore.Http;

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
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new PayHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockPayHistory().Object,logger.Object);

            var dto = new PayHistoryDto
            {
                BusinessEntityID = 300,
                RateChangeDate = new DateTime(2024, 06, 01),
                Rate = 40.00m,
                PayFrequency = 1
            };

            var before = DateTime.Now;
            using var cts = new CancellationTokenSource();
            var ct = cts.Token;
            var result = await controller.Create(dto, ct);
            var after = DateTime.Now;

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(PayHistoryController.Get), created.ActionName);

            var saved = await ctx.PayHistories.FirstOrDefaultAsync(ph =>
                ph.BusinessEntityID == 300 && ph.RateChangeDate == dto.RateChangeDate);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task Patch_AllowsNoOpOrReturnsBadRequest_WhenNoChanges()
        {
            var ctx = BuildContext();
            var changeDate = new DateTime(2024, 09, 15);
            SeedPayHistory(ctx, 500, changeDate, 60m, 1);

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new PayHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockPayHistory().Object,logger.Object);

            var dto = new PayHistoryDto();
            var result = await controller.Patch(500, changeDate, dto, CancellationToken.None);
            
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            var changeDate = new DateTime(2024, 09, 15);
            SeedPayHistory(ctx, 500, changeDate, 60m, 1);

            var mapper = MapperMockFactory.CreatePayHistoryMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new PayHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockPayHistory().Object, logger.Object);

            var dto = new PayHistoryDto
            {
                Rate = 62.5m,
                PayFrequency = 2
            };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;
            var result = await controller.Patch(500, changeDate, dto, ct);

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
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new PayHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockPayHistory().Object, logger.Object);

            var dto = new PayHistoryDto { Rate = 99m };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;
            var result = await controller.Patch(999, new DateTime(2000, 01, 01), dto, ct);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
