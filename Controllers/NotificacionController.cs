
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

        public async Task<IActionResult> Index(int pagina = 1)
        {
            int? idUsuario = ObtenerIdUsuarioSesion();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int registrosPorPagina = 10;

            var resultado =
                await _notificacionRepository.ObtenerPorUsuarioPaginadasAsync(
                    idUsuario.Value,
                    pagina,
                    registrosPorPagina);

            int totalPaginas = (int)Math.Ceiling(
                resultado.TotalRegistros / (double)registrosPorPagina);

            pagina = Math.Max(1, Math.Min(pagina, Math.Max(1, totalPaginas)));

            if (pagina != 1)
            {
                resultado =
                    await _notificacionRepository.ObtenerPorUsuarioPaginadasAsync(
                        idUsuario.Value,
                        pagina,
                        registrosPorPagina);
            }

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = resultado.TotalRegistros;

            return View(resultado.Notificaciones);
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
