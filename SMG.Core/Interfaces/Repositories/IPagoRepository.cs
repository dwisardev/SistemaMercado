using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data;


namespace SMG.Core.Repositories
{
    public interface IPagoRepository
    {
        Task<IEnumerable<Pago>> GetByFechaAsync(DateOnly fecha);
        Task<IEnumerable<Pago>> GetByCajeroAsync(Guid cajeroId, DateOnly fecha);
        Task<IEnumerable<Pago>> GetByPuestoAsync(Guid puestoId);
        Task<Pago?> GetByIdAsync(Guid id);
        Task<Pago?> GetByDeudaIdAsync(Guid deudaId);
        Task<Pago> AddAsync(Pago pago);

        Task UpdateAsync (Pago pago);
        Task <string?> GetUltimoNroComprobanteAsync();



    }
}
