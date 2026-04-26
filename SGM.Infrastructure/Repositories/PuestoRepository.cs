using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Repositories;
using SGM.Infrastructure.Data;

namespace SGM.Infrastructure.Repositories
{
    public class PuestoRepository : IPuestoRepository
    {
        private readonly AppDbContext _db;
        public PuestoRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<Puesto>> GetAllAsync() =>
            await _db.Puestos.AsNoTracking().Include(p => p.Dueno).ToListAsync();

        public async Task<Puesto?> GetByIdAsync(Guid id) =>
            await _db.Puestos.Include(p => p.Dueno).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Puesto?> GetByCodigoAsync(string codigo) =>
            await _db.Puestos.Include(p => p.Dueno).FirstOrDefaultAsync(p => p.Codigo == codigo);

        public async Task<IEnumerable<Puesto>> GetByDuenoAsync(Guid duenoId) =>
            await _db.Puestos.AsNoTracking().Include(p => p.Dueno)
                .Where(p => p.DuenoId == duenoId).ToListAsync();

        public async Task<IEnumerable<Puesto>> GetActivosAsync() =>
            await _db.Puestos.AsNoTracking().Include(p => p.Dueno)
                .Where(p => p.Estado == EstadoPuesto.Activo).ToListAsync();

        public async Task<Puesto> AddAsync(Puesto puesto)
        {
            _db.Puestos.Add(puesto);
            await _db.SaveChangesAsync();
            return puesto;
        }

        public async Task UpdateAsync(Puesto puesto)
        {
            _db.Puestos.Update(puesto);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId = null) =>
            await _db.Puestos.AnyAsync(p => p.Codigo == codigo && p.Id != excluirId);

        public async Task<(IEnumerable<Puesto> Data, int Total)> GetPaginadoAsync(string? search, int page, int pageSize)
        {
            var query = _db.Puestos.AsNoTracking().Include(p => p.Dueno).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Codigo.Contains(search) ||
                    (p.Ubicacion != null && p.Ubicacion.Contains(search)) ||
                    (p.Dueno != null && p.Dueno.NombreCompleto.Contains(search)));

            var total = await query.CountAsync();
            var data  = await query
                .OrderBy(p => p.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }
    }
}
