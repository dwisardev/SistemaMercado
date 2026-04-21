using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SGM.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Infrastructure.Data.Configuations
{
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            builder.ToTable("Pagos", "sgm");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(p => p.DeudaId).HasColumnName
                ("DeudaId");
            builder.Property(p => p.MontoPagado)
                .HasColumnName("MontoPagado")
                .HasPrecision(10, 2);
            builder.Property(p => p.FechaPago)
                .HasColumnName("fecha_pago");
            builder.Property(p => p.CajeroId)
                .HasColumnName("cajero_id");
            builder.Property(p => p.NumeroComprobante)
                .HasColumnName("nro_comprobante").HasMaxLength(30);
            //Enums
            builder.Property(p => p.Metodo)
                .HasColumnName("metodo_pago")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<MetodoPago>(v, true)
                );
            builder.Property(p => p.Estado)
                .HasColumnName("metodo_pago")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<EstadoPago>(v, true)
                );
            builder.Property(p => p.ReferenciaPago)
                .HasColumnName("referencia_pago")
                .HasMaxLength(100);
                
            builder.Property(p => p.Metodo)
                .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<EstadoPago>(v, true));
        }
    }

}

