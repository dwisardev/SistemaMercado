using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class DeudaRepository : IDeudaRepository
    {
        private readonly AppDbContext _db;
        public DeudaRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<Deuda>> GetAllAsync(Guid puestoId) =>
            await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.PuestoId == puestoId)
                .OrderByDescending(d => d.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Deuda>> GetPendienteByPuestoAsync(Guid puestoId) =>
            await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.PuestoId == puestoId && d.Estado == EstadoDeuda.Pendiente)
                .ToListAsync();

        public async Task<IEnumerable<Deuda>> GetPendienteAsync() =>
            await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.Estado == EstadoDeuda.Pendiente)
                .OrderBy(d => d.FechaVencimiento).ToListAsync();

        public async Task<IEnumerable<Deuda>> GetVencidadAsync()
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            return await _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .Where(d => d.Estado == EstadoDeuda.Pendiente && d.FechaVencimiento < hoy)
                .ToListAsync();
        }

        public async Task<Deuda?> GetByIdAsync(Guid id) =>
            await _db.Deudas
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<Deuda?> AddAsync(Deuda deuda)
        {
            _db.Deudas.Add(deuda);
            await _db.SaveChangesAsync();
            return deuda;
        }

        public async Task AddRangeAsync(IEnumerable<Deuda> deudas)
        {
            _db.Deudas.AddRange(deudas);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatedAsync(Deuda deuda)
        {
            _db.Deudas.Update(deuda);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Deuda>> GetFiltradosAsync(Guid? puestoId, string? estado, string? periodo)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var q = _db.Deudas.AsNoTracking()
                .Include(d => d.Puesto).ThenInclude(p => p!.Dueno)
                .Include(d => d.Concepto)
                .AsQueryable();

            if (puestoId.HasValue)
                q = q.Where(d => d.PuestoId == puestoId.Value);

            if (!string.IsNullOrEmpty(periodo))
                q = q.Where(d => d.Periodo.Contains(periodo));

            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "Vencida")
                    q = q.Where(d => d.Estado == EstadoDeuda.Pendiente && d.FechaVencimiento < hoy);
                else if (Enum.TryParse<EstadoDeuda>(estado, true, out var est))
                    q = q.Where(d => d.Estado == est);
            }

            return await q.OrderByDescending(d => d.CreatedAt).ToListAsync();
        }

        public async Task<bool> ExisteDuplicadoAsync(Guid puestoId, decimal monto, string periodo) =>
            await _db.Deudas.AnyAsync(d => d.PuestoId == puestoId && d.Periodo == periodo);
    }
}
