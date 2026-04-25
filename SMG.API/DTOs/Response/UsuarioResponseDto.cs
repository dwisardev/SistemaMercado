namespace SGM.API.DTOs.Response
{
    public class UsuarioResponseDto
    {
        public Guid Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
