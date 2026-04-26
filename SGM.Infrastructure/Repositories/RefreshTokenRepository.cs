using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _db;
        public RefreshTokenRepository(AppDbContext db) => _db = db;

        public async Task<RefreshToken?> GetByTokenAsync(string token) =>
            await _db.RefreshTokens.Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.Token == token);

        public async Task AddAsync(RefreshToken refreshToken)
        {
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();
        }

        public async Task RevokeByUsuarioIdAsync(Guid usuarioId) =>
            await _db.RefreshTokens
                .Where(r => r.UsuarioId == usuarioId && !r.Revocado)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Revocado, true));

        public async Task EliminarExpiradosAsync() =>
            await _db.RefreshTokens
                .Where(r => r.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync();
    }
}
