using System.ComponentModel.DataAnnotations;

namespace SGM.API.DTOs.Request
{
    public class CambiarPasswordDto
    {
        [Required]
        public string PasswordActual { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
