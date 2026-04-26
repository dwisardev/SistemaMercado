using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;
using SGM.Core.Entities;
using SGM.Core.Enums;
using SMG.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Infrastructure.Data
{
    public class AppDbContext : DbContext
    //Contructor : recibe la configuracion de conexión 
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

            public DbSet<Usuario> Usuarios { get; set; }
            public DbSet<Puesto> Puestos { get; set; }
            public DbSet<ConceptoCobro> ConceptoCobro { get; set; }
            public DbSet<Deuda> Deudas { get; set; }
            public DbSet<Pago> Pagos { get; set; }
            public DbSet<Configuracion> Configuations { get; set; }
            public DbSet<AuditLog> AuditLogs { get; set; }
            public DbSet<Notificacion> Notificaciones { get; set; }
            public DbSet<HistorialDueno> HistorialDuenos { get; set; }
            public DbSet<TarifaPuesto> TarifaPuestos { get;set; }
            public DbSet<RefreshToken>   RefreshTokens   { get; set; }
            public DbSet<TokenRevocado> TokensRevocados { get; set; }
       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var translator = new NpgsqlSnakeCaseNameTranslator();
            modelBuilder.HasPostgresEnum<EstadoPago>  ("sgm", "estado_pago",   translator);
            modelBuilder.HasPostgresEnum<MetodoPago>  ("sgm", "metodo_pago",   translator);
            modelBuilder.HasPostgresEnum<EstadoDeuda> ("sgm", "estado_deuda",  translator);
            modelBuilder.HasPostgresEnum<EstadoPuesto>("sgm", "estado_puesto", translator);
            modelBuilder.HasPostgresEnum<RolUsuario>  ("sgm", "rol_usuario",   translator);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.HasDefaultSchema("sgm");
        }
          
            





    }
}
        
    

