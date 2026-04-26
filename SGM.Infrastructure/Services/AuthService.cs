using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SGM.Core.Entities;
using SGM.Core.Results;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarios;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IConfiguration _config;
        private readonly ITokenBlacklist _blacklist;

        public AuthService(
            IUsuarioRepository usuarios,
            IRefreshTokenRepository refreshTokens,
            IConfiguration config,
            ITokenBlacklist blacklist)
        {
            _usuarios      = usuarios;
            _refreshTokens = refreshTokens;
            _config        = config;
            _blacklist     = blacklist;
        }

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            var usuario = await _usuarios.GetByNameAsync(email)
                ?? throw new UnauthorizedAccessException("Credenciales incorrectas.");

            if (!usuario.Activo)
                throw new UnauthorizedAccessException("Usuario inactivo.");

            if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales incorrectas.");

            var (tokenString, expires) = GenerarJwt(usuario);

            var refreshDays   = int.Parse(_config["JwtSettings:RefreshTokenDays"] ?? "7");
            var refreshExpiry = DateTime.UtcNow.AddDays(refreshDays);
            var refreshToken  = GenerarRefreshToken();

            await _refreshTokens.AddAsync(new RefreshToken
            {
                UsuarioId = usuario.Id,
                Token     = refreshToken,
                ExpiresAt = refreshExpiry,
            });

            return new LoginResult(
                Token:                  tokenString,
                UsuarioId:              usuario.Id,
                NombreCompleto:         usuario.NombreCompleto,
                Email:                  usuario.Email,
                Rol:                    usuario.Rol.ToString(),
                ExpiresAt:              expires,
                RefreshToken:           refreshToken,
                RefreshTokenExpiresAt:  refreshExpiry
            );
        }

        public async Task<LoginResult> RefreshAsync(string refreshToken)
        {
            var stored = await _refreshTokens.GetByTokenAsync(refreshToken)
                ?? throw new UnauthorizedAccessException("Refresh token inválido.");

            if (stored.Revocado)
                throw new UnauthorizedAccessException("Refresh token revocado.");

            if (stored.ExpiresAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token expirado.");

            var usuario = stored.Usuario
                ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

            if (!usuario.Activo)
                throw new UnauthorizedAccessException("Usuario inactivo.");

            // Rotate: revoke old, issue new
            stored.Revocado = true;
            var (tokenString, expires) = GenerarJwt(usuario);

            var refreshDays   = int.Parse(_config["JwtSettings:RefreshTokenDays"] ?? "7");
            var refreshExpiry = DateTime.UtcNow.AddDays(refreshDays);
            var nuevoRefresh  = GenerarRefreshToken();

            await _refreshTokens.AddAsync(new RefreshToken
            {
                UsuarioId = usuario.Id,
                Token     = nuevoRefresh,
                ExpiresAt = refreshExpiry,
            });

            return new LoginResult(
                Token:                  tokenString,
                UsuarioId:              usuario.Id,
                NombreCompleto:         usuario.NombreCompleto,
                Email:                  usuario.Email,
                Rol:                    usuario.Rol.ToString(),
                ExpiresAt:              expires,
                RefreshToken:           nuevoRefresh,
                RefreshTokenExpiresAt:  refreshExpiry
            );
        }

        public async Task LogoutAsync(string accessToken, string? refreshToken = null)
        {
            // Blacklist the access token JTI
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken);
                if (!string.IsNullOrEmpty(jwt.Id))
                    await _blacklist.RevokeAsync(jwt.Id, jwt.ValidTo);
            }
            catch { }

            // Revoke all refresh tokens for this user (if we can find them)
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var stored = await _refreshTokens.GetByTokenAsync(refreshToken);
                if (stored is not null)
                    await _refreshTokens.RevokeByUsuarioIdAsync(stored.UsuarioId);
            }
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

        private (string token, DateTime expires) GenerarJwt(Usuario usuario)
        {
            var jwt     = _config.GetSection("JwtSettings");
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var hours   = int.Parse(jwt["ExpirationHours"] ?? "2");
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
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        private static string GenerarRefreshToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
