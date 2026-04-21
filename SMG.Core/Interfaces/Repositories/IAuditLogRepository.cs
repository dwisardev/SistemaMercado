using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Repositories
{
    public interface IAuditLogRepository
    {
        Task TaskAsync(AuditLog log);
        Task<IEnumerable<AuditLog>>GetByFechaRangoAsyn(DateTime desde , DateTime hasta);
        Task<IEnumerable<AuditLog>> GetByUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<AuditLog>> GetByAccionAsync(string accion);

    }
}
