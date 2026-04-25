namespace SGM.API.DTOs.Response
{
    public class PagoResponseDto
    {
        public Guid Id { get; set; }
        public Guid DeudaId { get; set; }
        public string? PuestoNumero { get; set; }
        public string? DuenoNombre { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime FechaPago { get; set; }
        public string? CajeroNombre { get; set; }
        public string? NumeroComprobante { get; set; }
        public string Metodo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? ReferenciaPago { get; set; }
        public string? MotivoAnulacion { get; set; }
    }
}
