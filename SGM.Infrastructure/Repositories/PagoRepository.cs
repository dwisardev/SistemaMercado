using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class PagoRepository : IPagoRepository
    {
        private readonly AppDbContext _db;
        public PagoRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<Pago>> GetByFechaAsync(DateOnly fecha) =>
            await _db.Pagos.AsNoTracking()
                .Include(p => p.Cajero)
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Where(p => DateOnly.FromDateTime(p.FechaPago.ToUniversalTime()) == fecha)
                .OrderByDescending(p => p.FechaPago).ToListAsync();

        public async Task<IEnumerable<Pago>> GetByCajeroAsync(Guid cajeroId, DateOnly fecha) =>
            await _db.Pagos.AsNoTracking()
                .Include(p => p.Cajero)
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto)
                .Where(p => p.CajeroId == cajeroId && DateOnly.FromDateTime(p.FechaPago.ToUniversalTime()) == fecha)
                .ToListAsync();

        public async Task<IEnumerable<Pago>> GetByPuestoAsync(Guid puestoId) =>
            await _db.Pagos.AsNoTracking()
                .Include(p => p.Cajero)
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Where(p => p.Deuda.PuestoId == puestoId)
                .OrderByDescending(p => p.FechaPago).ToListAsync();

        public async Task<Pago?> GetByIdAsync(Guid id) =>
            await _db.Pagos
                .Include(p => p.Cajero)
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(p => p.Deuda).ThenInclude(d => d.Concepto)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Pago?> GetByDeudaIdAsync(Guid deudaId) =>
            await _db.Pagos.Include(p => p.Cajero).FirstOrDefaultAsync(p => p.DeudaId == deudaId);

        public async Task<Pago> AddAsync(Pago pago)
        {
            _db.Pagos.Add(pago);
            await _db.SaveChangesAsync();
            return pago;
        }

        public async Task UpdateAsync(Pago pago)
        {
            _db.Pagos.Update(pago);
            await _db.SaveChangesAsync();
        }

        public async Task<string?> GetUltimoNroComprobanteAsync() =>
            await _db.Pagos.OrderByDescending(p => p.CreatedAt)
                .Select(p => p.NumeroComprobante).FirstOrDefaultAsync();

        public async Task<IEnumerable<Pago>> GetFiltradosAsync(DateOnly desde, DateOnly hasta, Guid? puestoId, string? estado) =>
            await BuildFiltroQuery(desde, hasta, puestoId, estado)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

        public async Task<(IEnumerable<Pago> Data, int Total)> GetFiltradosPaginadoAsync(
            DateOnly desde, DateOnly hasta, Guid? puestoId, string? estado, int page, int pageSize)
        {
            var q = BuildFiltroQuery(desde, hasta, puestoId, estado);
            var total = await q.CountAsync();
            var data = await q
                .OrderByDescending(p => p.FechaPago)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, total);
        }

        private IQueryable<Pago> BuildFiltroQuery(DateOnly desde, DateOnly hasta, Guid? puestoId, string? estado)
        {
            var q = _db.Pagos.AsNoTracking()
                .Include(p => p.Cajero)
                .Include(p => p.Deuda).ThenInclude(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .AsQueryable();

            if (desde != DateOnly.MinValue)
                q = q.Where(p => DateOnly.FromDateTime(p.FechaPago.ToUniversalTime()) >= desde);
            q = q.Where(p => DateOnly.FromDateTime(p.FechaPago.ToUniversalTime()) <= hasta);

            if (puestoId.HasValue)
                q = q.Where(p => p.Deuda.PuestoId == puestoId.Value);

            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<SGM.Core.Enums.EstadoPago>(estado, true, out var est))
                q = q.Where(p => p.Estado == est);

            return q;
        }
    }
}
