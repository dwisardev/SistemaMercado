using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Infrastructure.Data.Configuations
{
    public class ConceptoCobroConfiguration : IEntityTypeConfiguration<ConceptoCobro>
    {
        public void Configure(EntityTypeBuilder<ConceptoCobro> builder)
        {
            builder.HasKey(cc => cc.Id);
            builder.Property(cc => cc.Id)
                .HasDefaultValueSql("gen_ramdom_uui()");
            builder.Property(cc => cc.Nombre)
                .IsRequired().HasMaxLength(100);
            builder.Property(cc => cc.Descripcion)
                .HasMaxLength(300);
            builder.Property(cc => cc.MontoDefault)
                .HasColumnName("monto_default")
                .HasPrecision(10, 2);
            builder.Property(cc => cc.EsRecurrente)
                .HasColumnName("es_recurrente");
            builder.Property(cc => cc.Metadata)
                .HasColumnType("jsonb");
            builder.Property(cc => cc.CreatedAt).HasColumnName
                ("created_at");
            builder.Property(cc => cc.UpdatedAt).HasColumnName
                ("updated_at");
            builder.HasIndex(CC=> CC.Nombre).IsUnique();



        }
    }
 

}

