using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Enums;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repo;
        public UsuarioService(IUsuarioRepository repo) => _repo = repo;

        public Task<IEnumerable<Usuario>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<Usuario?> GetByIdAsync(Guid id) => await _repo.GetByIdAsync(id);

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);
            return await _repo.AddAsync(usuario);
        }

        public async Task<Usuario> UpdateAsync(Guid id, Usuario datos)
        {
            var usuario = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Usuario {id} no encontrado.");
            usuario.NombreCompleto = datos.NombreCompleto;
            usuario.Email = datos.Email;
            usuario.Rol = datos.Rol;
            usuario.Activo = datos.Activo;
            if (!string.IsNullOrEmpty(datos.PasswordHash))
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(datos.PasswordHash);
            await _repo.UpdateAsync(usuario);
            return usuario;
        }

        public async Task DesactivateAsync(Guid id)
        {
            var usuario = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Usuario {id} no encontrado.");
            usuario.Activo = false;
            await _repo.UpdateAsync(usuario);
        }
    }
}
