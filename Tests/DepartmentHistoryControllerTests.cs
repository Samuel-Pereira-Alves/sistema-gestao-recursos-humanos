using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.Tests.Utils;

using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

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
        public async Task Create_ReturnsCreated()
        {
            var ctx = BuildContext();
            SeedBasicData(ctx, addHistory: false);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object, MapperMockFactory.CreateLoggerMockDepartment().Object);

            var dto = new DepartmentHistoryDto
            {
                BusinessEntityID = 100,
                DepartmentId = 1,
                ShiftID = 2,
                StartDate = new DateTime(2023, 01, 01),
                EndDate = null
            };

            var result = await controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(DepartmentHistoryController.Get), created.ActionName);
        }

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object,MapperMockFactory.CreateLoggerMockDepartment().Object);

            var dto = new DepartmentHistoryDto
            {
                EndDate = new DateTime(2024, 03, 10) 
            };

            var result = await controller.Patch(100, 1, 1, start, dto);

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

            var result = await controller.Delete(100, 1, 1, start);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await ctx.DepartmentHistories
                .FirstOrDefaultAsync(h => h.BusinessEntityID == 100 && h.DepartmentID == 1 && h.ShiftID == 1 && h.StartDate == start));
        }
    }
}