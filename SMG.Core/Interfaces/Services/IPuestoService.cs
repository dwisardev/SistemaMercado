using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Interfaces.Services
{
    public interface IPuestoService
    {
        Task<IEnumerable<Puesto>> getAllAsync();
        Task<Puesto> GetByIdAsync(Guid id);
        Task<Puesto> CreateAsync (Puesto puesto);
        Task AsignarDuenoAsync(Guid id, Puesto datosActualizados);
        Task LiberarPuestoAsync(Guid puestoiD);
        Task <IEnumerable<Puesto>> GetByDuenoAsync(Guid duenoId);

    }
}
