namespace SGM.API.DTOs.Response
{
    public class CargaMasivaResultDto
    {
        public int Total { get; set; }
        public int Exitosos { get; set; }
        public int Fallidos { get; set; }
        public List<string> Errores { get; set; } = new();
    }
}
