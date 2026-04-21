using System;
using System.Collections.Generic;
using System.Text;
using SGM.Core.Entities;
namespace SGM.Core.Interfaces.Repositories
{
    public interface INotificacionRepository
    {
        //Lectura 
        Task <IEnumerable<Notificacion>> GetByUsuarioAsync(Guid usuarioId);
        Task <IEnumerable<Notificacion>>GetNoleidasAsync (Guid usuarioId);

        Task <int> ContarNoLeidasAsync (Guid usuarioId);
        Task <Notificacion?> GetByIdAsync(Guid id);
        //Escritura
        Task <Notificacion> AddAsync(Notificacion notificacion);
        Task addRangeAsync (IEnumerable<Notificacion> notificaciones);
        Task MarcarLeidaAsync(Guid id);
        Task MarcarTodasLeidasAsync (Guid UsuarioId);

    }
}
