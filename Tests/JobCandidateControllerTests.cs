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

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var action = await controller.Get(42, ct);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var dto = Assert.IsType<JobCandidateDto>(ok.Value);
            Assert.Equal(42, dto.JobCandidateId);

        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenMissing()
        {
            var ctx = BuildContext();
            var mapper = MapperMockFactory.CreateJobCandidateMapperMock();
            var env = MapperMockFactory.CreateEnvMock(Path.Combine(Path.GetTempPath(), "test-root"));
            var logger = new Mock<ILogger<JobCandidateController>>();

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Get(999, ct);
            Assert.IsType<NotFoundResult>(result.Result);
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

            var controller = new JobCandidateController(ctx, mapper.Object, env.Object, logger.Object);
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

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Delete(777, ct);
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

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var result = await controller.Delete(12345, ct);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}