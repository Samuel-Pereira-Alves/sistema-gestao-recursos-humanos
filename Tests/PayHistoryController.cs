using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
    public class PayHistoryControllerTests
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

        private Mock<IMapper> CreatePayHistoryMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Map<List<PayHistoryDto>>(List<PayHistory>)
            m.Setup(x => x.Map<List<PayHistoryDto>>(It.IsAny<List<PayHistory>>()))
             .Returns((List<PayHistory> src) => src?.Select(ph => new PayHistoryDto
             {
                 BusinessEntityID = ph.BusinessEntityID,
                 RateChangeDate = ph.RateChangeDate,
                 Rate = ph.Rate,
                 PayFrequency = ph.PayFrequency
             }).ToList() ?? new List<PayHistoryDto>());

            // Map<PayHistoryDto>(PayHistory)
            m.Setup(x => x.Map<PayHistoryDto>(It.IsAny<PayHistory>()))
             .Returns((PayHistory ph) => new PayHistoryDto
             {
                 BusinessEntityID = ph.BusinessEntityID,
                 RateChangeDate = ph.RateChangeDate,
                 Rate = ph.Rate,
                 PayFrequency = ph.PayFrequency
             });

            // Map<PayHistory>(PayHistoryDto)
            m.Setup(x => x.Map<PayHistory>(It.IsAny<PayHistoryDto>()))
             .Returns((PayHistoryDto dto) => new PayHistory
             {
                 BusinessEntityID = dto.BusinessEntityID,
                 RateChangeDate = dto.RateChangeDate,
                 Rate = dto.Rate,
                 PayFrequency = dto.PayFrequency
                 // ModifiedDate setado no controller
             });

            // Map(source, destination) → PUT
            m.Setup(x => x.Map(It.IsAny<PayHistoryDto>(), It.IsAny<PayHistory>()))
             .Returns((PayHistoryDto src, PayHistory dest) =>
             {
                 if (src != null && dest != null)
                 {
                     // NÃO alterar chave composta
                     if (src.Rate != default(decimal)) dest.Rate = src.Rate;
                     if (src.PayFrequency != default(byte)) dest.PayFrequency = src.PayFrequency;
                 }
                 return dest!;
             });

            return m;
        }

        // --------------------------- GET ALL ---------------------------------

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            var ctx = BuildContext();
            SeedPayHistory(ctx, 100, new DateTime(2021, 1, 1), 10.5m, 1);
            SeedPayHistory(ctx, 101, new DateTime(2022, 1, 1), 12.75m, 2);

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<PayHistoryDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.Contains(list, d => d.BusinessEntityID == 100 && d.Rate == 10.5m && d.PayFrequency == 1);
            Assert.Contains(list, d => d.BusinessEntityID == 101 && d.Rate == 12.75m && d.PayFrequency == 2);
        }

        // ------------------ GET ALL BY EMPLOYEE -------------------------------

        [Fact]
        public async Task GetAllByEmployee_ReturnsOk_OrderedDescendingByRateChangeDate()
        {
            var ctx = BuildContext();
            SeedPayHistory(ctx, 100, new DateTime(2023, 01, 01), 15m, 1);
            SeedPayHistory(ctx, 100, new DateTime(2024, 01, 01), 20m, 1);
            SeedPayHistory(ctx, 101, new DateTime(2025, 01, 01), 25m, 2); // outro employee

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var result = await controller.GetAllByEmployee(100);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<PayHistoryDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.True(list[0].RateChangeDate > list[1].RateChangeDate); // desc
            Assert.All(list, d => Assert.Equal(100, d.BusinessEntityID));
        }

        // --------------------------- GET (composed) ---------------------------

        [Fact]
        public async Task Get_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            var changeDate = new DateTime(2022, 05, 10);
            SeedPayHistory(ctx, 200, changeDate, 30m, 2);

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var result = await controller.Get(200, changeDate);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<PayHistoryDto>(ok.Value);
            Assert.Equal(200, dto.BusinessEntityID);
            Assert.Equal(changeDate, dto.RateChangeDate);
            Assert.Equal(30m, dto.Rate);
            Assert.Equal((byte)2, dto.PayFrequency);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var result = await controller.Get(999, new DateTime(2020, 01, 01));

            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- CREATE ----------------------------------

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = CreatePayHistoryMapperMock();
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
            Assert.Equal(300, created!.RouteValues!["businessEntityId"]!);
            Assert.Equal(dto.RateChangeDate, created.RouteValues["rateChangeDate"]);

            var body = Assert.IsType<PayHistoryDto>(created.Value);
            Assert.Equal(300, body.BusinessEntityID);
            Assert.Equal(dto.RateChangeDate, body.RateChangeDate);
            Assert.Equal(40.00m, body.Rate);
            Assert.Equal((byte)1, body.PayFrequency);

            var saved = await ctx.PayHistories.FirstOrDefaultAsync(ph =>
                ph.BusinessEntityID == 300 && ph.RateChangeDate == dto.RateChangeDate);
            Assert.NotNull(saved);
            Assert.InRange(saved.ModifiedDate, before.AddSeconds(-1), after.AddSeconds(1));
        }

        // --------------------------- UPDATE (PUT) -----------------------------

        [Fact]
        public async Task Update_ReturnsNoContent_AndUpdatesEntity()
        {
            var ctx = BuildContext();
            var changeDate = new DateTime(2023, 02, 02);
            SeedPayHistory(ctx, 400, changeDate, 50m, 1);

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto
            {
                BusinessEntityID = 400,     // igual à rota (não muda chave)
                RateChangeDate = changeDate,
                Rate = 55m,
                PayFrequency = 2
            };

            var result = await controller.Update(400, changeDate, dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await ctx.PayHistories.FirstOrDefaultAsync(ph =>
                ph.BusinessEntityID == 400 && ph.RateChangeDate == changeDate);
            Assert.NotNull(updated);
            Assert.Equal(55m, updated.Rate);
            Assert.Equal((byte)2, updated.PayFrequency);
            Assert.True((DateTime.Now - updated.ModifiedDate).TotalSeconds < 5);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto
            {
                BusinessEntityID = 999,
                RateChangeDate = new DateTime(2020, 01, 01),
                Rate = 10m,
                PayFrequency = 1
            };

            var result = await controller.Update(999, dto.RateChangeDate, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- PATCH -----------------------------------

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            var changeDate = new DateTime(2024, 09, 15);
            SeedPayHistory(ctx, 500, changeDate, 60m, 1);

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto
            {
                Rate = 62.5m,      // != default => atualiza
                PayFrequency = 2   // != default => atualiza
            };

            var result = await controller.Patch(500, changeDate, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<PayHistoryDto>(ok.Value);
            Assert.Equal(500, body.BusinessEntityID);
            Assert.Equal(changeDate, body.RateChangeDate);
            Assert.Equal(62.5m, body.Rate);
            Assert.Equal((byte)2, body.PayFrequency);

            var updated = await ctx.PayHistories.FirstOrDefaultAsync(ph =>
                ph.BusinessEntityID == 500 && ph.RateChangeDate == changeDate);
            Assert.NotNull(updated);
            Assert.Equal(62.5m, updated.Rate);
            Assert.Equal((byte)2, updated.PayFrequency);
            Assert.True((DateTime.Now - updated.ModifiedDate).TotalSeconds < 5);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();

            var mapper = CreatePayHistoryMapperMock();
            var controller = new PayHistoryController(ctx, mapper.Object);

            var dto = new PayHistoryDto { Rate = 99m };

            var result = await controller.Patch(999, new DateTime(2000, 01, 01), dto);

            Assert.IsType<NotFoundResult>(result);
        }

    }
}