using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGM.Core.Entities;

namespace SGM.Infrastructure.Data.Configuations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs", "sgm");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.Accion).HasColumnName("accion").HasMaxLength(50).IsRequired();
            builder.Property(a => a.TablaAfectada).HasColumnName("tabla_afectada").HasMaxLength(50);
            builder.Property(a => a.RegistroId).HasColumnName("registro_id");
            builder.Property(a => a.UsuarioId).HasColumnName("usuario_id");
            builder.Property(a => a.UsuarioNombre).HasColumnName("usuario_nombre").HasMaxLength(200);
            builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            builder.Property(a => a.Detalle).HasColumnName("detalle").HasColumnType("jsonb");
            builder.Property(a => a.DatosAnteriores).HasColumnName("datos_anteriores").HasColumnType("jsonb");
            builder.Property(a => a.DatosNuevos).HasColumnName("datos_nuevos").HasColumnType("jsonb");
            builder.Property(a => a.Timestamp).HasColumnName("timestamp").IsRequired();
        }
    }
}
