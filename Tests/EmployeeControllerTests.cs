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
    public class EmployeeControllerTests
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

        private void SeedEmployee(
            AdventureWorksContext ctx,
            int id = 100,
            string? login = "user100",
            string? jobTitle = "Dev",
            bool withPerson = true,
            bool currentFlag = true)
        {
            var e = new Employee
            {
                BusinessEntityID = id,
                LoginID = login!,
                JobTitle = jobTitle!,
                BirthDate = new DateTime(1990, 1, 1),
                HireDate = new DateTime(2020, 1, 1),
                MaritalStatus = "S",
                Gender = "M",
                SalariedFlag = false,
                VacationHours = 10,
                SickLeaveHours = 5,
                NationalIDNumber = "NID100",
                CurrentFlag = currentFlag,
                ModifiedDate = DateTime.UtcNow
            };

            if (withPerson)
            {
                e.Person = new Person
                {
                    BusinessEntityID = id,
                    FirstName = "John",
                    LastName = "Doe",
                    MiddleName = "Q",
                    Title = "Mr",
                    Suffix = "Jr",
                    PersonType = "EM",
                    ModifiedDate = DateTime.UtcNow
                };
            }

            ctx.Employees.Add(e);
            ctx.SaveChanges();
        }

        // --------------------------- GET ALL ---------------------------------

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");
            SeedEmployee(ctx, id: 101, jobTitle: "QA");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<EmployeeDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.Contains(list, e => e.BusinessEntityID == 100 && e.JobTitle == "Dev");
            Assert.Contains(list, e => e.BusinessEntityID == 101 && e.JobTitle == "QA");
        }

        // --------------------------- GET BY ID --------------------------------

        [Fact]
        public async Task GetEmployee_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var result = await controller.GetEmployee(100);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<EmployeeDto>(ok.Value);
            Assert.Equal(100, dto.BusinessEntityID);
            Assert.Equal("Dev", dto.JobTitle);
            Assert.NotNull(dto.Person);
            Assert.Equal("John", dto.Person.FirstName);
            Assert.Equal("Doe", dto.Person.LastName);
        }

        [Fact]
        public async Task GetEmployee_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            // sem seed

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var result = await controller.GetEmployee(999);

            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- CREATE ----------------------------------

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var ctx = BuildContext();

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto
            {
                BusinessEntityID = 200, // no InMemory, chave deve ser fornecida
                LoginID = "new.user",
                JobTitle = "New Hire",
                BirthDate = new DateTime(1995, 4, 10),
                HireDate = new DateTime(2024, 1, 1), // será sobrescrito para Now pelo controller
                MaritalStatus = "S",
                Gender = "F",
                SalariedFlag = true,
                VacationHours = 2,
                SickLeaveHours = 0,
                NationalIDNumber = "NID200",
                Person = new PersonDto
                {
                    FirstName = "Alice",
                    LastName = "Smith"
                }
            };

            var before = DateTime.Now;
            var result = await controller.Create(dto);
            var after = DateTime.Now;

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(EmployeeController.GetEmployee), created.ActionName);
            Assert.Equal(200, created!.RouteValues!["id"]!);

            var body = Assert.IsType<EmployeeDto>(created.Value);
            Assert.Equal(200, body.BusinessEntityID);
            Assert.Equal("New Hire", body.JobTitle);
            Assert.NotNull(body.Person);
            Assert.Equal("Alice", body.Person.FirstName);

            var saved = await ctx.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == 200);
            Assert.NotNull(saved);
            // HireDate e ModifiedDate setados para Now
            Assert.InRange(saved.HireDate, before.AddSeconds(-1), after.AddSeconds(1));
            Assert.InRange(saved.ModifiedDate, before.AddSeconds(-1), after.AddSeconds(1));
        }

        // --------------------------- UPDATE (PUT) -----------------------------

        [Fact]
        public async Task Update_ReturnsNoContent_AndUpdatesEntity()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto
            {
                BusinessEntityID = 100,           // tem de coincidir com {id}
                JobTitle = "Senior Dev",
                LoginID = "user100_updated",
                VacationHours = 15,
                SickLeaveHours = 8,
                SalariedFlag = true,
                BirthDate = new DateTime(1991, 2, 2),
                HireDate = new DateTime(2021, 2, 2)
            };

            var result = await controller.Update(100, dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await ctx.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == 100);
            Assert.NotNull(updated);
            Assert.Equal("Senior Dev", updated.JobTitle);
            Assert.Equal("user100_updated", updated.LoginID);
            Assert.Equal((short)15, updated.VacationHours);
            Assert.Equal((short)8, updated.SickLeaveHours);
            Assert.True(updated.SalariedFlag);
            Assert.Equal(new DateTime(1991, 2, 2), updated.BirthDate);
            Assert.Equal(new DateTime(2021, 2, 2), updated.HireDate);
            Assert.True((DateTime.Now - updated.ModifiedDate).TotalSeconds < 5);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenIdMismatch()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto { BusinessEntityID = 999, JobTitle = "X" };

            var result = await controller.Update(100, dto);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            // sem seed

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto { BusinessEntityID = 100, JobTitle = "Any" };

            var result = await controller.Update(100, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- PATCH -----------------------------------

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto
            {
                BusinessEntityID = 100,
                JobTitle = "Lead Dev",          // string
                VacationHours = 20,             // short != default
                SickLeaveHours = 12,            // short != default
                SalariedFlag = true,            // bool != default
                HireDate = new DateTime(2022, 5, 1), // date != default
                BirthDate = new DateTime(1990, 12, 25)
            };

            var result = await controller.Patch(100, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<EmployeeDto>(ok.Value);
            Assert.Equal(100, body.BusinessEntityID);
            Assert.Equal("Lead Dev", body.JobTitle);
            Assert.Equal((short)20, body.VacationHours);
            Assert.Equal((short)12, body.SickLeaveHours);
            Assert.True(body.SalariedFlag);
            Assert.Equal(new DateTime(2022, 5, 1), body.HireDate);
            Assert.Equal(new DateTime(1990, 12, 25), body.BirthDate);

            var updated = await ctx.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == 100);
            Assert.NotNull(updated);
            Assert.Equal("Lead Dev", updated.JobTitle);
            Assert.Equal((short)20, updated.VacationHours);
            Assert.Equal((short)12, updated.SickLeaveHours);
            Assert.True(updated.SalariedFlag);
            Assert.Equal(new DateTime(2022, 5, 1), updated.HireDate);
            Assert.Equal(new DateTime(1990, 12, 25), updated.BirthDate);
            Assert.True((DateTime.Now - updated.ModifiedDate).TotalSeconds < 5);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenIdMismatch()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto { BusinessEntityID = 999, JobTitle = "Lead Dev" };

            var result = await controller.Patch(100, dto);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            // sem seed
            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var dto = new EmployeeDto { BusinessEntityID = 100, JobTitle = "Lead Dev" };

            var result = await controller.Patch(100, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------- DELETE ----------------------------------

        [Fact]
        public async Task Delete_SoftDeletes_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev", currentFlag: true);

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var result = await controller.Delete(100);

            Assert.IsType<NoContentResult>(result);

            var emp = await ctx.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == 100);
            Assert.NotNull(emp);
            Assert.False(emp.CurrentFlag); // "soft delete"
            Assert.True((DateTime.Now - emp.ModifiedDate).TotalSeconds < 5);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var controller = new EmployeeController(ctx, mapper.Object);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
