namespace SGM.API.DTOs.Response
{
    public class CajaDiariaDto
    {
        public string Fecha { get; set; } = string.Empty;
        public decimal TotalRecaudado { get; set; }
        public int CantidadPagos { get; set; }
        public List<PagoResponseDto> Pagos { get; set; } = new();
    }
}
