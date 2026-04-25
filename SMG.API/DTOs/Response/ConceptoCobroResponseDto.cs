namespace SGM.API.DTOs.Response
{
    public class ConceptoCobroResponseDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Monto { get; set; }
        public bool Activo { get; set; }
    }
}
