using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Results
{
    public record LoginResult
    (
        string Token,
        Guid UsuarioId,
        string NombreCompleto,
        string Email,
        string Rol,
        DateTime ExpiresAt


       );

    
}
