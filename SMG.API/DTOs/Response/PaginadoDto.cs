namespace SGM.API.DTOs.Response
{
    public class PaginadoDto<T>
    {
        public IEnumerable<T> Data      { get; set; } = [];
        public int            Total     { get; set; }
        public int            Page      { get; set; }
        public int            PageSize  { get; set; }
        public int            TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    }
}
