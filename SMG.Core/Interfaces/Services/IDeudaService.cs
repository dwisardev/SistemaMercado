using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Interfaces.Services
{
    public interface IDeudaService
    {
        Task<IEnumerable<Deuda>> GetByPuestoAsync(Guid puestoId);
        Task<IEnumerable<Deuda>> GetPendienteByAsync(Guid duenoId);
        Task<Deuda> CargaIndividualAsync(
            Guid puestoId, Guid duenoId,
            decimal monto, string periodo, DateOnly? fechaVencimiento, string descripcion,
            Guid generadoPor);
        Task AnularDeudaAsync(Guid deudaId, string motivo, Guid anuladoPor);
    }
}
