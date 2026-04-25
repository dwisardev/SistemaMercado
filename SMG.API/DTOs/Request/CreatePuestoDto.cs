namespace SGM.API.DTOs.Request
{
    public class CreatePuestoDto
    {
        public string NumeroPuesto { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Sector { get; set; }
        public decimal? TarifaMensual { get; set; }
    }
}
