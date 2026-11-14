
using cmse.Repositories;
using cmse.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cmse.Controllers
{
    public class PerfilController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;
        private readonly PasswordService _passwordService;

        public PerfilController(
            UsuarioRepository usuarioRepository,
            PasswordService passwordService)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("UsuarioId");

            if (idUsuario == null)
                return RedirectToAction("Login", "Account");

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(idUsuario.Value);

            if (usuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCuenta(string contrasenaActual)
        {
            int? idUsuario = HttpContext.Session.GetInt32("UsuarioId");

            if (idUsuario == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(contrasenaActual))
            {
                TempData["MensajeError"] = "Debes ingresar tu contraseña actual.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(idUsuario.Value);

            if (usuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            if (!_passwordService.VerifyPassword(contrasenaActual, usuario.Contrasena))
            {
                TempData["MensajeError"] = "La contraseña ingresada no es correcta.";
                return RedirectToAction(nameof(Index));
            }

            if (usuario.IdRol == 1 &&
                !await _usuarioRepository.ExisteOtroAdministradorActivoAsync(idUsuario.Value))
            {
                TempData["MensajeError"] =
                    "No puedes eliminar tu cuenta porque eres el último administrador activo del sistema.";

                return RedirectToAction(nameof(Index));
            }

            if (await _usuarioRepository.TieneRegistrosOperativosAsync(idUsuario.Value))
            {
                TempData["MensajeError"] =
                    "No es posible eliminar tu cuenta porque tienes registros operativos asociados. Esto permite conservar el historial del sistema.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _usuarioRepository.EliminarCuentaAsync(idUsuario.Value);

                HttpContext.Session.Clear();

                TempData["MensajeExito"] = "Tu cuenta ha sido eliminada correctamente.";

                return RedirectToAction("Login", "Account");
            }
            catch (DbUpdateException)
            {
                TempData["MensajeError"] =
                    "No fue posible eliminar la cuenta porque existen otros registros relacionados. No se realizaron cambios.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException)
            {
                TempData["MensajeError"] =
                    "No fue posible completar la eliminación porque la cuenta ya no está disponible.";

                return RedirectToAction(nameof(Index));
            }
        }
    }
}
