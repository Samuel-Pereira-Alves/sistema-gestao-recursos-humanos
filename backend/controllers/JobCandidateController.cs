using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using sistema_gestao_recursos_humanos.backend.data;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.backend.controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class JobCandidateController : ControllerBase
    {
        private readonly AdventureWorksContext _db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public JobCandidateController(AdventureWorksContext db, IMapper mapper, IWebHostEnvironment env)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
        }

        // GET: api/v1/jobcandidate
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var candidates = await _db.JobCandidates.ToListAsync();
            var dto = _mapper.Map<List<JobCandidateDto>>(candidates);
            return Ok(dto);
        }

        // GET: api/v1/jobcandidate/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var candidate = await _db.JobCandidates
                .FirstOrDefaultAsync(jc => jc.JobCandidateId == id);

            if (candidate == null) return NotFound();

            var dto = _mapper.Map<JobCandidateDto>(candidate);
            return Ok(dto);
        }

        // POST: api/v1/jobcandidate
        [HttpPost]
        public async Task<IActionResult> Create(JobCandidateDto dto)
        {
            var candidate = _mapper.Map<JobCandidate>(dto);
            candidate.ModifiedDate = DateTime.Now;

            _db.JobCandidates.Add(candidate);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get),
                new { id = candidate.JobCandidateId },
                _mapper.Map<JobCandidateDto>(candidate));
        }


        // POST: api/v1/jobcandidates/upload
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50 MB (ajusta conforme necessidade)
        public async Task<IActionResult> UploadCv([FromForm] IFormFile cv)
        {
            if (cv == null || cv.Length == 0)
                return BadRequest(new { message = "Nenhum ficheiro enviado." });

            if (!string.Equals(cv.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "O ficheiro deve ser um PDF." });

            // 1) Guardar PDF no servidor (wwwroot/uploads/cv/)
            var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "cv");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await cv.CopyToAsync(stream);
            }

            // 2) Construir XML com metadados + caminho
            // (podes usar XElement/XmlDocument; aqui string simples)
            // Regra de negócio: guarda só o essencial
            var now = DateTime.Now;
            var relativeUrl = $"/uploads/cv/{safeFileName}";

            var resumeXml = BuildAdventureWorksResumeMinimal(
                firstName: "",
                lastName: "",
                skills: "",
                email: ""
                );


            // 3) Persistir JobCandidate
            var jc = new JobCandidate
            {
                BusinessEntityId = null,           // candidato ainda não aprovado
                Resume = resumeXml,               // XML
                ModifiedDate = now
            };

            _db.JobCandidates.Add(jc);
            await _db.SaveChangesAsync();

            return Created($"/api/v1/jobcandidate/{jc.JobCandidateId}", new
            {
                jobCandidateId = jc.JobCandidateId,
                fileUrl = relativeUrl
            });
        }



        private static string BuildAdventureWorksResumeMinimal(
            string? firstName = "",
            string? lastName = "",
            string? skills = "",
            string? email = "")
        {
            XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/Resume";

            // Datas válidas exigidas pelo XSD
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd'Z'");
            string defaultStart = "2020-01-01Z";
            string defaultEnd = "2024-01-01Z";

            var doc = new XDocument(
                new XElement(ns + "Resume",
                    new XAttribute(XNamespace.Xmlns + "ns", ns.NamespaceName),

                    // 1. Name
                    new XElement(ns + "Name",
                        new XElement(ns + "Name.Prefix"),
                        new XElement(ns + "Name.First", firstName ?? "Unknown"),
                        new XElement(ns + "Name.Middle"),
                        new XElement(ns + "Name.Last", lastName ?? "Unknown"),
                        new XElement(ns + "Name.Suffix")
                    ),

                    // 2. Skills
                    new XElement(ns + "Skills", skills ?? "N/A"),

                    // 3. Employment (mínimo 1 obrigatório)
                    new XElement(ns + "Employment",
                        new XElement(ns + "Emp.StartDate", defaultStart),
                        new XElement(ns + "Emp.EndDate", defaultEnd),
                        new XElement(ns + "Emp.OrgName", "Unknown"),
                        new XElement(ns + "Emp.JobTitle", "Unknown"),
                        new XElement(ns + "Emp.Responsibility", "N/A"),
                        new XElement(ns + "Emp.FunctionCategory", "N/A"),
                        new XElement(ns + "Emp.IndustryCategory", "N/A"),
                        new XElement(ns + "Emp.Location",
                            new XElement(ns + "Location",
                                new XElement(ns + "Loc.CountryRegion", "N/A"),
                                new XElement(ns + "Loc.State", "N/A"),
                                new XElement(ns + "Loc.City", "N/A")
                            )
                        )
                    ),

                    // 4. Education (obrigatória no teu XSD)
                    new XElement(ns + "Education",
                        new XElement(ns + "Edu.Level", "Bachelor"),
                        new XElement(ns + "Edu.StartDate", defaultStart),
                        new XElement(ns + "Edu.EndDate", defaultEnd),
                        new XElement(ns + "Edu.Degree", "Unknown"),
                        new XElement(ns + "Edu.Major", "Unknown"),
                        new XElement(ns + "Edu.Minor", "Unknown"),
                        new XElement(ns + "Edu.GPA", "0.0"),
                        new XElement(ns + "Edu.GPAScale", "4"),
                        new XElement(ns + "Edu.School", "Unknown"),
                        new XElement(ns + "Edu.Location",
                            new XElement(ns + "Location",
                                new XElement(ns + "Loc.CountryRegion", "N/A"),
                                new XElement(ns + "Loc.State", "N/A"),
                                new XElement(ns + "Loc.City", "N/A")
                            )
                        )
                    ),

                    // 5. Address
                    new XElement(ns + "Address",
                        new XElement(ns + "Addr.Type", "Home"),
                        new XElement(ns + "Addr.Street", "N/A"),
                        new XElement(ns + "Addr.Location",
                            new XElement(ns + "Location",
                                new XElement(ns + "Loc.CountryRegion", "N/A"),
                                new XElement(ns + "Loc.State", "N/A"),
                                new XElement(ns + "Loc.City", "N/A")
                            )
                        ),
                        new XElement(ns + "Addr.PostalCode", "0000-000"),
                        new XElement(ns + "Addr.Telephone",
                            new XElement(ns + "Telephone",
                                new XElement(ns + "Tel.Type", "Voice"),
                                new XElement(ns + "Tel.IntlCode", "351"),
                                new XElement(ns + "Tel.AreaCode", "21"),
                                new XElement(ns + "Tel.Number", "0000000")
                            )
                        )
                    ),

                    // 6. EMail
                    new XElement(ns + "EMail", email ?? "unknown@example.com"),

                    // 7. WebSite
                    new XElement(ns + "WebSite", "")
                )
            );
            return doc.ToString(SaveOptions.DisableFormatting);
        }
    }
}