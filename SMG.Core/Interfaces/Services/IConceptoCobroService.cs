using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Interfaces.Services
{
    public interface IConceptoCobroService
    {
        Task<IEnumerable<ConceptoCobro>> GetAllAsync();
        Task<ConceptoCobro> GetActivosAsync();
        Task<ConceptoCobro> CreateAsync(ConceptoCobro concepto);
        Task<ConceptoCobro> UpdateAsync(Guid id, ConceptoCobro datos);
    }
}
