
using cmse.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace cmse.Controllers
{
    public class NotificacionController : Controller
    {
        private readonly NotificacionRepository _notificacionRepository;

        public NotificacionController(
            NotificacionRepository notificacionRepository)
        {
            _notificacionRepository = notificacionRepository;
        }

        private int? ObtenerIdUsuarioSesion()
        {
            return HttpContext.Session.GetInt32("UsuarioId");
        }

        public async Task<IActionResult> Index()
        {
            int? idUsuario = ObtenerIdUsuarioSesion();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var notificaciones =
                await _notificacionRepository.ObtenerPorUsuarioAsync(
                    idUsuario.Value);

            return View(notificaciones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            int? idUsuario = ObtenerIdUsuarioSesion();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _notificacionRepository.MarcarComoLeidaAsync(
                id,
                idUsuario.Value);

            return RedirectToAction(nameof(Index));
        }
    }
}
