using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SGM.Core.Enums;

namespace SGM.Infrastructure.Data.Configuations
{
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            builder.ToTable("pagos", "sgm");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(p => p.DeudaId).HasColumnName("deuda_id");
            builder.Property(p => p.MontoPagado).HasColumnName("monto_pagado").HasPrecision(10, 2);
            builder.Property(p => p.FechaPago).HasColumnName("fecha_pago");
            builder.Property(p => p.CajeroId).HasColumnName("cajero_id");
            builder.Property(p => p.NumeroComprobante).HasColumnName("nro_comprobante").HasMaxLength(30);
            builder.Property(p => p.ReferenciaPago).HasColumnName("referencia_pago").HasMaxLength(100);
            builder.Property(p => p.Observaciones).HasColumnName("observaciones");
            builder.Property(p => p.MotivoAnulacion).HasColumnName("motivo_anulacion").HasMaxLength(300);
            builder.Property(p => p.AnuladoPor).HasColumnName("anulado_por");
            builder.Property(p => p.FechaAnulacion).HasColumnName("fecha_anulacion");
            builder.Property(p => p.ComprobanteUrl).HasColumnName("comprobante_url").HasMaxLength(500);
            builder.Property(p => p.CreatedAt).HasColumnName("created_at");
            builder.Property(p => p.UpdataAt).HasColumnName("updated_at");

            builder.Property(p => p.Metodo)
                .HasColumnName("metodo_pago")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<MetodoPago>(v, true));

            builder.Property(p => p.Estado)
                .HasColumnName("estado")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<EstadoPago>(v, true));

            builder.HasOne(p => p.Cajero)
                .WithMany(u => u.PagosRegistrados)
                .HasForeignKey(p => p.CajeroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Deuda)
                .WithOne(d => d.Pago)
                .HasForeignKey<Pago>(p => p.DeudaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.DeudaId).IsUnique();
            builder.HasIndex(p => p.CajeroId);
            builder.HasIndex(p => p.FechaPago);
        }
    }
}
