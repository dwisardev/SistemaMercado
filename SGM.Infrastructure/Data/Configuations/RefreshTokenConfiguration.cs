using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;

namespace SGM.Infrastructure.Data.Configuations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens", "sgm");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("id");
            builder.Property(r => r.UsuarioId).HasColumnName("usuario_id").IsRequired();
            builder.Property(r => r.Token).HasColumnName("token").HasMaxLength(128).IsRequired();
            builder.Property(r => r.ExpiresAt).HasColumnName("expires_at").IsRequired();
            builder.Property(r => r.Revocado).HasColumnName("revocado").HasDefaultValue(false);
            builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasOne(r => r.Usuario)
                   .WithMany()
                   .HasForeignKey(r => r.UsuarioId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.Token).IsUnique();
            builder.HasIndex(r => r.UsuarioId);
        }
    }
}
