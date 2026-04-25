using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SGM.Core.Entities;
using SMG.Core.Enums;
using SMG.Core.Interfaces.Services;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize(Roles = "Admin")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _service;
        public UsuariosController(IUsuarioService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll()
        {
            var usuarios = await _service.GetAllAsync();
            return Ok(usuarios.Select(ToDto));
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
