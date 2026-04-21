using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;



namespace SMG.Core.Interfaces.Services
{
    public interface IUsuarioService
    {
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(Guid id);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(Guid id, Usuario datosActualizados);

        Task DesactivateAsync(Guid id);



    }
}
