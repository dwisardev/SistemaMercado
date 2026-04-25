namespace SGM.API.DTOs.Request
{
    public class UpdateUsuarioDto
    {
        public string? NombreCompleto { get; set; }
        public string? Rol { get; set; }
        public bool? Activo { get; set; }
        public string? NuevaPassword { get; set; }
    }
}
