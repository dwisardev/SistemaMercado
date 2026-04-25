using SGM.Core.Entities;
using SGM.Infrastructure.Repositories;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class ConceptoCobroService : IConceptoCobroService
    {
        private readonly IConceptoCobroRepository _repo;
        public ConceptoCobroService(IConceptoCobroRepository repo) => _repo = repo;

        public async Task<IEnumerable<ConceptoCobro>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<ConceptoCobro> GetActivosAsync() =>
            (await _repo.GetActivosAsync()).FirstOrDefault() ?? throw new InvalidOperationException("No hay conceptos activos.");

        public async Task<ConceptoCobro> CreateAsync(ConceptoCobro concepto) =>
            await _repo.AddAsync(concepto) ?? throw new InvalidOperationException("No se pudo crear el concepto.");

        public async Task<ConceptoCobro> UpdateAsync(Guid id, ConceptoCobro datos)
        {
            var concepto = await _repo.GetByIdAsync(id);
            if (!string.IsNullOrWhiteSpace(datos.Nombre)) concepto.Nombre = datos.Nombre;
            if (datos.Descripcion is not null) concepto.Descripcion = datos.Descripcion;
            if (datos.MontoDefault > 0) concepto.MontoDefault = datos.MontoDefault;
            if (datos.DiaEmision > 0) concepto.DiaEmision = datos.DiaEmision;
            concepto.Activo = datos.Activo;
            await _repo.UpdateAsync(concepto);
            return concepto;
        }
    }
}
