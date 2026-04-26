using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _db;
        public AuditLogRepository(AppDbContext db) => _db = db;

        public async Task TaskAsync(AuditLog log)
        {
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByFechaRangoAsyn(DateTime desde, DateTime hasta) =>
            await _db.AuditLogs.AsNoTracking()
                .Where(a => a.Timestamp >= desde && a.Timestamp <= hasta)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

        public async Task<IEnumerable<AuditLog>> GetByUsuarioAsync(Guid usuarioId) =>
            await _db.AuditLogs.AsNoTracking()
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

        public async Task<IEnumerable<AuditLog>> GetByAccionAsync(string accion) =>
            await _db.AuditLogs.AsNoTracking()
                .Where(a => a.Accion.StartsWith(accion))
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
    }
}
