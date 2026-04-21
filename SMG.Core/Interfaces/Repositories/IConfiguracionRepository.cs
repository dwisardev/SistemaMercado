using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMG.Core.Repositories
{
    public interface IConfiguracionRepository
    {
        Task<Configuracion?> GetAsync(string clave);
        Task<IEnumerable<Configuracion>> GetByCategoriaAsync(string categoria);
        Task<IEnumerable<Configuracion>> GetAllAsync();
        Task UpserAsync(Configuracion configuracion);
    }
}
