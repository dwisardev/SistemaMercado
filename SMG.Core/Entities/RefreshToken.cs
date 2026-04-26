namespace SGM.Core.Entities
{
    public class RefreshToken
    {
        public Guid     Id         { get; set; } = Guid.NewGuid();
        public Guid     UsuarioId  { get; set; }
        public string   Token      { get; set; } = string.Empty;
        public DateTime ExpiresAt  { get; set; }
        public bool     Revocado   { get; set; }
        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
        public Usuario? Usuario    { get; set; }
    }
}
