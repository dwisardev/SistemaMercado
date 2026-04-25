using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SMG.Core.Enums;

namespace SGM.Infrastructure.Data.Configuations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuarios", "sgm");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(u => u.NombreCompleto)
                .IsRequired().HasMaxLength(200)
                .HasColumnName("nombre_completo");
            builder.Property(u => u.Email)
                .IsRequired().HasMaxLength(150)
                .HasColumnName("email");
            builder.Property(u => u.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
            builder.Property(u => u.Dni)
                .IsRequired().HasMaxLength(15)
                .HasColumnName("dni");
            builder.Property(u => u.PasswordHash)
                .IsRequired().HasMaxLength(200)
                .HasColumnName("password_hash");
            builder.Property(u => u.Activo)
                .HasColumnName("activo");
            builder.Property(u => u.AuthUserId)
                .HasColumnName("auth_user_id");
            builder.Property(u => u.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");
            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at");
            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(u => u.Rol)
                .HasColumnName("rol");

            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Dni).IsUnique();
            builder.HasIndex(u => u.Rol);
        }
    }
}
