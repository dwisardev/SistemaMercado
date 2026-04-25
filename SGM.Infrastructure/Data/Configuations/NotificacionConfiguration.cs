using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;

namespace SGM.Infrastructure.Data.Configuations
{
    public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
    {
        public void Configure(EntityTypeBuilder<Notificacion> builder)
        {
            builder.ToTable("notificaciones", "sgm");

            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(n => n.Tipo).HasColumnName("tipo").IsRequired().HasMaxLength(30);
            builder.Property(n => n.DestinatarioId).HasColumnName("destinatario_id");
            builder.Property(n => n.Titulo).HasColumnName("titulo").IsRequired().HasMaxLength(200);
            builder.Property(n => n.Mensaje).HasColumnName("mensaje").IsRequired();
            builder.Property(n => n.Canal).HasColumnName("canal").HasMaxLength(20);
            builder.Property(n => n.Leida).HasColumnName("leida");
            builder.Property(n => n.Datos).HasColumnName("datos").HasColumnType("jsonb");
            builder.Property(n => n.EnviadaAt).HasColumnName("enviada_at");
            builder.Property(n => n.CreatedAt).HasColumnName("created_at");

            builder.HasOne(n => n.Destinatario)
                .WithMany()
                .HasForeignKey(n => n.DestinatarioId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(n => new { n.DestinatarioId, n.Leida });
        }
    }
}
