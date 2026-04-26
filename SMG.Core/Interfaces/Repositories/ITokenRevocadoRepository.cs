using SGM.Core.Entities;

namespace SMG.Core.Repositories
{
    public interface ITokenRevocadoRepository
    {
        Task AddAsync(TokenRevocado token);
        Task<IEnumerable<TokenRevocado>> GetActivosAsync();
        Task EliminarExpiradosAsync();
    }
}
