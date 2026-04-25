using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Repositories;
using SMG.Core.Interfaces.Services;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Services
{
    public class DeudaService : IDeudaService
    {
        private readonly IDeudaRepository _deudas;
        private readonly IPuestoRepository _puestos;

        public DeudaService(IDeudaRepository deudas, IPuestoRepository puestos)
        {
            _deudas = deudas;
            _puestos = puestos;
        }

        public Task<IEnumerable<Deuda>> GetByPuestoAsync(Guid puestoId) =>
            _deudas.GetAllAsync(puestoId);

        public Task<IEnumerable<Deuda>> GetPendienteByAsync(Guid duenoId) =>
            _deudas.GetPendienteAsync();

        public async Task<Deuda> CargaIndividualAsync(
            Guid puestoId, Guid duenoId,
            decimal monto, string periodo, DateOnly? fechaVencimiento, string descripcion,
            Guid generadoPor)
        {
            var deuda = new Deuda
            {
                PuestoId = puestoId,
                ConceptoId = duenoId,   // duenoId carries conceptoId in this context
                Monto = monto,
                Periodo = periodo,
                FechaEmision = DateOnly.FromDateTime(DateTime.UtcNow),
                FechaVencimiento = fechaVencimiento,
                Estado = EstadoDeuda.Pendiente,
                GeneradoPor = generadoPor,
            };
            return await _deudas.AddAsync(deuda) ?? throw new InvalidOperationException("No se pudo crear la deuda.");
        }

        public async Task AnularDeudaAsync(Guid deudaId, string motivo, Guid anuladoPor)
        {
            var deuda = await _deudas.GetByIdAsync(deudaId) ?? throw new KeyNotFoundException($"Deuda {deudaId} no encontrada.");
            deuda.Estado = EstadoDeuda.Anulada;
            await _deudas.UpdatedAsync(deuda);
        }
    }
}
