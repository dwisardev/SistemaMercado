using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Repositories
{
    internal interface IPuestoRepository
    {
        Task<IEnumerable<Puesto>> GetAllAsync();
        Task<Puesto?> GetByIdAsync(Guid id);
        Task<Puesto?> GetByCodigoAsync(string codigo);
        Task<IEnumerable<Puesto>> GetByDuenoAsync(Guid duenoId);
        Task<IEnumerable<Puesto>> GetActivosAsync();
        Task<Puesto> AddAsync(Puesto puesto);
        Task UpdateAsync(Puesto puesto);
        Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId = null);

    }
}
