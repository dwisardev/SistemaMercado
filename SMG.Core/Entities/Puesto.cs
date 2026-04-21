using SGM.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Entities
{
    public class Puesto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public string? Ubicacion { get; set; }

        public decimal? Aream2 { get; set; }

        public Guid? DuenoId { get; set; }

        public EstadoPuesto Estado { get; set; } = EstadoPuesto.Activo;
        public DateOnly? FechaAsignacion { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdateAt {  get; set; } = DateTime.UtcNow;

        //Navegaciones (No son colunas RF las lineas con includes)

        public Usuario? Dueno { get; set; }
        public ICollection<Deuda> Deudas{ get; set; } = new List <Deuda>();
    }
}
