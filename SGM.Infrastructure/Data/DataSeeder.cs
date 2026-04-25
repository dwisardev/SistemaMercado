using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SGM.Infrastructure.Data
{
    public static class DataSeeder
    {
        // Contraseña unificada para todos los usuarios de prueba
        private const string DefaultPassword = "admin123";

        private static readonly Dictionary<string, string> _seedPasswords = new()
        {
            ["admin@mercado.com"]    = DefaultPassword,
            ["cajero1@mercado.com"]  = DefaultPassword,
            ["cajera2@mercado.com"]  = DefaultPassword,
        };

        public static async Task SeedPasswordsAsync(AppDbContext db, ILogger logger)
        {
            var users = await db.Usuarios
                .Where(u => u.PasswordHash == "")
                .ToListAsync();

            if (users.Count == 0) return;

            logger.LogInformation("DataSeeder: seteando contraseñas para {Count} usuarios...", users.Count);

            foreach (var user in users)
            {
                if (_seedPasswords.TryGetValue(user.Email, out var password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }
                else
                {
                    // Contraseña genérica para usuarios sin contraseña asignada
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cambiar123!");
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("DataSeeder: contraseñas configuradas.");
        }
    }
}
