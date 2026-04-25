namespace SGM.API.DTOs.Request
{
    public class CreateConceptoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Monto { get; set; }
        public int? DiaEmision { get; set; }
    }

    public class UpdateConceptoDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Monto { get; set; }
        public int? DiaEmision { get; set; }
        public bool? Activo { get; set; }
    }
}
