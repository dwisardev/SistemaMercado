using SGM.Core.Entities;
using SMG.Core.Enums;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SMG.Core.Repositories
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(Guid id);
        Task<Usuario?> GetByNameAsync(string email);
        Task<IEnumerable<Usuario>> GetByRolAsync(RolUsuario rol);
        //Escritura
        Task<Usuario>AddAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);

        //Verifcación
        Task<bool> ExisteEmailAsync(string email, Guid? excludeId = null);
        Task<bool> ExisteDniAsync(string dni, Guid? excludeId = null);
        Task<(IEnumerable<Usuario> Data, int Total)> GetPaginadoAsync(string? search, string? rol, int page, int pageSize);
    }
}
