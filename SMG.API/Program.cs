using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using SGM.API.Middleware;
using SGM.Core.Interfaces.Repositories;
using SGM.Core.Interfaces.Services;
using SGM.Core.Repositories;
using SGM.Infrastructure.Data;
using SGM.Infrastructure.Jobs;
using SGM.Infrastructure.Repositories;
using SGM.Infrastructure.Services;
using SMG.Core.Enums;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ─── CORS ─────────────────────────────────────────────────────────────────────
var codespaceName = Environment.GetEnvironmentVariable("CODESPACE_NAME");
var codespaceFrontend = codespaceName is not null
    ? $"https://{codespaceName}-3000.preview.app.github.dev"
    : null;

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = new List<string>
        {
            "http://localhost:3000",
            "https://localhost:3000",
        };
        if (codespaceFrontend is not null) origins.Add(codespaceFrontend);

        policy
            .WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ─── EF Core + PostgreSQL ─────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

// ─── JWT ──────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidAudience            = jwtSettings["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew                = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var blacklist = ctx.HttpContext.RequestServices.GetRequiredService<ITokenBlacklist>();
                var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (!string.IsNullOrEmpty(jti) && blacklist.IsRevoked(jti))
                    ctx.Fail("Token revocado.");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", cfg =>
    {
        cfg.PermitLimit       = 10;
        cfg.Window            = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit        = 0;
    });
    options.RejectionStatusCode = 429;
});

// ─── Token Blacklist (singleton) ─────────────────────────────────────────────
builder.Services.AddSingleton<ITokenBlacklist, TokenBlacklist>();

// ─── FluentValidation ────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<SGM.API.Validators.LoginRequestValidator>();

// ─── DI: Repositorios y Servicios ────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository,      UsuarioRepository>();
builder.Services.AddScoped<IPuestoRepository,       PuestoRepository>();
builder.Services.AddScoped<IDeudaRepository,        DeudaRepository>();
builder.Services.AddScoped<IPagoRepository,         PagoRepository>();
builder.Services.AddScoped<IConceptoCobroRepository,ConceptoCobroRepository>();
builder.Services.AddScoped<INotificacionRepository,  NotificacionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository,  RefreshTokenRepository>();
builder.Services.AddScoped<ITokenRevocadoRepository, TokenRevocadoRepository>();

builder.Services.AddScoped<IAuthService,             AuthService>();
builder.Services.AddScoped<IPuestoService,          PuestoService>();
builder.Services.AddScoped<IDeudaService,           DeudaService>();
builder.Services.AddScoped<IPagoService,            PagoService>();
builder.Services.AddScoped<IConceptoCobroService,   ConceptoCobroService>();
builder.Services.AddScoped<IUsuarioService,         UsuarioService>();
builder.Services.AddScoped<INotificacionService,    NotificacionService>();
builder.Services.AddScoped<IAuditLogRepository,     AuditLogRepository>();

// ─── Background Services ──────────────────────────────────────────────────────
builder.Services.AddHostedService<DeudaAlertaService>();

// ─── Controllers + Swagger ───────────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "MercaGest API",
        Version     = "v1",
        Description = "Sistema de Gestión de Mercado",
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Ingresa: Bearer {token}",
    });
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

// ─── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// Seeder: setea contraseñas BCrypt en usuarios que aún tienen hash vacío
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
    await DataSeeder.SeedPasswordsAsync(db, logger);
}

// Load persisted JWT blacklist into memory
var tokenBlacklist = app.Services.GetRequiredService<ITokenBlacklist>();
try
{
    await tokenBlacklist.LoadFromDatabaseAsync();
}
catch (Exception ex)
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogWarning(ex, "No se pudo cargar el JWT blacklist desde la DB en startup. " +
        "Asegúrate de haber aplicado sgm_v1.6_patch.sql. El servidor arranca igual.");
}

app.UseCors("Frontend");
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MercaGest API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();

app.Run();
