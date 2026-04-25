using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SGM.Core.Entities;
using SMG.Core.Interfaces.Services;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/conceptos")]
    [Authorize]
    public class ConceptosCobroController : ControllerBase
    {
        private readonly IConceptoCobroService _service;
        public ConceptosCobroController(IConceptoCobroService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConceptoCobroResponseDto>>> GetAll()
        {
            var conceptos = await _service.GetAllAsync();
            return Ok(conceptos.Select(ToDto));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ConceptoCobroResponseDto>> Create([FromBody] CreateConceptoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(new { message = "El nombre es obligatorio." });
            if (dto.Monto <= 0)
                return BadRequest(new { message = "El monto debe ser mayor a 0." });

            var concepto = new ConceptoCobro
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                MontoDefault = dto.Monto,
                DiaEmision = dto.DiaEmision ?? 1,
                Activo = true,
            };
            var created = await _service.CreateAsync(concepto);
            return CreatedAtAction(nameof(GetAll), ToDto(created));
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ConceptoCobroResponseDto>> Update(Guid id, [FromBody] UpdateConceptoDto dto)
        {
            var datos = new ConceptoCobro
            {
                Nombre = dto.Nombre ?? string.Empty,
                Descripcion = dto.Descripcion,
                MontoDefault = dto.Monto ?? 0,
                DiaEmision = dto.DiaEmision ?? 0,
                Activo = dto.Activo ?? true,
            };
            var updated = await _service.UpdateAsync(id, datos);
            return Ok(ToDto(updated));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Soft delete: marcar como inactivo
            var datos = new ConceptoCobro { Activo = false };
            await _service.UpdateAsync(id, datos);
            return NoContent();
        }

        private static ConceptoCobroResponseDto ToDto(ConceptoCobro c) => new()
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            Monto = c.MontoDefault,
            Activo = c.Activo,
        };
    }
}
