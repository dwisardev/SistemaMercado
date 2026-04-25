using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;

namespace SGM.Infrastructure.Data.Configuations
{
    public class ConceptoCobroConfiguration : IEntityTypeConfiguration<ConceptoCobro>
    {
        public void Configure(EntityTypeBuilder<ConceptoCobro> builder)
        {
            builder.ToTable("conceptos_cobro", "sgm");
            builder.HasKey(cc => cc.Id);
            builder.Property(cc => cc.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(cc => cc.Nombre)
                .IsRequired().HasMaxLength(100)
                .HasColumnName("nombre");
            builder.Property(cc => cc.Descripcion)
                .HasMaxLength(300)
                .HasColumnName("descripcion");
            builder.Property(cc => cc.MontoDefault)
                .HasColumnName("monto_default")
                .HasPrecision(10, 2);
            builder.Property(cc => cc.EsRecurrente)
                .HasColumnName("es_recurrente");
            builder.Property(cc => cc.Activo)
                .HasColumnName("activo");
            builder.Property(cc => cc.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");
            builder.Property(cc => cc.CreatedAt)
                .HasColumnName("created_at");
            builder.Property(cc => cc.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasIndex(cc => cc.Nombre).IsUnique();
        }
    }
}
