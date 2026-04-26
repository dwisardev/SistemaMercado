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
            builder.ToTable("puestos", "sgm");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(p => p.Codigo)
                .IsRequired().HasMaxLength(20)
                .HasColumnName("codigo");
            builder.Property(p => p.Descripcion)
                .HasMaxLength(300)
                .HasColumnName("descripcion");
            builder.Property(p => p.Ubicacion)
                .HasMaxLength(100)
                .HasColumnName("ubicacion");
            builder.Property(p => p.Aream2)
                .HasColumnName("area_m2")
                .HasPrecision(8, 2);
            builder.Property(p => p.DuenoId)
                .HasColumnName("dueno_id");
            builder.Property(p => p.FechaAsignacion)
                .HasColumnName("fecha_asignacion");
            builder.Property(p => p.CreateAt)
                .HasColumnName("created_at");
            builder.Property(p => p.UpdateAt)
                .HasColumnName("updated_at");

            builder.Property(p => p.Estado)
                .HasColumnName("estado")
                .HasConversion(
                    v => v == EstadoPuesto.EnMantenimiento ? "en_mantenimiento" : v.ToString().ToLower(),
                    v => v == "en_mantenimiento" ? EstadoPuesto.EnMantenimiento : Enum.Parse<EstadoPuesto>(v, true));

            builder.HasOne(p => p.Dueno)
                .WithMany(u => u.Puestos)
                .HasForeignKey(p => p.DuenoId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(p => p.Deudas)
                .WithOne(d => d.Puesto)
                .HasForeignKey(d => d.PuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.Codigo).IsUnique();
            builder.HasIndex(p => p.DuenoId);
            builder.HasIndex(p => p.Estado);
        }
    }
}
