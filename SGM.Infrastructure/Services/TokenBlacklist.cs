using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SGM.Core.Entities;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class TokenBlacklist : ITokenBlacklist
    {
        private readonly ConcurrentDictionary<string, DateTime> _revoked = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public TokenBlacklist(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public async Task RevokeAsync(string jti, DateTime expiry)
        {
            _revoked[jti] = expiry;
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITokenRevocadoRepository>();
            await repo.AddAsync(new TokenRevocado { Jti = jti, ExpiresAt = expiry });
        }

        public bool IsRevoked(string jti)
        {
            if (!_revoked.TryGetValue(jti, out var expiry)) return false;
            if (DateTime.UtcNow > expiry) { _revoked.TryRemove(jti, out _); return false; }
            return true;
        }

        public async Task LoadFromDatabaseAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITokenRevocadoRepository>();
            var activos = await repo.GetActivosAsync();
            foreach (var t in activos)
                _revoked[t.Jti] = t.ExpiresAt;
        }
    }
}
