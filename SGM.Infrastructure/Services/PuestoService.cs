using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Interfaces.Services;
using SGM.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class PuestoService : IPuestoService
    {
        private readonly IPuestoRepository _repo;
        public PuestoService(IPuestoRepository repo) => _repo = repo;

        public Task<IEnumerable<Puesto>> getAllAsync() => _repo.GetAllAsync();
        public Task<Puesto> GetByIdAsync(Guid id) =>
            _repo.GetByIdAsync(id).ContinueWith(t => t.Result ?? throw new KeyNotFoundException($"Puesto {id} no encontrado."));

        public async Task<Puesto> CreateAsync(Puesto puesto) => await _repo.AddAsync(puesto);

        public async Task AsignarDuenoAsync(Guid id, Puesto datos)
        {
            var puesto = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Puesto {id} no encontrado.");
            puesto.DuenoId = datos.DuenoId;
            puesto.Estado = EstadoPuesto.Activo;
            puesto.FechaAsignacion = DateOnly.FromDateTime(DateTime.UtcNow);
            await _repo.UpdateAsync(puesto);
        }

        public async Task LiberarPuestoAsync(Guid puestoId)
        {
            var puesto = await _repo.GetByIdAsync(puestoId) ?? throw new KeyNotFoundException($"Puesto {puestoId} no encontrado.");
            puesto.DuenoId = null;
            puesto.FechaAsignacion = null;
            await _repo.UpdateAsync(puesto);
        }

        public async Task UpdatePuestoAsync(Guid id, Puesto datos)
        {
            var puesto = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Puesto {id} no encontrado.");
            puesto.Descripcion = datos.Descripcion;
            puesto.Ubicacion = datos.Ubicacion;
            puesto.Estado = datos.Estado;
            puesto.UpdateAt = DateTime.UtcNow;
            await _repo.UpdateAsync(puesto);
        }

        public Task<IEnumerable<Puesto>> GetByDuenoAsync(Guid duenoId) => _repo.GetByDuenoAsync(duenoId);
    }
}
