namespace SGM.API.DTOs.Response
{
    public class PuestoResponseDto
    {
        public Guid Id { get; set; }
        public string NumeroPuesto { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public Guid? DuenoId { get; set; }
        public string? DuenoNombre { get; set; }
        public string? DuenoEmail { get; set; }
        public string? Sector { get; set; }
        public decimal? TarifaMensual { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
