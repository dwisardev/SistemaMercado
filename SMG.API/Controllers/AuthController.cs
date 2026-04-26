using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SMG.Core.Interfaces.Services;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email y contraseña son obligatorios." });

            try
            {
                var result = await _auth.LoginAsync(dto.Email, dto.Password);
                return Ok(ToDto(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { message = "RefreshToken es obligatorio." });

            try
            {
                var result = await _auth.RefreshAsync(dto.RefreshToken);
                return Ok(ToDto(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto? dto,
            [FromHeader(Name = "Authorization")] string? authorization)
        {
            var accessToken = authorization?.Replace("Bearer ", "") ?? string.Empty;
            await _auth.LogoutAsync(accessToken, dto?.RefreshToken);
            return NoContent();
        }

        private static LoginResponseDto ToDto(SGM.Core.Results.LoginResult r) => new()
        {
            Token                 = r.Token,
            UsuarioId             = r.UsuarioId,
            NombreCompleto        = r.NombreCompleto,
            Email                 = r.Email,
            Rol                   = r.Rol,
            ExpiresAt             = r.ExpiresAt,
            RefreshToken          = r.RefreshToken,
            RefreshTokenExpiresAt = r.RefreshTokenExpiresAt,
        };
    }
}
