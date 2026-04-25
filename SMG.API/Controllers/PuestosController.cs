using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Interfaces.Services;
using System.Security.Claims;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/puestos")]
    [Authorize]
    public class PuestosController : ControllerBase
    {
        private readonly IPuestoService _service;
        public PuestosController(IPuestoService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PuestoResponseDto>>> GetAll()
        {
            var puestos = await _service.getAllAsync();
            return Ok(puestos.Select(ToDto));
        }

        [HttpGet("mis-puestos")]
        public async Task<ActionResult<IEnumerable<PuestoResponseDto>>> GetMisPuestos()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var puestos = await _service.GetByDuenoAsync(userId);
            return Ok(puestos.Select(ToDto));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PuestoResponseDto>> Create([FromBody] CreatePuestoDto dto)
        {
            var puesto = new Puesto
            {
                Codigo = dto.NumeroPuesto,
                Descripcion = dto.Descripcion,
                Ubicacion = dto.Sector,
                Estado = EstadoPuesto.Activo,
            };
            var created = await _service.CreateAsync(puesto);
            return CreatedAtAction(nameof(GetAll), ToDto(created));
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PuestoResponseDto>> Update(Guid id, [FromBody] UpdatePuestoDto dto)
        {
            var puesto = await _service.GetByIdAsync(id);

            if (dto.Descripcion is not null) puesto.Descripcion = dto.Descripcion;
            if (dto.Sector is not null) puesto.Ubicacion = dto.Sector;
            if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                puesto.Estado = dto.Estado switch
                {
                    "Mantenimiento" => EstadoPuesto.EnMantenimiento,
                    _ => puesto.DuenoId.HasValue ? EstadoPuesto.Activo : EstadoPuesto.Activo,
                };
            }

            await _service.UpdatePuestoAsync(id, puesto);
            var updated = await _service.GetByIdAsync(id);
            return Ok(ToDto(updated));
        }

        [HttpPatch("{id:guid}/asignar-dueno")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AsignarDueno(Guid id, [FromBody] AsignarDuenoDto dto)
        {
            await _service.AsignarDuenoAsync(id, new Puesto { DuenoId = dto.DuenoId });
            var puesto = await _service.GetByIdAsync(id);
            return Ok(ToDto(puesto));
        }

        [HttpPatch("{id:guid}/liberar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Liberar(Guid id)
        {
            await _service.LiberarPuestoAsync(id);
            var puesto = await _service.GetByIdAsync(id);
            return Ok(ToDto(puesto));
        }

        private static PuestoResponseDto ToDto(Puesto p) => new()
        {
            Id = p.Id,
            NumeroPuesto = p.Codigo,
            Descripcion = p.Descripcion,
            Estado = p.Estado == EstadoPuesto.EnMantenimiento ? "Mantenimiento"
                   : p.DuenoId.HasValue ? "Ocupado" : "Disponible",
            DuenoId = p.DuenoId,
            DuenoNombre = p.Dueno?.NombreCompleto,
            DuenoEmail = p.Dueno?.Email,
            Sector = p.Ubicacion,
            CreatedAt = p.CreateAt,
        };
    }
}
