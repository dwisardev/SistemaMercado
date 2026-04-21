using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Entities
{
    public class HistorialDueno
    {
        public Guid Id { get; set; }
        public Guid PuestoId { get; set; }
        public Guid? DuenoId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public string? Motivo { get; set; }
        public Guid? RegistradoPor { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Puesto Puesto { get; set; } = null!;
        public Usuario? Dueno { get; set; }
    }
}
