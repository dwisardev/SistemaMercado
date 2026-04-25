using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SGM.Core.Enums;

namespace SGM.Infrastructure.Data.Configuations
{
    public class DeudaConfiguration : IEntityTypeConfiguration<Deuda>
    {
        public void Configure(EntityTypeBuilder<Deuda> builder)
        {
            builder.ToTable("deudas", "sgm");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(d => d.PuestoId)
                .HasColumnName("puesto_id");
            builder.Property(d => d.ConceptoId)
                .HasColumnName("concepto_id");
            builder.Property(d => d.Monto)
                .HasColumnName("monto")
                .HasPrecision(10, 2);
            builder.Property(d => d.Periodo)
                .IsRequired().HasMaxLength(7)
                .HasColumnName("periodo");
            builder.Property(d => d.FechaEmision)
                .HasColumnName("fecha_emision");
            builder.Property(d => d.FechaVencimiento)
                .HasColumnName("fecha_vencimiento");
            builder.Property(d => d.LoteCargaId)
                .HasColumnName("lote_carga_id");
            builder.Property(d => d.GeneradoPor)
                .HasColumnName("generado_por");
            builder.Property(d => d.CreatedAt)
                .HasColumnName("created_at");
            builder.Property(d => d.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(d => d.Estado)
                .HasColumnName("estado");

            builder.HasOne(d => d.Puesto)
                .WithMany(p => p.Deudas)
                .HasForeignKey(d => d.PuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Concepto)
                .WithMany(cc => cc.Deudas)
                .HasForeignKey(d => d.ConceptoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.PuestoId);
            builder.HasIndex(d => d.ConceptoId);
            builder.HasIndex(d => d.Estado);
            builder.HasIndex(d => d.Periodo);
            builder.HasIndex(d => new { d.PuestoId, d.ConceptoId, d.Periodo }).IsUnique();
        }
    }
}
