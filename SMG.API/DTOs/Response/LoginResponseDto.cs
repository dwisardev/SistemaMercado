namespace SGM.API.DTOs.Response
{
    public class LoginResponseDto
    {
        public string   Token                 { get; set; } = string.Empty;
        public Guid     UsuarioId             { get; set; }
        public string   NombreCompleto        { get; set; } = string.Empty;
        public string   Email                 { get; set; } = string.Empty;
        public string   Rol                   { get; set; } = string.Empty;
        public DateTime ExpiresAt             { get; set; }
        public string   RefreshToken          { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
