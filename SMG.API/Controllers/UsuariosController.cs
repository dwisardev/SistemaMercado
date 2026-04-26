using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SGM.Core.Entities;
using SMG.Core.Enums;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;
using System.Security.Claims;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize(Roles = "Admin")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService    _service;
        private readonly IUsuarioRepository _repo;
        public UsuariosController(IUsuarioService service, IUsuarioRepository repo)
        {
            _service = service;
            _repo    = repo;
        }

        [HttpGet]
        public async Task<ActionResult<PaginadoDto<UsuarioResponseDto>>> GetAll(
            [FromQuery] string? search   = null,
            [FromQuery] string? rol      = null,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 25)
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            page     = Math.Max(page, 1);

            var (data, total) = await _repo.GetPaginadoAsync(search, rol, page, pageSize);
            return Ok(new PaginadoDto<UsuarioResponseDto>
            {
                Data     = data.Select(ToDto),
                Total    = total,
                Page     = page,
                PageSize = pageSize,
            });
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] CreateUsuarioDto dto)
        {
            if (!Enum.TryParse<RolUsuario>(dto.Rol, true, out var rol))
                return BadRequest(new { message = $"Rol inválido: {dto.Rol}" });

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto,
                Email = dto.Email,
                PasswordHash = dto.Password,
                Rol = rol,
                Activo = true,
                Dni = string.Empty,
            };
            var created = await _service.CreateAsync(usuario);
            return CreatedAtAction(nameof(GetAll), ToDto(created));
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<UsuarioResponseDto>> Update(Guid id, [FromBody] UpdateUsuarioDto dto)
        {
            var usuario = await _service.GetByIdAsync(id);
            if (usuario is null) return NotFound(new { message = "Usuario no encontrado." });

            if (!string.IsNullOrWhiteSpace(dto.NombreCompleto))
                usuario.NombreCompleto = dto.NombreCompleto;

            if (!string.IsNullOrWhiteSpace(dto.Rol))
            {
                if (!Enum.TryParse<RolUsuario>(dto.Rol, true, out var rol))
                    return BadRequest(new { message = $"Rol inválido: {dto.Rol}" });
                usuario.Rol = rol;
            }

            if (dto.Activo.HasValue)
                usuario.Activo = dto.Activo.Value;

            if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
                usuario.PasswordHash = BCryptNet.HashPassword(dto.NuevaPassword);

            await _service.UpdateAsync(id, usuario);
            return Ok(ToDto(usuario));
        }

        [HttpPatch("me/password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idClaim, out var userId))
                return Unauthorized();

            var usuario = await _service.GetByIdAsync(userId);
            if (usuario is null) return NotFound(new { message = "Usuario no encontrado." });

            if (!BCryptNet.Verify(dto.PasswordActual, usuario.PasswordHash))
                return BadRequest(new { message = "La contraseña actual es incorrecta." });

            usuario.PasswordHash = BCryptNet.HashPassword(dto.NuevaPassword);
            await _service.UpdateAsync(userId, usuario);
            return NoContent();
        }

        private static UsuarioResponseDto ToDto(Usuario u) => new()
        {
            Id = u.Id,
            NombreCompleto = u.NombreCompleto,
            Email = u.Email,
            Rol = u.Rol.ToString(),
            Activo = u.Activo,
            CreatedAt = u.CreatedAt,
        };
    }
}
