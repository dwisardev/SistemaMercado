using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SGM.Core.Entities
{
    public class Configuracion
    {
        public Guid Id { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string Categoria { get; set; } = "general";
        public JsonDocument Valor { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? UpdatedBy { get; set; }
    }
}
