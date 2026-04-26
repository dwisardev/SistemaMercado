using SGM.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string email, string password);
        Task LogoutAsync(string accessToken, string? refreshToken = null);
        Task<LoginResult> RefreshAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
    }
}
