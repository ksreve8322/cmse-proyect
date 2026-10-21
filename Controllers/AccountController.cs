
using Microsoft.AspNetCore.Mvc;
using cmse.Dtos;
using cmse.Repositories;
using cmse.Repositories.Models;
using cmse.Utilities;

namespace cmse.Controllers
{
    public class AccountController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;
        private readonly PasswordService _passwordService;
        private readonly EmailManager _emailManager;
        private readonly NotificacionRepository _notificacionRepository;
        private readonly NotificacionEnvioRepository _notificacionEnvioRepository;

        public AccountController(
            UsuarioRepository usuarioRepository,
            PasswordService passwordService,
            EmailManager emailManager,
            NotificacionRepository notificacionRepository,
            NotificacionEnvioRepository notificacionEnvioRepository)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
            _emailManager = emailManager;
            _notificacionRepository = notificacionRepository;
            _notificacionEnvioRepository = notificacionEnvioRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.TiposDocumento =
                await _usuarioRepository.ObtenerTiposDocumentoAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistroDto modelo)
        {
            ViewBag.TiposDocumento =
                await _usuarioRepository.ObtenerTiposDocumentoAsync();

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            if (await _usuarioRepository.ExisteCorreoAsync(modelo.Correo))
            {
                ModelState.AddModelError(
                    "Correo",
                    "El correo ya está registrado.");

                return View(modelo);
            }

            if (await _usuarioRepository.ExisteNumeroDocumentoAsync(
                modelo.NumeroDocumento))
            {
                ModelState.AddModelError(
                    "NumeroDocumento",
                    "El número de documento ya está registrado.");

                return View(modelo);
            }

            var usuario = new Usuario
            {
                IdRol = 3,
                IdTipoDocumento = modelo.IdTipoDocumento,
                Nombres = modelo.Nombres,
                Apellidos = modelo.Apellidos,
                NumeroDocumento = modelo.NumeroDocumento,
                Correo = modelo.Correo,
                Telefono = modelo.Telefono,
                Contrasena = _passwordService.HashPassword(
                    modelo.Contrasena),
                Estado = true,
                IntentosFallidos = 0
            };

            // 1. Guardar el usuario.
            await _usuarioRepository.CrearAsync(usuario);

            // 2. Crear notificación interna.
            try
            {
                await _notificacionRepository.CrearAsync(
                    new Notificacion
                    {
                        IdUsuario = usuario.IdUsuario,
                        IdCanal = 2,
                        Titulo = "Registro exitoso",
                        Mensaje =
                            "Tu cuenta de CMSE se creó correctamente.",
                        FechaCreacion = DateTime.Now,
                        Leida = false,
                        Estado = true
                    });
            }
            catch (Exception)
            {
                // Un fallo de notificación no cancela el registro.
            }

            // 3. Crear notificación asociada al correo.
            int idNotificacionCorreo = 0;

            try
            {
                idNotificacionCorreo =
                    await _notificacionRepository.CrearAsync(
                        new Notificacion
                        {
                            IdUsuario = usuario.IdUsuario,
                            IdCanal = 3,
                            Titulo = "Correo de bienvenida",
                            Mensaje =
                                "Se intentará enviar el correo de bienvenida.",
                            FechaCreacion = DateTime.Now,
                            Leida = false,
                            Estado = true
                        });
            }
            catch (Exception)
            {
                // Se conserva el registro aunque falle esta notificación.
            }

            // 4. Enviar correo y registrar el resultado.
            try
            {
                await _emailManager.EnviarCorreoPlantillaAsync(
                    usuario.Correo,
                    "Bienvenido a CMSE",
                    "¡Tu registro se ha completado!",
                    usuario.Nombres,
                    "Tu cuenta ha sido creada correctamente. "
                    + "Ya puedes iniciar sesión en el sistema de Control "
                    + "y Mantenimiento de Sistemas y Equipos.",
                    "Contacta al área de soporte de CMSE.");

                if (idNotificacionCorreo > 0)
                {
                    await _notificacionEnvioRepository.RegistrarEnvioAsync(
                        idNotificacionCorreo,
                        usuario.Correo,
                        "enviado");
                }

                TempData["Mensaje"] =
                    "¡Registro exitoso! Te enviamos un correo de bienvenida.";
            }
            catch (Exception ex)
            {
                if (idNotificacionCorreo > 0)
                {
                    try
                    {
                        await _notificacionEnvioRepository.RegistrarEnvioAsync(
                            idNotificacionCorreo,
                            usuario.Correo,
                            "fallido",
                            ex.Message);
                    }
                    catch (Exception)
                    {
                        // No se pudo guardar el resultado del envío.
                    }
                }

                TempData["Mensaje"] =
                    "Tu cuenta fue creada correctamente, pero no pudimos "
                    + "enviar el correo de bienvenida. Puedes iniciar sesión.";
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuario =
                await _usuarioRepository.ObtenerPorCorreoAsync(modelo.Correo);

            if (usuario == null ||
                !_passwordService.VerifyPassword(
                    modelo.Contrasena,
                    usuario.Contrasena))
            {
                ModelState.AddModelError(
                    "",
                    "Correo o contraseña incorrectos.");

                return View(modelo);
            }

            // No permitir el acceso a cuentas desactivadas.
            if (!usuario.Estado)
            {
                ModelState.AddModelError(
                    "",
                    "Tu cuenta está desactivada. Contacta al administrador.");

                return View(modelo);
            }

            // Limpiar cualquier sesión anterior.
            HttpContext.Session.Clear();

            // Guardar únicamente los datos necesarios.
            HttpContext.Session.SetInt32(
                "UsuarioId",
                usuario.IdUsuario);

            HttpContext.Session.SetString(
                "UsuarioNombre",
                usuario.Nombres ?? string.Empty);

            HttpContext.Session.SetString(
                "UsuarioCorreo",
                usuario.Correo ?? string.Empty);

            HttpContext.Session.SetInt32(
                "RolId",
                usuario.IdRol);

            HttpContext.Session.SetString(
                "RolNombre",
                usuario.IdRolNavigation?.Nombre ?? string.Empty);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Cerrar la sesión y eliminar sus datos.
            HttpContext.Session.Clear();

            // Evitar que el navegador reutilice una página privada en caché.
            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction(nameof(Login));
        }
    }
}
