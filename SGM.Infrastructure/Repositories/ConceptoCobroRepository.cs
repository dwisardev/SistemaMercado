using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class ConceptoCobroRepository : IConceptoCobroRepository
    {
        private readonly AppDbContext _db;
        public ConceptoCobroRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<ConceptoCobro>> GetAllAsync() =>
            await _db.ConceptoCobro.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();

        public async Task<IEnumerable<ConceptoCobro>> GetActivosAsync() =>
            await _db.ConceptoCobro.AsNoTracking().Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();

        public async Task<ConceptoCobro> GetByIdAsync(Guid id) =>
            await _db.ConceptoCobro.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException($"Concepto {id} no encontrado.");

        public async Task<ConceptoCobro?> AddAsync(ConceptoCobro concepto)
        {
            _db.ConceptoCobro.Add(concepto);
            await _db.SaveChangesAsync();
            return concepto;
        }

        public async Task UpdateAsync(ConceptoCobro concepto)
        {
            _db.ConceptoCobro.Update(concepto);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombre, Guid? idExcluido = null) =>
            await _db.ConceptoCobro.AnyAsync(c => c.Nombre == nombre && c.Id != idExcluido);
    }
}
