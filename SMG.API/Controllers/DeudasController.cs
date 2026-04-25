using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Repositories;
using SMG.Core.Repositories;
using System.Security.Claims;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/deudas")]
    [Authorize]
    public class DeudasController : ControllerBase
    {
        private readonly IDeudaRepository _deudas;
        private readonly IPuestoRepository _puestos;
        private readonly IConceptoCobroRepository _conceptos;

        public DeudasController(IDeudaRepository deudas, IPuestoRepository puestos, IConceptoCobroRepository conceptos)
        {
            _deudas = deudas;
            _puestos = puestos;
            _conceptos = conceptos;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeudaResponseDto>>> GetDeudas(
            [FromQuery] Guid? puestoId,
            [FromQuery] string? estado,
            [FromQuery] string? periodo)
        {
            IEnumerable<Deuda> deudas;
            if (!puestoId.HasValue && string.IsNullOrEmpty(estado) && string.IsNullOrEmpty(periodo))
                return BadRequest(new { message = "Debe especificar al menos un filtro (puestoId, estado o periodo)." });

            deudas = await _deudas.GetFiltradosAsync(puestoId, estado, periodo);
            return Ok(deudas.Select(ToDto));
        }

        [HttpPost("individual")]
        [Authorize(Roles = "Admin,Cajero")]
        public async Task<ActionResult<DeudaResponseDto>> CargaIndividual([FromBody] CargaIndividualDeudaDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var fechaVenc = DateOnly.TryParse(dto.FechaVencimiento, out var fv) ? (DateOnly?)fv : null;

            var deuda = new Deuda
            {
                PuestoId = dto.PuestoId,
                ConceptoId = dto.ConceptoId,
                Monto = dto.Monto,
                Periodo = dto.Periodo,
                FechaEmision = DateOnly.FromDateTime(DateTime.UtcNow),
                FechaVencimiento = fechaVenc,
                Estado = EstadoDeuda.Pendiente,
                GeneradoPor = userId,
            };

            var created = await _deudas.AddAsync(deuda)
                ?? throw new InvalidOperationException("No se pudo crear la deuda.");

            var full = await _deudas.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetDeudas), new { puestoId = dto.PuestoId }, ToDto(full!));
        }

        [HttpPost("masiva")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CargaMasivaResultDto>> CargaMasiva([FromBody] CargaMasivaDeudaDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var puestos = await _puestos.GetActivosAsync();
            var puestosConDueno = puestos.Where(p => p.DuenoId.HasValue).ToList();
            var fechaVenc = DateOnly.TryParse(dto.FechaVencimiento, out var fv) ? (DateOnly?)fv : null;
            var loteId = Guid.NewGuid();
            var errores = new List<string>();
            var exitosos = 0;

            foreach (var puesto in puestosConDueno)
            {
                var yaExiste = await _deudas.ExisteDuplicadoAsync(puesto.Id, dto.Monto, dto.Periodo);
                if (yaExiste)
                {
                    errores.Add($"Puesto {puesto.Codigo}: ya existe deuda para el período {dto.Periodo}.");
                    continue;
                }
                try
                {
                    var deuda = new Deuda
                    {
                        PuestoId = puesto.Id,
                        ConceptoId = dto.ConceptoId,
                        Monto = dto.Monto,
                        Periodo = dto.Periodo,
                        FechaEmision = DateOnly.FromDateTime(DateTime.UtcNow),
                        FechaVencimiento = fechaVenc,
                        Estado = EstadoDeuda.Pendiente,
                        LoteCargaId = loteId,
                        GeneradoPor = userId,
                    };
                    await _deudas.AddAsync(deuda);
                    exitosos++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Puesto {puesto.Codigo}: {ex.Message}");
                }
            }

            return Ok(new CargaMasivaResultDto
            {
                Total = puestosConDueno.Count,
                Exitosos = exitosos,
                Fallidos = errores.Count,
                Errores = errores,
            });
        }

        private static DeudaResponseDto ToDto(Deuda d)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var estaVencida = d.Estado == EstadoDeuda.Pendiente
                && d.FechaVencimiento.HasValue
                && d.FechaVencimiento.Value < hoy;

            return new DeudaResponseDto
            {
                Id = d.Id,
                PuestoId = d.PuestoId,
                PuestoNumero = d.Puesto?.Codigo,
                DuenoNombre = d.Puesto?.Dueno?.NombreCompleto,
                ConceptoId = d.ConceptoId,
                ConceptoNombre = d.Concepto?.Nombre,
                Monto = d.Monto,
                SaldoPendiente = d.Estado == EstadoDeuda.Pendiente ? d.Monto : 0,
                Estado = estaVencida ? "Vencida" : d.Estado.ToString(),
                FechaVencimiento = d.FechaVencimiento?.ToString("yyyy-MM-dd"),
                Periodo = d.Periodo,
                CreatedAt = d.CreatedAt,
            };
        }
    }
}
