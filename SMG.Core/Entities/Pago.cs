using SGM.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Entities
{
    public class Pago
    {
        public Guid Id { get; set; }
        public Guid DeudaId { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime FechaPago { get; set; }
        public Guid CajeroId { get; set; }
        public string ? NumeroComprobante { get; set; }

        public MetodoPago Metodo { get; set; }
        public EstadoPago  Estado { get; set; } = EstadoPago.Activo;
        public string ? ReferenciaPago { get; set; }
        public string ? Observaciones { get; set; }

        public Guid? AnuladoPor { get; set; }
        public string? ComprobanteUrl { get; set; }

        public string? MotivoAnulacion { get; set; } 
        public DateTime? FechaAnulacion { get; set; }
      
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdataAt { get; set; } = DateTime.UtcNow;

        public Deuda Deuda { get; set; } = null!;
        public Usuario Cajero { get; set; } = null!;



    }
}
