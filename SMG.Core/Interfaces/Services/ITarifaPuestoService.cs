using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Interfaces.Services
{
    public interface ITarifaPuestoService
    {
        Task<IEnumerable<TarifaPuesto>> GetTarifaPuestosAsync(Guid puestoId);
        Task<TarifaPuesto> CreateAsync(TarifaPuesto tarifa);
        Task<TarifaPuesto> UpdateAsync(Guid id, TarifaPuesto datos);
        Task<decimal> GetMontoVigenteAsync(Guid puestoId, Guid conceptoId);
    }
}
