using SGM.Core.Entities;
using SGM.Core.Interfaces.Repositories;
using SGM.Core.Interfaces.Services;

namespace SGM.Infrastructure.Services
{
    public class NotificacionService : INotificacionService
    {
        private readonly INotificacionRepository _repo;
        public NotificacionService(INotificacionRepository repo) => _repo = repo;

        public async Task CrearNotificacionAsync(Guid destinatarioId, string titulo, string mensaje)
        {
            var n = new Notificacion
            {
                Tipo = "sistema",
                DestinatarioId = destinatarioId,
                Titulo = titulo,
                Mensaje = mensaje,
                CreatedAt = DateTime.UtcNow,
            };
            await _repo.AddAsync(n);
        }

        public Task<IEnumerable<Notificacion>> GetMisNotificacionesAsync(Guid usuarioId) =>
            _repo.GetByUsuarioAsync(usuarioId);

        public Task<int> ContarLeidasAsync(Guid usuarioId) =>
            _repo.ContarNoLeidasAsync(usuarioId);

        public Task MarcarLeidaAsync(Guid notificacionId) =>
            _repo.MarcarLeidaAsync(notificacionId);

        public Task MarcarTodasLeidasAsync(Guid usuarioId) =>
            _repo.MarcarTodasLeidasAsync(usuarioId);
    }
}
