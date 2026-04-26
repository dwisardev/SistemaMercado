using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Enums;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _db;

        public UsuarioRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<Usuario>> GetAllAsync() =>
            await _db.Usuarios.AsNoTracking().ToListAsync();

        public async Task<Usuario?> GetByIdAsync(Guid id) =>
            await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        public async Task<Usuario?> GetByNameAsync(string email) =>
            await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        public async Task<IEnumerable<Usuario>> GetByRolAsync(RolUsuario rol) =>
            await _db.Usuarios.AsNoTracking().Where(u => u.Rol == rol).ToListAsync();

        public async Task<Usuario> AddAsync(Usuario usuario)
        {
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            return usuario;
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            _db.Usuarios.Update(usuario);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExisteEmailAsync(string email, Guid? excludeId = null) =>
            await _db.Usuarios.AnyAsync(u =>
                u.Email.ToLower() == email.ToLower() && u.Id != excludeId);

        public async Task<bool> ExisteDniAsync(string dni, Guid? excludeId = null) =>
            await _db.Usuarios.AnyAsync(u =>
                u.Dni == dni && u.Id != excludeId);

        public async Task<(IEnumerable<Usuario> Data, int Total)> GetPaginadoAsync(
            string? search, string? rol, int page, int pageSize)
        {
            var query = _db.Usuarios.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u =>
                    u.NombreCompleto.Contains(search) ||
                    u.Email.Contains(search));

            if (!string.IsNullOrWhiteSpace(rol) && Enum.TryParse<RolUsuario>(rol, true, out var rolEnum))
                query = query.Where(u => u.Rol == rolEnum);

            var total = await query.CountAsync();
            var data  = await query
                .OrderBy(u => u.NombreCompleto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }
    }
}
