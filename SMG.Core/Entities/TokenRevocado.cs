namespace SGM.Core.Entities
{
    public class TokenRevocado
    {
        public Guid     Id        { get; set; } = Guid.NewGuid();
        public string   Jti       { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    }
}
