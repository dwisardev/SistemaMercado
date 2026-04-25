using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Repositories
{
    public interface IConceptoCobroRepository
    {
        Task<IEnumerable<ConceptoCobro>> GetAllAsync();
        Task<IEnumerable<ConceptoCobro>>GetActivosAsync();
        Task<ConceptoCobro> GetByIdAsync(Guid id);
        Task<ConceptoCobro?> AddAsync(ConceptoCobro concepto);
        Task UpdateAsync(ConceptoCobro concepto);
        Task<bool> ExisteNombreAsync(string nombre, Guid? idExcluido = null);
         
    }
}
