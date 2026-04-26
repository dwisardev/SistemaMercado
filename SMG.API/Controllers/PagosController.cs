using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SGM.API.DTOs.Request;
using SGM.API.DTOs.Response;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SGM.Core.Interfaces.Repositories;
using SMG.Core.Repositories;
using System.Security.Claims;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/pagos")]
    [Authorize]
    public class PagosController : ControllerBase
    {
        private readonly IPagoRepository _pagos;
        private readonly IDeudaRepository _deudas;
        private readonly INotificacionRepository _notificaciones;

        public PagosController(IPagoRepository pagos, IDeudaRepository deudas, INotificacionRepository notificaciones)
        {
            _pagos = pagos;
            _deudas = deudas;
            _notificaciones = notificaciones;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Cajero")]
        public async Task<ActionResult<PaginadoDto<PagoResponseDto>>> GetAll(
            [FromQuery] string? fechaInicio,
            [FromQuery] string? fechaFin,
            [FromQuery] Guid? puestoId,
            [FromQuery] string? estado,
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 25)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var desde = fechaInicio is not null && DateOnly.TryParse(fechaInicio, out var d) ? d : DateOnly.MinValue;
            var hasta = fechaFin is not null && DateOnly.TryParse(fechaFin, out var h) ? h : DateOnly.FromDateTime(DateTime.UtcNow);

            var (data, total) = await _pagos.GetFiltradosPaginadoAsync(desde, hasta, puestoId, estado, page, pageSize);
            return Ok(new PaginadoDto<PagoResponseDto>
            {
                Data     = data.Select(ToDto),
                Total    = total,
                Page     = page,
                PageSize = pageSize,
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Cajero")]
        public async Task<ActionResult<PagoResponseDto>> Registrar([FromBody] RegistrarPagoDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var deuda = await _deudas.GetByIdAsync(dto.DeudaId);
            if (deuda is null) return NotFound(new { message = "Deuda no encontrada." });
            if (deuda.Estado != EstadoDeuda.Pendiente)
                return BadRequest(new { message = "La deuda no está en estado pendiente." });

            var metodo = Enum.TryParse<MetodoPago>(dto.Metodo, true, out var m) ? m : MetodoPago.Efectivo;
            var ultimo = await _pagos.GetUltimoNroComprobanteAsync();
            var nroComprobante = GenerarNroComprobante(ultimo);

            var pago = new Pago
            {
                DeudaId = dto.DeudaId,
                MontoPagado = dto.MontoPagado,
                FechaPago = DateTime.UtcNow,
                CajeroId = userId,
                NumeroComprobante = nroComprobante,
                Metodo = metodo,
                Estado = EstadoPago.Activo,
                ReferenciaPago = dto.ReferenciaPago,
                Observaciones = dto.Observaciones,
            };

            var created = await _pagos.AddAsync(pago);
            var full = await _pagos.GetByIdAsync(created.Id);

            // Notificar al dueño del puesto si existe
            var duenoId = deuda.Puesto?.DuenoId;
            if (duenoId.HasValue)
            {
                var concepto = deuda.Concepto?.Nombre ?? "deuda";
                var puesto = deuda.Puesto?.Codigo ?? "";
                await _notificaciones.AddAsync(new Notificacion
                {
                    Tipo = "pago_registrado",
                    DestinatarioId = duenoId,
                    Titulo = "Pago registrado",
                    Mensaje = $"Se registró un pago de S/ {dto.MontoPagado:0.00} por concepto '{concepto}' (Puesto {puesto}). Comprobante: {nroComprobante}.",
                    Canal = "sistema",
                });
            }

            return CreatedAtAction(nameof(Registrar), ToDto(full!));
        }

        [HttpPatch("{id:guid}/anular")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagoResponseDto>> Anular(Guid id, [FromBody] AnularPagoDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pago = await _pagos.GetByIdAsync(id);
            if (pago is null) return NotFound(new { message = "Pago no encontrado." });
            if (pago.Estado == EstadoPago.Anulado)
                return BadRequest(new { message = "El pago ya está anulado." });

            pago.Estado = EstadoPago.Anulado;
            pago.MotivoAnulacion = dto.MotivoAnulacion;
            pago.AnuladoPor = userId;
            pago.FechaAnulacion = DateTime.UtcNow;
            await _pagos.UpdateAsync(pago);

            // Notificar al dueño sobre la anulación
            var duenoId = pago.Deuda?.Puesto?.DuenoId;
            if (duenoId.HasValue)
            {
                await _notificaciones.AddAsync(new Notificacion
                {
                    Tipo = "pago_anulado",
                    DestinatarioId = duenoId,
                    Titulo = "Pago anulado",
                    Mensaje = $"El pago {pago.NumeroComprobante} de S/ {pago.MontoPagado:0.00} fue anulado. Motivo: {dto.MotivoAnulacion}.",
                    Canal = "sistema",
                });
            }

            return Ok(ToDto(pago));
        }

        [HttpGet("{id:guid}/comprobante")]
        public async Task<ActionResult> GetComprobante(Guid id)
        {
            var pago = await _pagos.GetByIdAsync(id);
            if (pago is null) return NotFound();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // Encabezado
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("MercaGest").Bold().FontSize(18);
                                c.Item().Text("Sistema de Gestión de Mercado").FontSize(9).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text("COMPROBANTE DE PAGO").Bold().FontSize(13);
                                c.Item().Text(pago.NumeroComprobante ?? "—").FontSize(11).FontColor(Colors.Blue.Medium);
                            });
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Datos principales
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });

                            void Fila(string label, string value)
                            {
                                table.Cell().PaddingBottom(4).Column(c =>
                                {
                                    c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(value).SemiBold();
                                });
                            }

                            Fila("Puesto", pago.Deuda?.Puesto?.Codigo ?? "—");
                            Fila("Fecha de pago", pago.FechaPago.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                            Fila("Dueño", pago.Deuda?.Puesto?.Dueno?.NombreCompleto ?? "—");
                            Fila("Concepto", pago.Deuda?.Concepto?.Nombre ?? "—");
                            Fila("Cajero", pago.Cajero?.NombreCompleto ?? "—");
                            Fila("Período", pago.Deuda?.Periodo ?? "—");
                            Fila("Método de pago", pago.Metodo.ToString());
                            Fila("Referencia", pago.ReferenciaPago ?? "—");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Monto total
                        col.Item().AlignRight().Column(c =>
                        {
                            c.Item().Text("TOTAL PAGADO").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"Q {pago.MontoPagado:N2}").Bold().FontSize(22).FontColor(Colors.Blue.Darken2);
                        });

                        if (pago.Estado == EstadoPago.Anulado)
                        {
                            col.Item().PaddingTop(8).Background(Colors.Red.Lighten4).Padding(6)
                                .Text($"ANULADO — {pago.MotivoAnulacion}").Bold().FontColor(Colors.Red.Darken2);
                        }

                        col.Item().PaddingTop(12).AlignCenter()
                            .Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Lighten1);
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            var filename = $"comprobante-{pago.NumeroComprobante ?? id.ToString()}.pdf";
            return File(bytes, "application/pdf", filename);
        }

        private static string GenerarNroComprobante(string? ultimo)
        {
            if (ultimo is null || !ultimo.StartsWith("COMP-")) return "COMP-00001";
            if (int.TryParse(ultimo.Replace("COMP-", ""), out var n)) return $"COMP-{(n + 1):D5}";
            return $"COMP-{DateTime.UtcNow:yyMMddHHmmss}";
        }

        private static PagoResponseDto ToDto(Pago p) => new()
        {
            Id = p.Id,
            DeudaId = p.DeudaId,
            PuestoNumero = p.Deuda?.Puesto?.Codigo,
            DuenoNombre = p.Deuda?.Puesto?.Dueno?.NombreCompleto,
            MontoPagado = p.MontoPagado,
            FechaPago = p.FechaPago,
            CajeroNombre = p.Cajero?.NombreCompleto,
            NumeroComprobante = p.NumeroComprobante,
            Metodo = p.Metodo.ToString(),
            Estado = p.Estado.ToString(),
            ReferenciaPago = p.ReferenciaPago,
            MotivoAnulacion = p.MotivoAnulacion,
        };
    }
}
