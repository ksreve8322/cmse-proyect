
using cmse.Models;
using cmse.Utilities;
using cmse.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

namespace cmse.Controllers
{
    public class HomeController : Controller
    {
        private readonly EmailManager _emailManager;
        private readonly IWebHostEnvironment _environment;
        private readonly UsuarioRepository _usuarioRepository;

        public HomeController(
            EmailManager emailManager,
            IWebHostEnvironment environment,
            UsuarioRepository usuarioRepository)
        {
            _emailManager = emailManager;
            _environment = environment;
            _usuarioRepository = usuarioRepository;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            int? idUsuario = HttpContext.Session.GetInt32("UsuarioId");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(idUsuario.Value);

            if (usuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return View(usuario);
        }
        [HttpGet]
        public async Task<IActionResult> ProbarCorreo()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            int? idUsuario = HttpContext.Session.GetInt32("UsuarioId");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string? destinatario =
                HttpContext.Session.GetString("UsuarioCorreo");

            string nombre =
                HttpContext.Session.GetString("UsuarioNombre")
                ?? "Usuario CMSE";

            if (string.IsNullOrWhiteSpace(destinatario))
            {
                return BadRequest(
                    "La sesión actual no tiene un correo electrónico.");
            }

            await _emailManager.EnviarCorreoPlantillaAsync(
                destinatario,
                "Prueba de plantilla - CMSE",
                "Prueba de notificaciones",
                nombre,
                "Este es un correo de prueba de la plantilla HTML "
                + "de CMSE. Estamos comprobando el logo, los colores "
                + "y el formato.",
                "Contacta al área de soporte de CMSE.");

            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return Content(
                "Correo enviado. Revisa tu bandeja de entrada.",
                "text/plain; charset=utf-8");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
