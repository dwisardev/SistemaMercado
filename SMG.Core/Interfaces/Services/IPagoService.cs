using SGM.Core.Entities;
using SGM.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Interfaces.Services
{
    public interface IPagoService
    {
        Task<CajaDiariaResult> GetCajaDiariaAsync(DateOnly fecha);
        Task<IEnumerable<DeudaPendienteResult>> GetDeudaPendienteResultsAsync();
        Task<IEnumerable<MorosidadResult>> GetMorosidadsAsync();
        Task<IEnumerable<Pago>> GetHistorialPagosByDuenoAsync(Guid duenoId);

    }
}
