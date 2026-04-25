using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email y contraseña son obligatorios." });

            try
            {
                var result = await _auth.LoginAsync(dto.Email, dto.Password);

                return Ok(new LoginResponseDto
                {
                    Token          = result.Token,
                    UsuarioId      = result.UsuarioId,
                    NombreCompleto = result.NombreCompleto,
                    Email          = result.Email,
                    Rol            = result.Rol,
                    ExpiresAt      = result.ExpiresAt,
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromHeader(Name = "Authorization")] string? authorization)
        {
            var token = authorization?.Replace("Bearer ", "");
            if (!string.IsNullOrEmpty(token))
                await _auth.LogoutAsync(token);

            return NoContent();
        }
    }
}
