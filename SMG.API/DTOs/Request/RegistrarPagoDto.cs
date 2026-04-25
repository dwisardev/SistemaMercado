namespace SGM.API.DTOs.Request
{
    public class RegistrarPagoDto
    {
        public Guid DeudaId { get; set; }
        public decimal MontoPagado { get; set; }
        public string Metodo { get; set; } = "Efectivo";
        public string? ReferenciaPago { get; set; }
        public string? Observaciones { get; set; }
    }
}
