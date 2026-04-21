using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Entities
{
    public class TarifaPuesto
    {
        public Guid Id { get; set; }
        public Guid PuestoId { get; set; }
        public Guid ConceptoId { get; set; }
        public decimal Monto { get; set; }
        public DateOnly VigenteDesde { get; set; }
        public DateOnly? VigenteHasta { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Puesto Puesto { get; set; } = null!;
        public ConceptoCobro Concepto { get; set; } = null!;
    }
}
