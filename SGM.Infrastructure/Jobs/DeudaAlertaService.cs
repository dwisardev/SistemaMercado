using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Infrastructure.Data;
using SMG.Core.Repositories;

namespace SGM.Infrastructure.Jobs
{
    public class DeudaAlertaService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeudaAlertaService> _logger;

        public DeudaAlertaService(IServiceScopeFactory scopeFactory, ILogger<DeudaAlertaService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once at startup then every day at midnight UTC
            await RunDailyTasksAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                    await RunDailyTasksAsync(stoppingToken);
            }
        }

        private async Task RunDailyTasksAsync(CancellationToken ct)
        {
            await NotificarDeudasProximasAsync(ct);
            await LimpiarTokensRevocadosExpiradosAsync(ct);
        }

        private async Task NotificarDeudasProximasAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
                var en3Dias = hoy.AddDays(3);

                var deudas = await db.Deudas
                    .Include(d => d.Puesto)
                    .Include(d => d.Concepto)
                    .Where(d => d.Estado == EstadoDeuda.Pendiente
                             && d.FechaVencimiento == en3Dias
                             && d.Puesto!.DuenoId != null)
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (deudas.Count == 0) return;

                var notifs = deudas.Select(d => new Notificacion
                {
                    Tipo           = "deuda_proxima_vencer",
                    DestinatarioId = d.Puesto!.DuenoId,
                    Titulo         = "Deuda próxima a vencer",
                    Mensaje        = $"Tu deuda por '{d.Concepto?.Nombre}' (Puesto {d.Puesto?.Codigo}) " +
                                     $"de S/ {d.Monto:0.00} vence en 3 días ({d.FechaVencimiento:dd/MM/yyyy}).",
                    Canal          = "sistema",
                }).ToList();

                db.Notificaciones.AddRange(notifs);
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("DeudaAlertaService: {Count} alertas de vencimiento enviadas.", notifs.Count);
            }
            catch (OperationCanceledException) { /* graceful shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeudaAlertaService: error al procesar alertas de vencimiento.");
            }
        }

        private async Task LimpiarTokensRevocadosExpiradosAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITokenRevocadoRepository>();
                await repo.EliminarExpiradosAsync();
                _logger.LogInformation("DeudaAlertaService: tokens revocados expirados eliminados.");
            }
            catch (OperationCanceledException) { /* graceful shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeudaAlertaService: error al limpiar tokens revocados expirados.");
            }
        }
    }
}
