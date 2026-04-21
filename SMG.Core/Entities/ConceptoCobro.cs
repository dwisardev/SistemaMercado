using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SGM.Core.Entities
{
    public class ConceptoCobro
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal MontoDefault { get; set; }
        public bool EsRecurrente { get; set; } = true;
        public int DiaEmision { get; set; } = 1;
        public bool Activo { get; set; } = true;
        public JsonDocument? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación: un concepto genera muchas deudas
        public ICollection<Deuda> Deudas { get; set; } = new List<Deuda>();
    }
}
