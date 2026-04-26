using SGM.Core.Entities;
using SMG.Core.Repositories;
using System.Security.Claims;

namespace SGM.API.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HashSet<string> _metodos = ["POST", "PATCH", "DELETE", "PUT"];

        public AuditMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, IAuditLogRepository repo)
        {
            await _next(ctx);

            if (!_metodos.Contains(ctx.Request.Method)) return;
            if (ctx.Response.StatusCode < 200 || ctx.Response.StatusCode >= 300) return;

            try
            {
                var path = ctx.Request.Path.Value ?? "";
                var segments = path.Trim('/').Split('/');
                var tabla = segments.Length >= 2 ? segments[1] : null;
                var ua = ctx.Request.Headers.UserAgent.ToString();

                await repo.TaskAsync(new AuditLog
                {
                    Accion        = $"{ctx.Request.Method} {path}",
                    TablaAfectada = tabla,
                    UsuarioId     = Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
                                        ? uid : null,
                    UsuarioNombre = ctx.User.FindFirstValue(ClaimTypes.Name),
                    IpAddress     = ctx.Connection.RemoteIpAddress?.ToString(),
                    UserAgent     = ua.Length > 500 ? ua[..500] : ua,
                    Timestamp     = DateTime.UtcNow,
                });
            }
            catch { /* audit failure must not break the request */ }
        }
    }
}
