using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGM.API.DTOs.Response;
using SGM.Core.Interfaces.Services;
using System.Security.Claims;

namespace SGM.API.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _service;
        public NotificacionesController(INotificacionService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificacionResponseDto>>> GetAll()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var notifs = await _service.GetMisNotificacionesAsync(userId);
            return Ok(notifs.Select(n => new NotificacionResponseDto
            {
                Id = n.Id,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                Leida = n.Leida,
                FechaCreacion = n.CreatedAt,
                Tipo = n.Tipo,
            }));
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> MarcarLeida(Guid id)
        {
            await _service.MarcarLeidaAsync(id);
            return NoContent();
        }

        [HttpPatch("todas-leidas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _service.MarcarTodasLeidasAsync(userId);
            return NoContent();
        }
    }
}
