using Microsoft.AspNetCore.Authentication.JwtBearer;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using Npgsql.NameTranslation;
using SGM.Core.Enums;
using SGM.Core.Interfaces.Repositories;
using SGM.Core.Interfaces.Services;
using SGM.Core.Repositories;
using SGM.Infrastructure.Data;
using SGM.Infrastructure.Repositories;
using SGM.Infrastructure.Services;
using SMG.Core.Enums;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;
using System.Text;

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
var translator = new NpgsqlSnakeCaseNameTranslator();
dataSourceBuilder.MapEnum<EstadoPago>  ("sgm.estado_pago",   translator);
dataSourceBuilder.MapEnum<MetodoPago>  ("sgm.metodo_pago",   translator);
dataSourceBuilder.MapEnum<EstadoDeuda> ("sgm.estado_deuda",  translator);
dataSourceBuilder.MapEnum<EstadoPuesto>("sgm.estado_puesto", translator);
dataSourceBuilder.MapEnum<RolUsuario>  ("sgm.rol_usuario",   translator);
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
    });

builder.Services.AddAuthorization();

// ─── DI: Repositorios y Servicios ────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository,      UsuarioRepository>();
builder.Services.AddScoped<IPuestoRepository,       PuestoRepository>();
builder.Services.AddScoped<IDeudaRepository,        DeudaRepository>();
builder.Services.AddScoped<IPagoRepository,         PagoRepository>();
builder.Services.AddScoped<IConceptoCobroRepository,ConceptoCobroRepository>();
builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();

builder.Services.AddScoped<IAuthService,            AuthService>();
builder.Services.AddScoped<IPuestoService,          PuestoService>();
builder.Services.AddScoped<IDeudaService,           DeudaService>();
builder.Services.AddScoped<IPagoService,            PagoService>();
builder.Services.AddScoped<IConceptoCobroService,   ConceptoCobroService>();
builder.Services.AddScoped<IUsuarioService,         UsuarioService>();
builder.Services.AddScoped<INotificacionService,    NotificacionService>();

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

app.UseCors("Frontend");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MercaGest API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
