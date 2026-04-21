using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SGM.Core.Enums;


namespace SGM.Infrastructure.Data.Configuations
{
    public class PuestoConfiguration : IEntityTypeConfiguration<Puesto>
    {
        public void Configure(EntityTypeBuilder<Puesto> builder)
        {
            //Nombre de la tabla y schema
            builder.ToTable("Puestos", "SGM");
            //Clave primaria
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()");
            //Propiedades con restricciones
            builder.Property(p => p.Codigo)
                .IsRequired()
                .HasMaxLength(20);
            builder.Property(p => p.Descripcion)
                .HasMaxLength(300);
            builder.Property(p => p.Ubicacion)
                .HasMaxLength(100);
            builder.Property(P => P.Aream2)
                .HasColumnName("area_m2")
                .HasPrecision(8, 2);
            //Conversión del enum a string para PostgreSQL
            builder.Property(p => p.Estado)
                .HasConversion(
                    v => v.ToString().ToLower(), // Convertir enum a string para almacenar
                      v => Enum.Parse<EstadoPuesto>(v, true) // Convertir string a enum al leer
                )
                .HasMaxLength(20); // Asegurar que el campo tenga suficiente longitud para el valor del enum
            //Mapeo de nombres snake_case
            builder.Property(p => p.DuenoId).HasColumnName("dueno_id");
            builder.Property(p => p.UpdateAt).HasColumnName("updated_At");
            //Forign Key: Puesto -> Usuario (duenño)
            builder.HasOne(p => p.Dueno)
                .WithMany(u => u.Puestos)
                .HasForeignKey(p => p.DuenoId)
                .OnDelete(DeleteBehavior.SetNull);
            //Relación uno a muchos: Puesto -> Deudas
            builder.HasMany(p => p.Deudas)
                .WithOne(d => d.Puesto)
                .HasForeignKey(p => p.PuestoId)
                .OnDelete(DeleteBehavior.SetNull);
            //Indices
            builder.HasIndex(p => p.Codigo).IsUnique();
            builder.HasIndex(p => p.DuenoId);
            builder.HasIndex(p => p.Estado);
        }

    }
}
