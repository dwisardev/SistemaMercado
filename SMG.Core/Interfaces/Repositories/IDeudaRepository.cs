using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Repositories
{
    public interface IDeudaRepository
    {
        //Lectura
        Task<IEnumerable<Deuda>> GetAllAsync(Guid puestoId);
        Task<IEnumerable<Deuda>> GetFiltradosAsync(Guid? puestoId, string? estado, string? periodo);
        Task<IEnumerable<Deuda>> GetPendienteByPuestoAsync(Guid puestoId);
        Task<IEnumerable<Deuda>> GetPendienteAsync();
        Task<IEnumerable<Deuda>> GetVencidadAsync();
        Task<Deuda?> GetByIdAsync(Guid id);

        //Escritura 
        Task<Deuda?> AddAsync(Deuda deuda);
        Task AddRangeAsync (IEnumerable<Deuda> deudas);
        Task UpdatedAsync (Deuda deuda);
        //Verificada 
        Task<bool> ExisteDuplicadoAsync(Guid puestoId, decimal monto, string periodo);
    }
}
