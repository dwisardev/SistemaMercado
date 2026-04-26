using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;

namespace SGM.Infrastructure.Data.Configuations
{
    public class TokenRevocadoConfiguration : IEntityTypeConfiguration<TokenRevocado>
    {
        public void Configure(EntityTypeBuilder<TokenRevocado> builder)
        {
            builder.ToTable("tokens_revocados", "sgm");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id");
            builder.Property(t => t.Jti).HasColumnName("jti").HasMaxLength(36).IsRequired();
            builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
            builder.Property(t => t.RevokedAt).HasColumnName("revoked_at").IsRequired();
            builder.HasIndex(t => t.Jti).IsUnique();
        }
    }
}
