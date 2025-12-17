using Microsoft.EntityFrameworkCore;
using sistema_gestao_recursos_humanos.backend.data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AdventureWorksContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Lê secrets do appsettings.json / User Secrets / Key Vault:
var issuer  = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var key     = builder.Configuration["Jwt:Key"];

// 1) Authentication: define esquemas por defeito e adiciona JWT Bearer
builder.Services
    .AddAuthentication(options =>
    {
        // ✅ Defaults (evita a exceção “No DefaultChallengeScheme”)
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;   // em Dev podes colocar false
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer   = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
            ClockSkew = TimeSpan.Zero // tokens expiram exatamente no horário
        };
    });

// 2) Authorization (roles/policies opcionais)

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));

    options.AddPolicy("EmployeeOrAdmin", policy => policy.RequireRole("employee", "admin"));

    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("employee"));
});


builder.Services.AddControllers();

builder.Services.AddAuthorization();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
            .AllowAnyHeader()
            .AllowAnyMethod());
});


var app = builder.Build();
app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseAuthentication();  
app.UseAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();


app.MapControllers();

app.Run();
