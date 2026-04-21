using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SMG.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Infrastructure.Data.Configuations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>

    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuarios", "sgm");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasDefaultValueSql("gen_ramdom_uuid()");
            builder.Property(u => u.NombreCompleto)
                .IsRequired().HasMaxLength(200)
                .HasColumnName("nombre_completo");
            builder.Property(u => u.Email)
                .IsRequired().HasMaxLength(150);
            builder.Property(u => u.Telefono).HasMaxLength(20);

            builder.Property(u => u.Dni)
                .IsRequired().HasMaxLength(15);
            builder.Property(u => u.PasswordHash)
                .IsRequired().HasMaxLength(200).
                HasColumnName("password_hash");
            //Enum a string 
            builder.Property(u => u.Rol)
                .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<RolUsuario>(v, true))
                .HasMaxLength(20);
            builder.Property(u => u.AuthUserId)
                .HasColumnName("auth_user_id");
            // JSONB
            builder.Property(u => u.Metadata)
                .HasColumnName("jsonb");
            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at");
            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at");
            // Indices
            builder.HasIndex(u=>u.Email).IsUnique();
            builder.HasIndex(u => u.Dni).IsUnique();
            builder.HasIndex(u => u.Rol);




        }
    }
}
