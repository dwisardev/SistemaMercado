using Microsoft.EntityFrameworkCore;
using SGM.Core.Entities;
using SGM.Core.Interfaces.Repositories;
using SGM.Infrastructure.Data;

namespace SGM.Infrastructure.Repositories
{
    public class NotificacionRepository : INotificacionRepository
    {
        private readonly AppDbContext _db;
        public NotificacionRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<Notificacion>> GetByUsuarioAsync(Guid usuarioId) =>
            await _db.Notificaciones.AsNoTracking()
                .Where(n => n.DestinatarioId == usuarioId)
                .OrderByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Notificacion>> GetNoleidasAsync(Guid usuarioId) =>
            await _db.Notificaciones.AsNoTracking()
                .Where(n => n.DestinatarioId == usuarioId && !n.Leida)
                .OrderByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<int> ContarNoLeidasAsync(Guid usuarioId) =>
            await _db.Notificaciones.CountAsync(n => n.DestinatarioId == usuarioId && !n.Leida);

        public async Task<Notificacion?> GetByIdAsync(Guid id) =>
            await _db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id);

        public async Task<Notificacion> AddAsync(Notificacion notificacion)
        {
            _db.Notificaciones.Add(notificacion);
            await _db.SaveChangesAsync();
            return notificacion;
        }

        public async Task addRangeAsync(IEnumerable<Notificacion> notificaciones)
        {
            _db.Notificaciones.AddRange(notificaciones);
            await _db.SaveChangesAsync();
        }

        public async Task MarcarLeidaAsync(Guid id)
        {
            var n = await _db.Notificaciones.FindAsync(id);
            if (n is not null) { n.Leida = true; await _db.SaveChangesAsync(); }
        }

        public async Task MarcarTodasLeidasAsync(Guid usuarioId)
        {
            await _db.Notificaciones
                .Where(n => n.DestinatarioId == usuarioId && !n.Leida)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
        }
    }
}
