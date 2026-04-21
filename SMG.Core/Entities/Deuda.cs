using SGM.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Entities
{
    public class Deuda
    {
        public Guid Id { get; set; }

        public Guid PuestoId { get; set; }

        public Guid ConceptoId { get; set; }

        public decimal Monto { get; set; }

        public string Periodo { get; set; }

        public DateOnly FechaEmision { get; set; }

        public DateOnly? FechaVencimiento { get; set; }

        public EstadoDeuda Estado { get; set; } = EstadoDeuda.Pendiente;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Puesto? Puesto { get; set; }

        public ConceptoCobro Concepto { get; set; } = null!;

        public Pago? Pago { get; set; }

        public Guid? LoteCargaId { get; set; }

        public Guid? GeneradoPor { get; set; }


    }
}
