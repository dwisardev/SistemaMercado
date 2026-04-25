using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Results;
using SGM.Infrastructure.Data;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SGM.Infrastructure.Services
{
    public class PagoService : IPagoService
    {
        private readonly IPagoRepository _pagos;
        private readonly AppDbContext _db;

        public PagoService(IPagoRepository pagos, AppDbContext db)
        {
            _pagos = pagos;
            _db = db;
        }

        public async Task<CajaDiariaResult> GetCajaDiariaAsync(DateOnly fecha)
        {
            var pagos = await _pagos.GetByFechaAsync(fecha);
            var activos = pagos.Where(p => p.Estado == EstadoPago.Activo).ToList();
            return new CajaDiariaResult(
                Fecha: fecha,
                TotalCobrado: activos.Sum(p => p.MontoPagado),
                TotalOperaciones: activos.Count,
                TotalEfectivo: activos.Where(p => p.Metodo == MetodoPago.Efectivo).Sum(p => p.MontoPagado),
                TotalTransferencia: activos.Where(p => p.Metodo == MetodoPago.Transferencia).Sum(p => p.MontoPagado),
                TotalOtro: activos.Where(p => p.Metodo == MetodoPago.Otro).Sum(p => p.MontoPagado)
            );
        }

        public async Task<IEnumerable<DeudaPendienteResult>> GetDeudaPendienteResultsAsync()
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            return await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.Estado == EstadoDeuda.Pendiente)
                .Select(d => new DeudaPendienteResult(
                    d.Id,
                    d.Puesto!.Codigo,
                    d.Puesto.Dueno != null ? d.Puesto.Dueno.NombreCompleto : null,
                    d.Concepto.Nombre,
                    d.Monto,
                    d.Periodo,
                    d.FechaEmision,
                    d.FechaVencimiento,
                    d.FechaVencimiento.HasValue ? (int)(hoy.DayNumber - d.FechaVencimiento.Value.DayNumber) : 0
                )).ToListAsync();
        }

        public async Task<IEnumerable<MorosidadResult>> GetMorosidadsAsync()
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            return await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Where(d => d.Estado == EstadoDeuda.Pendiente && d.FechaVencimiento < hoy)
                .GroupBy(d => new { d.PuestoId, d.Puesto!.Codigo, Dueno = d.Puesto.Dueno })
                .Select(g => new MorosidadResult(
                    g.Key.Codigo,
                    g.Key.Dueno != null ? g.Key.Dueno.NombreCompleto : null,
                    g.Key.Dueno != null ? g.Key.Dueno.Telefono : null,
                    g.Count(),
                    g.Sum(d => d.Monto),
                    g.Min(d => d.FechaEmision),
                    g.Max(d => d.FechaVencimiento.HasValue ? (hoy.DayNumber - d.FechaVencimiento.Value.DayNumber) : 0)
                )).ToListAsync();
        }

        public async Task<IEnumerable<Pago>> GetHistorialPagosByDuenoAsync(Guid duenoId) =>
            await _db.Pagos.AsNoTracking()
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto)
                .Where(p => p.Deuda.Puesto!.DuenoId == duenoId)
                .OrderByDescending(p => p.FechaPago).ToListAsync();
    }
}
