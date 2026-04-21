using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SGM.Core.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? TablaAfectada { get; set; }
        public Guid? RegistroId { get; set; }
        public Guid? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public JsonDocument? Detalle { get; set; }
        public JsonDocument? DatosAnteriores { get; set; }
        public JsonDocument? DatosNuevos { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
