using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Interfaces.Repositories
{
    public interface IHistorialDuenoRepository
    {
        Task<IEnumerable<HistorialDueno>> GetByPuestoAsync(Guid puestoId);
        Task<IEnumerable<HistorialDueno>> GetByDuenoAsync(Guid duenoId);
        Task <HistorialDueno?> GetActualByPuestoAsyncGuid(Guid puestoId);
        Task AddAsync(HistorialDueno historial);
    }
}
