using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using DynamicDataControls.Api.Data;
using DynamicDataControls.Api.Middleware;
using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Repositories;
using DynamicDataControls.Api.Repositories.Interfaces;
using DynamicDataControls.Api.Services;
using DynamicDataControls.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// Serilog
// ─────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ─────────────────────────────────────────────────────────────
// Controllers
// ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────
// EF Core – System DB
// ─────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SystemDb"),
        sql => sql.CommandTimeout(60)));

// ─────────────────────────────────────────────────────────────
// JWT Authentication
// ─────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey   = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
var issuer      = jwtSection["Issuer"]    ?? "DynamicDataControls";
var audience    = jwtSection["Audience"]  ?? "DynamicDataControls";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ControlsManagerPolicy", policy =>
        policy.RequireRole(Roles.ControlsManager));
    options.AddPolicy("SystemAdminPolicy", policy =>
        policy.RequireRole(Roles.SystemAdmin));
});

// ─────────────────────────────────────────────────────────────
// CORS – allow Angular dev server
// ─────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ─────────────────────────────────────────────────────────────
// HttpClient factory (used by AiProxyService)
// ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient();

// ─────────────────────────────────────────────────────────────
// Security audit logger (singleton — stateless, thread-safe)
// Requirements: 8.4, 8.5, 8.7
// ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();

// ─────────────────────────────────────────────────────────────
// Repositories
// Requirements: 1.5, 1.6, 6.5, 6.7
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IMetadataRepository, MetadataRepository>();
builder.Services.AddScoped<IControlsRepository, ControlsRepository>();

// ─────────────────────────────────────────────────────────────
// Application services
// ─────────────────────────────────────────────────────────────
// Requirements: 6.5, 6.6, 6.7, 6.8
builder.Services.AddScoped<IMetadataService, MetadataService>();

// builder.Services.AddScoped<IControlsService,  ControlsService>();
builder.Services.AddScoped<IControlsService, ControlsService>();
// Requirements: 3.1, 3.2, 3.3, 4.1
builder.Services.AddScoped<IQueryBuilderService, QueryBuilderService>();
// Requirements: 4.1, 4.2, 4.3, 5.7, 8.3
builder.Services.AddScoped<IExecutionService, ExecutionService>();
// Requirements: 7.2, 7.3, 7.7, 7.8, 7.11
builder.Services.AddScoped<IAiProxyService,   AiProxyService>();

var app = builder.Build();

// ─────────────────────────────────────────────────────────────
// Middleware pipeline
// ─────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseCors("AllowAngularDev");

// Global exception middleware — Requirements: 4.6, 8.4, 8.5
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ─────────────────────────────────────────────────────────────
// DB initialisation — apply migrations and seed reference data
// Requirements: 10.1, 10.3
// ─────────────────────────────────────────────────────────────
await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

// Expose the generated Program class so WebApplicationFactory<Program> in the
// integration-test project can reference it without InternalsVisibleTo.
// Requirements: 17.2 (integration tests)
public partial class Program { }
