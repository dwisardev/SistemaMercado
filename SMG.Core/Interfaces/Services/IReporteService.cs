using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Interfaces.Services
{
    internal interface IReporteService
    {
        Task<IEnumerable<Puesto>> GetAllAsync();
        Task<Puesto> GetByIdAsync(Guid id);
        Task <Puesto> CreateAsync(Puesto puesto);
        Task<Puesto> UpdateASync(Guid id, Puesto datosActualizados);
        Task AsignarDuenoAsync(Guid puestoId, Guid duenoId);
        Task LiberarPuestoAsync(Guid puestoId);
        Task<IEnumerable<Puesto>> GetByDuenoAsync(Guid duenoId);

    }
}
