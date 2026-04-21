using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Interfaces.Repositories
{
    public interface ITarifaPuestoRepository
    {
        Task<TarifaPuesto?> GetByPuestoAsync(Guid puestoId, Guid conceptoId);
        Task<IEnumerable<TarifaPuesto>> GetByPuestoAsync(Guid puestoId);
        Task <IEnumerable<TarifaPuesto>> GetByConceptoAsync(Guid conceptoId);
        Task UpdateAsync(TarifaPuesto tarifa);

    }
}
