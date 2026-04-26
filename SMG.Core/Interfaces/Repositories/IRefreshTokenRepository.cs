using SGM.Core.Entities;

namespace SMG.Core.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task AddAsync(RefreshToken refreshToken);
        Task RevokeByUsuarioIdAsync(Guid usuarioId);
        Task EliminarExpiradosAsync();
    }
}
