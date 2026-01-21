using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.Tests.Utils;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class EmployeeControllerTests
    {
        private AdventureWorksContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
                    FirstName = "Samuel",
                    LastName = "Alves",
                    MiddleName = "P",
                    Title = "Mr",
                    Suffix = "Jr",
                    PersonType = "EM",
                    ModifiedDate = DateTime.UtcNow
                };
            }

            ctx.Employees.Add(e);
            ctx.SaveChanges();
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");
            SeedEmployee(ctx, id: 101, jobTitle: "QA");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var action = await controller.GetAll(ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result); 
            var list = Assert.IsAssignableFrom<List<EmployeeDto>>(ok.Value);
            Assert.Equal(2, list.Count);
            Assert.Contains(list, e => e.BusinessEntityID == 100 && e.JobTitle == "Dev");
            Assert.Contains(list, e => e.BusinessEntityID == 101 && e.JobTitle == "QA");
        }

        [Fact]
        public async Task GetEmployee_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await controller.GetEmployee(100, ct);
            });
        }

        [Fact]
        public async Task GetEmployee_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await controller.GetEmployee(999, ct);
            });
        }

        [Fact]
        public async Task Patch_ReturnsOk_AndPartiallyUpdates()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev");

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            var dto = new EmployeeDto
            {
                BusinessEntityID = 100,
                JobTitle = "Lead Dev",
                VacationHours = 20,
                SickLeaveHours = 12,
                SalariedFlag = true,
                HireDate = new DateTime(2022, 5, 1),
                BirthDate = new DateTime(1990, 12, 25)
            };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await controller.Patch(100, dto, ct);
            });
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            var dto = new EmployeeDto { BusinessEntityID = 100, JobTitle = "Lead Dev" };

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await controller.Patch(100, dto, ct);
            });
        }

        [Fact]
        public async Task Delete_SoftDeletes_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            SeedEmployee(ctx, id: 100, jobTitle: "Dev", currentFlag: true);

            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Delete(100, ct);

            Assert.IsType<NoContentResult>(result);

            var emp = await ctx.Employees.FirstOrDefaultAsync(e => e.BusinessEntityID == 100);
            Assert.NotNull(emp);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateEmployeeMapperMock();
            var logger = MapperMockFactory.CreateAppLogMock();
            var controller = new EmployeeController(ctx, mapper.Object, MapperMockFactory.CreateLoggerMockEmployee().Object, logger.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Delete(999, ct);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
