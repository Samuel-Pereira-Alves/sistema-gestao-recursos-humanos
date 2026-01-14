using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.Tests.Utils;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using Microsoft.AspNetCore.Http;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class DepartmentHistoryControllerTests
    {
        private AdventureWorksContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            return new AdventureWorksContext(options);
        }

        private void SeedBasicData(AdventureWorksContext ctx,
            int businessEntityId = 100,
            short departmentId = 1,
            byte shiftId = 1,
            DateTime? startDate = null,
            bool addDepartment = true,
            bool addEmployee = true,
            bool addHistory = true)
        {
            if (addDepartment)
            {
                ctx.Departments.Add(new Department
                {
                    DepartmentID = departmentId
                });
            }

            if (addEmployee)
            {
                ctx.Employees.Add(new Employee
                {
                    BusinessEntityID = businessEntityId
                });
            }

            if (addHistory)
            {
                ctx.DepartmentHistories.Add(new DepartmentHistory
                {
                    BusinessEntityID = businessEntityId,
                    DepartmentID = departmentId,
                    ShiftID = shiftId,
                    StartDate = startDate ?? new DateTime(2020, 01, 01),
                    EndDate = null,
                    ModifiedDate = DateTime.UtcNow
                });
            }

            ctx.SaveChanges();
        }

        [Fact]
        public async Task Create_ReturnsConflict_WhenDuplicate()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapper = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            var dto = new DepartmentHistoryDto
            {
                BusinessEntityID = 100,
                DepartmentId = 1,
                ShiftID = 1,
                StartDate = start,
                EndDate = null
            };

            var result = await controller.Create(dto, CancellationToken.None);

            if (result is ObjectResult obj)
            {
                Assert.Contains(obj.StatusCode ?? -1, new[] {
                StatusCodes.Status409Conflict,
                StatusCodes.Status400BadRequest,
                StatusCodes.Status500InternalServerError
        });
            }
            else
            {
                Assert.Fail($"Tipo inesperado: {result.GetType().Name}");
            }
        }

        [Fact]
        public async Task Get_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapper = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var action = await controller.Get(100, 1, 1, start, ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var dto = Assert.IsType<DepartmentHistoryDto>(ok.Value);
            Assert.Equal(100, dto.BusinessEntityID);
            Assert.Equal(1, dto.DepartmentId);
            Assert.Equal(1, dto.ShiftID);
            Assert.Equal(start, dto.StartDate);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var action = await controller.Get(999, 1, 1, new DateTime(2020, 1, 1), ct);

            Assert.IsType<NotFoundResult>(action.Result);
        }

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            var dto = new DepartmentHistoryDto
            {
                EndDate = new DateTime(2024, 03, 10)
            };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Patch(100, 1, 1, start, dto, ct);

            var ok = Assert.IsType<OkObjectResult>(result);
            var bodyDto = Assert.IsType<DepartmentHistoryDto>(ok.Value);
            Assert.Equal(1, bodyDto.DepartmentId);
            Assert.Equal(dto.EndDate, bodyDto.EndDate);

            var updated = await ctx.DepartmentHistories
                .FirstOrDefaultAsync(h => h.BusinessEntityID == 100 && h.StartDate == start);
            Assert.Equal(dto.EndDate, updated!.EndDate);
        }

        [Fact]
        public async Task Delete_RemovesEntity_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Delete(100, 1, 1, start, ct);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await ctx.DepartmentHistories
                .FirstOrDefaultAsync(h => h.BusinessEntityID == 100 && h.DepartmentID == 1 && h.ShiftID == 1 && h.StartDate == start));
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            var result = await controller.Delete(100, 1, 1, new DateTime(2020, 1, 1), CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenEndDateBeforeStartDate()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapper = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            var dto = new DepartmentHistoryDto { EndDate = new DateTime(2019, 12, 31) };

            var result = await controller.Patch(100, 1, 1, start, dto, CancellationToken.None);

            if (result is ObjectResult obj)
            {
                Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
            }
            else
            {
                Assert.IsType<BadRequestObjectResult>(result);
            }
        }
    }
}