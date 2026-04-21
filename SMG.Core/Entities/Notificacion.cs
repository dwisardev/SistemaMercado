using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SGM.Core.Entities
{
    public class Notificacion
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public Guid? DestinatarioId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Canal { get; set; } = "sistema";
        public bool Leida { get; set; } = false;
        public JsonDocument? Datos { get; set; }
        public DateTime? EnviadaAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public Usuario? Destinatario { get; set; }
    }
}
