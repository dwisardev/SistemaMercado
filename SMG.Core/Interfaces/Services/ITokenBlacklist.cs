namespace SMG.Core.Interfaces.Services
{
    public interface ITokenBlacklist
    {
        Task RevokeAsync(string jti, DateTime expiry);
        bool IsRevoked(string jti);
        Task LoadFromDatabaseAsync();
    }
}
