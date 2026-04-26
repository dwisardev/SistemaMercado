using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class TokenRevocadoRepository : ITokenRevocadoRepository
    {
        private readonly AppDbContext _db;
        public TokenRevocadoRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(TokenRevocado token)
        {
            var exists = await _db.TokensRevocados.AnyAsync(t => t.Jti == token.Jti);
            if (!exists)
            {
                _db.TokensRevocados.Add(token);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TokenRevocado>> GetActivosAsync() =>
            await _db.TokensRevocados
                .AsNoTracking()
                .Where(t => t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

        public async Task EliminarExpiradosAsync() =>
            await _db.TokensRevocados
                .Where(t => t.ExpiresAt <= DateTime.UtcNow)
                .ExecuteDeleteAsync();
    }
}
