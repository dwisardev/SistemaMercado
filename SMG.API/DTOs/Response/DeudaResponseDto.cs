namespace SGM.API.DTOs.Response
{
    public class DeudaResponseDto
    {
        public Guid Id { get; set; }
        public Guid PuestoId { get; set; }
        public string? PuestoNumero { get; set; }
        public string? DuenoNombre { get; set; }
        public Guid ConceptoId { get; set; }
        public string? ConceptoNombre { get; set; }
        public decimal Monto { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? FechaVencimiento { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
