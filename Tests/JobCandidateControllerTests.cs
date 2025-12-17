using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;
using sistema_gestao_recursos_humanos.Tests.Utils;
using Microsoft.Extensions.Logging;

namespace sistema_gestao_recursos_humanos.Tests.Controllers
{
    public class JobCandidateControllerTests
    {
        private AdventureWorksContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            return new AdventureWorksContext(options);
        }

        private static IFormFile CreateFormFile(byte[] bytes, string fileName, string contentType = "application/pdf")
        {
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, name: "cv", fileName: fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static DefaultHttpContext CreateHttpContext(string scheme = "http", string host = "localhost")
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Scheme = scheme;
            ctx.Request.Host = new HostString(host);
            return ctx;
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithMappedList()
        {
            var ctx = BuildContext();

            ctx.JobCandidates.AddRange(
                new JobCandidate { JobCandidateId = 1, FirstName = "A", LastName = "Z", ModifiedDate = new DateTime(2023, 1, 1) },
                new JobCandidate { JobCandidateId = 2, FirstName = "B", LastName = "Y", ModifiedDate = new DateTime(2024, 1, 1) }
            );
            await ctx.SaveChangesAsync();

            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<JobCandidateDto>>(ok.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task Get_ReturnsOk_WhenFound()
        {
            var ctx = BuildContext();
            ctx.JobCandidates.Add(new JobCandidate
            {
                JobCandidateId = 42,
                FirstName = "Alice",
                LastName = "Doe",
                ModifiedDate = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "jcc-tests-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);

            var result = await controller.Get(42);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<JobCandidateDto>(ok.Value);
            Assert.Equal(42, dto.JobCandidateId);
            Assert.Equal("Alice", dto.FirstName);
            Assert.Equal("Doe", dto.LastName);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);

            var result = await controller.Get(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UploadAndCreate_ReturnsCreated_WritesFile_AndPersists()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var tempRoot = Path.Combine(Path.GetTempPath(), "test-root");
            var env = MapperMockFactory.CreateEnvMock(tempRoot);
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = CreateHttpContext(scheme: "http", host: "localhost")
                }
            };

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x54, 0x45, 0x53, 0x54 };
            var file = CreateFormFile(pdfBytes, "cv.pdf");

            var form = new JobCandidateCreateForm
            {
                Cv = file,
                FirstName = "Carol",
                LastName = "Johnson",
                NationalIDNumber = "NID-001",
                BirthDate = new DateTime(1988, 3, 10),
                MaritalStatus = "M",
                Gender = "F"
            };

            var result = await controller.UploadAndCreate(form, CancellationToken.None);

            var created = Assert.IsType<CreatedResult>(result);

            var value = created.Value!;
            var fileUrl = (string)value.GetType().GetProperty("fileUrl")!.GetValue(value)!;

            Assert.StartsWith("http://localhost/uploads/cv/", fileUrl);

            var relative = new Uri(fileUrl).AbsolutePath; 
            var diskPath = Path.Combine(env.Object.WebRootPath!, relative.TrimStart('/'));
            Assert.True(File.Exists(diskPath));
        }

        [Fact]
        public async Task UploadAndCreate_ReturnsBadRequest_WhenNoFile()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
            };

            var form = new JobCandidateCreateForm
            {
                Cv = null!,
                FirstName = "Test",
                LastName = "User"
            };

            var result = await controller.UploadAndCreate(form, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadAndCreate_ReturnsBadRequest_WhenWrongExtension()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
            };

            var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; 
            var file = CreateFormFile(bytes, "cv.txt", contentType: "text/plain");

            var form = new JobCandidateCreateForm { Cv = file, FirstName = "X", LastName = "Y" };

            var result = await controller.UploadAndCreate(form, CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
            Assert.Equal("O ficheiro deve ser um PDF (.pdf).", msg);
        }

        [Fact]
        public async Task UploadAndCreate_ReturnsBadRequest_WhenInvalidPdfContent()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() }
            };

            var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
            var file = CreateFormFile(bytes, "cv.pdf");

            var form = new JobCandidateCreateForm { Cv = file, FirstName = "X", LastName = "Y" };

            var result = await controller.UploadAndCreate(form, CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var msg = (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
            Assert.Equal("Conteúdo inválido: o ficheiro não é um PDF válido.", msg);
        }

        [Fact]
        public async Task Delete_RemovesEntity_AndReturnsNoContent()
        {
            var ctx = BuildContext();
            ctx.JobCandidates.Add(new JobCandidate
            {
                JobCandidateId = 777,
                FirstName = "Del",
                LastName = "Me",
                ModifiedDate = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);

            var result = await controller.Delete(777);
            Assert.IsType<NoContentResult>(result);

            Assert.Null(await ctx.JobCandidates.FindAsync(777));
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);

            var result = await controller.Delete(12345);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}