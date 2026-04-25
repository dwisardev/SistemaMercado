namespace SGM.API.DTOs.Response
{
    public class DeudaPendienteDto
    {
        public Guid PuestoId { get; set; }
        public string PuestoNumero { get; set; } = string.Empty;
        public string DuenoNombre { get; set; } = string.Empty;
        public decimal TotalPendiente { get; set; }
        public List<DeudaResponseDto> Deudas { get; set; } = new();
    }
}
