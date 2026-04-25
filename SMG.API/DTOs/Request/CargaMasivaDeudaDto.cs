namespace SGM.API.DTOs.Request
{
    public class CargaMasivaDeudaDto
    {
        public Guid ConceptoId { get; set; }
        public decimal Monto { get; set; }
        public string FechaVencimiento { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
    }
}
