using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Response;
using SGM.Core.Enums;
using SGM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize(Roles = "Admin")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReportesController(AppDbContext db) => _db = db;

        [HttpGet("caja-diaria")]
        public async Task<ActionResult<CajaDiariaDto>> CajaDiaria([FromQuery] string? fecha)
        {
            var dia = fecha is not null && DateOnly.TryParse(fecha, out var f) ? f : DateOnly.FromDateTime(DateTime.UtcNow);

            var pagos = await _db.Pagos.AsNoTracking()
                .Include(p => p.Cajero)
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Where(p => DateOnly.FromDateTime(p.FechaPago.ToUniversalTime()) == dia)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            var activos = pagos.Where(p => p.Estado == EstadoPago.Activo).ToList();

            return Ok(new CajaDiariaDto
            {
                Fecha = dia.ToString("yyyy-MM-dd"),
                TotalRecaudado = activos.Sum(p => p.MontoPagado),
                CantidadPagos = activos.Count,
                Pagos = pagos.Select(PagosController_ToDto).ToList(),
            });
        }

        [HttpGet("morosidad")]
        public async Task<ActionResult<IEnumerable<MorosidadDto>>> Morosidad()
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var deudas = await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.Estado == EstadoDeuda.Pendiente && d.FechaVencimiento < hoy && d.Puesto!.DuenoId != null)
                .ToListAsync();

            var grouped = deudas
                .GroupBy(d => d.PuestoId)
                .Select(g => new MorosidadDto
                {
                    PuestoId = g.Key,
                    PuestoNumero = g.First().Puesto?.Codigo ?? "",
                    DuenoNombre = g.First().Puesto?.Dueno?.NombreCompleto ?? "",
                    Deudas = g.Select(DeudasController_ToDto).ToList(),
                    TotalPendiente = g.Sum(d => d.Monto),
                });

            return Ok(grouped);
        }

        [HttpGet("deudas-pendientes")]
        public async Task<ActionResult<IEnumerable<DeudaPendienteDto>>> DeudasPendientes()
        {
            var deudas = await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.Estado == EstadoDeuda.Pendiente && d.Puesto!.DuenoId != null)
                .ToListAsync();

            var grouped = deudas
                .GroupBy(d => d.PuestoId)
                .Select(g => new DeudaPendienteDto
                {
                    PuestoId = g.Key,
                    PuestoNumero = g.First().Puesto?.Codigo ?? "",
                    DuenoNombre = g.First().Puesto?.Dueno?.NombreCompleto ?? "",
                    TotalPendiente = g.Sum(d => d.Monto),
                    Deudas = g.Select(DeudasController_ToDto).ToList(),
                });

            return Ok(grouped);
        }

        private static PagoResponseDto PagosController_ToDto(SGM.Core.Entities.Pago p) => new()
        {
            Id = p.Id,
            DeudaId = p.DeudaId,
            PuestoNumero = p.Deuda?.Puesto?.Codigo,
            DuenoNombre = p.Deuda?.Puesto?.Dueno?.NombreCompleto,
            MontoPagado = p.MontoPagado,
            FechaPago = p.FechaPago,
            CajeroNombre = p.Cajero?.NombreCompleto,
            NumeroComprobante = p.NumeroComprobante,
            Metodo = p.Metodo.ToString(),
            Estado = p.Estado.ToString(),
            ReferenciaPago = p.ReferenciaPago,
            MotivoAnulacion = p.MotivoAnulacion,
        };

        private static DeudaResponseDto DeudasController_ToDto(SGM.Core.Entities.Deuda d)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var estaVencida = d.Estado == EstadoDeuda.Pendiente && d.FechaVencimiento.HasValue && d.FechaVencimiento.Value < hoy;
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
