using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SGM.Infrastructure.Data;
using SGM.Infrastructure.Repositories;
using SGM.Infrastructure.Services;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// ─── EF Core + PostgreSQL ─────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService,       AuthService>();

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
