using SGM.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task <LoginResult> LoginAsync(string email, string password);
        Task LogoutAsync(string token);
        Task<bool> ValidateTokenAsync(string token);
    }
}
