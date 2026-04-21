using SMG.Core.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SGM.Core.Entities
{
    public class Usuario
    {
        // Columnas
        public Guid Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Dni { get; set; } = string.Empty;
        public RolUsuario Rol { get; set; } = RolUsuario.Dueno;
        public bool Activo { get; set; } = true;
        public Guid? AuthUserId { get; set; }
        public JsonDocument? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public ICollection<Puesto> Puestos { get; set; } = new List<Puesto>();
        public ICollection<Pago> PagosRegistrados { get; set; } = new List<Pago>();
    }
}
