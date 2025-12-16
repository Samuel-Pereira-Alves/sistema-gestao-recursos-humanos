using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;
using sistema_gestao_recursos_humanos.Tests.Utils;

using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class DepartmentHistoryControllerTests
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

        // --------------------------- GET -------------------------------------

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            // Arrange
            var ctx = BuildContext();
            SeedBasicData(ctx, addHistory: true);
            SeedBasicData(ctx, businessEntityId: 101, departmentId: 2, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dtoList = Assert.IsAssignableFrom<List<DepartmentHistoryDto>>(ok.Value);
            Assert.Equal(2, dtoList.Count);
            Assert.Contains(dtoList, x => x.BusinessEntityID == 100 && x.DepartmentId == 1);
            Assert.Contains(dtoList, x => x.BusinessEntityID == 101 && x.DepartmentId == 2);
        }

        // --------------------------- CREATE ----------------------------------
        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();
            // FK válidas
            SeedBasicData(ctx, addHistory: false);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object);

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

            // Route values
            Assert.Equal(100, created!.RouteValues!["businessEntityId"]!);
            Assert.Equal((short)1, created.RouteValues["departmentId"]);
            Assert.Equal((byte)2, created.RouteValues["shiftId"]);
            Assert.Equal(dto.StartDate, created.RouteValues["startDate"]);

            // Body
            var bodyDto = Assert.IsType<DepartmentHistoryDto>(created.Value);
            Assert.Equal(100, bodyDto.BusinessEntityID);
            Assert.Equal(1, bodyDto.DepartmentId);
            Assert.Equal((byte)2, bodyDto.ShiftID);
            Assert.Equal(dto.StartDate, bodyDto.StartDate);

            // Persistência
            var saved = await ctx.DepartmentHistories.FirstOrDefaultAsync(h =>
                h.BusinessEntityID == 100 && h.DepartmentID == 1 && h.ShiftID == 2 && h.StartDate == dto.StartDate);
            Assert.NotNull(saved);
            Assert.True((DateTime.Now - saved.ModifiedDate).TotalSeconds < 5); // setado pelo controller
        }




        [Fact]
        public async Task Update_ReturnsNoContent_AndUpdatesEntity()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object);

            var dto = new DepartmentHistoryDto
            {
                DepartmentId = 1,        // igual
                ShiftID = 1,             // igual (não alteramos PK)
                EndDate = new DateTime(2024, 02, 01)
            };

            var result = await controller.Update(100, 1, 1, start, dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await ctx.DepartmentHistories
                .FirstOrDefaultAsync(h => h.BusinessEntityID == 100 && h.DepartmentID == 1 && h.StartDate == start);
            Assert.NotNull(updated);
            Assert.Equal((byte)1, updated.ShiftID);
            Assert.Equal(dto.EndDate, updated.EndDate);
            Assert.True((DateTime.Now - updated.ModifiedDate).TotalSeconds < 5);
        }



        // --------------------------- PATCH -----------------------------------


        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object);

            var dto = new DepartmentHistoryDto
            {
                EndDate = new DateTime(2024, 03, 10) // apenas campo não-PK
            };

            var result = await controller.Patch(100, 1, 1, start, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var bodyDto = Assert.IsType<DepartmentHistoryDto>(ok.Value);
            Assert.Equal(1, bodyDto.DepartmentId);  // PK inalterada
            Assert.Equal((byte)1, bodyDto.ShiftID); // PK inalterada
            Assert.Equal(dto.EndDate, bodyDto.EndDate);

            var updated = await ctx.DepartmentHistories
                .FirstOrDefaultAsync(h => h.BusinessEntityID == 100 && h.StartDate == start);
            Assert.NotNull(updated);
            Assert.Equal((short)1, updated.DepartmentID);
            Assert.Equal((byte)1, updated.ShiftID);
            Assert.Equal(dto.EndDate, updated.EndDate);
            Assert.True((DateTime.Now - updated.ModifiedDate).TotalSeconds < 5);
        }



        // --------------------------- DELETE ----------------------------------

        [Fact]
        public async Task Delete_RemovesEntity_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            var start = new DateTime(2020, 01, 01);
            SeedBasicData(ctx, startDate: start, addHistory: true);

            var mapperMock = MapperMockFactory.CreateDepartmentHistoryMapperMock();
            var controller = new DepartmentHistoryController(ctx, mapperMock.Object);

            var result = await controller.Delete(100, 1, 1, start);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await ctx.DepartmentHistories
                .FirstOrDefaultAsync(h => h.BusinessEntityID == 100 && h.DepartmentID == 1 && h.ShiftID == 1 && h.StartDate == start));
        }

    }


}