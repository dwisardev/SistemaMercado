using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Interfaces.Services
{
    public interface INotificacionService
    {
        Task CrearNotificacionAsync(
            Guid destinatarioId,
            string titulo,
            string mensaje);
        Task<IEnumerable<Notificacion>> GetMisNotificacionesAsync(Guid usuarioId);
        Task<int> ContarLeidasAsync(Guid usuarioId);

        // Actualizar estado 
        Task MarcarLeidaAsync(Guid notificacionId);
        Task MarcarTodasLeidasAsync(Guid usuarioId);
    }
}
