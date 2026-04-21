using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using SGM.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Infrastructure.Data.Configuations
{
    public class DeudaConfiguration : IEntityTypeConfiguration<Deuda>
    {
        public void Configure(EntityTypeBuilder<Deuda> builder)
        {
            builder.ToTable("Deudas", "sgm");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id)
                .HasDefaultValueSql("gen_ramdom_uuid()");
            builder.Property(d => d.PuestoId).HasColumnName("puesto_id");
            builder.Property(d => d.Concepto).HasColumnName("concepto_id");

            builder.Property(d => d.Monto).HasPrecision(10, 2);
            builder.Property(d => d.Periodo).IsRequired().HasMaxLength(7);

            builder.Property(d => d.FechaEmision).HasColumnName("fecha_emision");
            builder.Property(d => d.FechaVencimiento)
                .HasColumnName("fecha_vencimiento");
            //Enum a string 
            builder.Property(d => d.Estado)
                .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<EstadoDeuda>(v, true))
                .HasMaxLength(20);
            builder.Property(d => d.LoteCargaId)
                .HasColumnName("lote_carga_id");
            builder.Property(d => d.GeneradoPor)
                .HasColumnName("generado_por");
            builder.Property(d => d.CreatedAt).HasColumnName("created_at");
            builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");

            //Fk= deuda tiene una relacion con puesto
            builder.HasOne(d => d.Puesto)
                .WithMany(p => p.Deudas)
                .HasForeignKey(d => d.PuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            //Fk= deuda tiene una relacion con conceptoCobro
            builder.HasOne(d => d.Concepto)
                .WithMany(cc => cc.Deudas)
                .HasForeignKey(d => d.ConceptoId)
                .OnDelete(DeleteBehavior.Restrict);
            //indices
            builder.HasIndex(d=>d.PuestoId);
            builder.HasIndex(d=>d.ConceptoId);
            builder.HasIndex(d=>d.Estado);
            builder.HasIndex(d=>d.Periodo);
            //Indice compuesto para consultas frecuentes por puesto, estado y periodo
            builder.HasIndex(d => new { d.PuestoId, d.ConceptoId, d.Periodo }).IsUnique();



        }
    }
}
