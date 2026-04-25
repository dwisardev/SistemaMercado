using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SGM.Core.Results;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarios;
        private readonly IConfiguration _config;

        public AuthService(IUsuarioRepository usuarios, IConfiguration config)
        {
            _usuarios = usuarios;
            _config = config;
        }

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            var usuario = await _usuarios.GetByNameAsync(email)
                ?? throw new UnauthorizedAccessException("Credenciales incorrectas.");

            if (!usuario.Activo)
                throw new UnauthorizedAccessException("Usuario inactivo.");

            if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales incorrectas.");

            var jwt = _config.GetSection("JwtSettings");
            var secretKey = jwt["SecretKey"]!;
            var issuer    = jwt["Issuer"]!;
            var audience  = jwt["Audience"]!;
            var hours     = int.Parse(jwt["ExpirationHours"] ?? "24");

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(hours);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimTypes.Role,               usuario.Rol.ToString()),
                new Claim("nombreCompleto",              usuario.NombreCompleto),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new LoginResult(
                Token:          tokenString,
                UsuarioId:      usuario.Id,
                NombreCompleto: usuario.NombreCompleto,
                Email:          usuario.Email,
                Rol:            usuario.Rol.ToString(),
                ExpiresAt:      expires
            );
        }

        public Task LogoutAsync(string token)
        {
            // JWT es stateless; el cliente descarta el token.
            return Task.CompletedTask;
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            var jwt = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));

            var handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = true,
                    ValidIssuer              = jwt["Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = jwt["Audience"],
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                }, out _);

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
