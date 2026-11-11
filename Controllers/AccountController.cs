
using System.Security.Cryptography;
using System.Text.Json;
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

        private const string RegistroPendienteKey = "RegistroPendiente";
        private const string CodigoHashKey = "CodigoVerificacionHash";
        private const string CodigoExpiraKey = "CodigoVerificacionExpira";
        private const string IntentosKey = "CodigoVerificacionIntentos";

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

        // Paso 1: validar los datos y enviar el código.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistroDto modelo)
        {
            ViewBag.TiposDocumento =
                await _usuarioRepository.ObtenerTiposDocumentoAsync();

            if (!ModelState.IsValid)
                return View(modelo);

            if (await _usuarioRepository.ExisteCorreoAsync(modelo.Correo))
            {
                ModelState.AddModelError(
                    "Correo", "El correo ya está registrado.");
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

            // Guardar temporalmente los datos, con la contraseña protegida.
            var pendiente = new RegistroPendiente
            {
                IdRol = 3,
                IdTipoDocumento = modelo.IdTipoDocumento,
                Nombres = modelo.Nombres,
                Apellidos = modelo.Apellidos,
                NumeroDocumento = modelo.NumeroDocumento,
                Correo = modelo.Correo,
                Telefono = modelo.Telefono,
                ContrasenaHash =
                    _passwordService.HashPassword(modelo.Contrasena)
            };

            // Código aleatorio de seis dígitos.
            string codigo = RandomNumberGenerator
                .GetInt32(100000, 1000000).ToString();

            HttpContext.Session.SetString(
                RegistroPendienteKey,
                JsonSerializer.Serialize(pendiente));

            HttpContext.Session.SetString(
                CodigoHashKey, CalcularHashCodigo(codigo));

            HttpContext.Session.SetString(
                CodigoExpiraKey,
                DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
                    .ToString());

            HttpContext.Session.SetInt32(IntentosKey, 0);

            try
            {
                await _emailManager.EnviarCorreoPlantillaAsync(
                    pendiente.Correo,
                    "Código de verificación - CMSE",
                    "Verifica tu correo electrónico",
                    pendiente.Nombres,
                    $"Tu código de verificación es: <strong>{codigo}</strong>. " +
                    "Introdúcelo en la pantalla de verificación para completar " +
                    "tu registro. Este código vence en 10 minutos.",
                    "Si no solicitaste este registro, puedes ignorar este correo.");

                TempData["Mensaje"] =
                    "Te enviamos un código de verificación. Revisa tu correo.";

                return RedirectToAction(nameof(VerifyEmail));
            }
            catch (Exception)
            {
                LimpiarRegistroPendiente();

                ModelState.AddModelError(
                    "",
                    "No pudimos enviar el código. Verifica los datos e inténtalo de nuevo.");

                return View(modelo);
            }
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            if (HttpContext.Session.GetString(RegistroPendienteKey) == null)
                return RedirectToAction(nameof(Register));

            return View();
        }

        // Paso 2: validar el código y solo entonces crear la cuenta.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string codigo)
        {
            var sesion = HttpContext.Session;

            string? pendienteJson =
                sesion.GetString(RegistroPendienteKey);

            string? codigoGuardado =
                sesion.GetString(CodigoHashKey);

            string? expiraTexto =
                sesion.GetString(CodigoExpiraKey);

            if (pendienteJson == null ||
                codigoGuardado == null ||
                expiraTexto == null)
            {
                LimpiarRegistroPendiente();
                TempData["Mensaje"] =
                    "El registro ha caducado. Inténtalo nuevamente.";

                return RedirectToAction(nameof(Register));
            }

            if (!long.TryParse(expiraTexto, out long expira) ||
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expira)
            {
                LimpiarRegistroPendiente();
                TempData["Mensaje"] =
                    "El código ha caducado. Regístrate nuevamente.";

                return RedirectToAction(nameof(Register));
            }

            int intentos = sesion.GetInt32(IntentosKey) ?? 0;

            if (intentos >= 5)
            {
                LimpiarRegistroPendiente();
                TempData["Mensaje"] =
                    "Superaste los intentos permitidos. Inicia el registro nuevamente.";

                return RedirectToAction(nameof(Register));
            }

            if (string.IsNullOrWhiteSpace(codigo) ||
                codigo.Length != 6 ||
                codigo.Any(c => c < '0' || c > '9') ||
                !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(codigoGuardado),
                    Convert.FromHexString(CalcularHashCodigo(codigo))))
            {
                intentos++;
                sesion.SetInt32(IntentosKey, intentos);

                if (intentos >= 5)
                {
                    LimpiarRegistroPendiente();
                    TempData["Mensaje"] =
                        "Superaste los intentos permitidos. Inicia el registro nuevamente.";

                    return RedirectToAction(nameof(Register));
                }

                ViewBag.Error =
                    $"El código es incorrecto. Intentos restantes: {5 - intentos}.";

                return View();
            }

            RegistroPendiente? datos;

            try
            {
                datos = JsonSerializer.Deserialize<RegistroPendiente>(
                    pendienteJson);
            }
            catch (JsonException)
            {
                LimpiarRegistroPendiente();
                return RedirectToAction(nameof(Register));
            }

            if (datos == null)
            {
                LimpiarRegistroPendiente();
                return RedirectToAction(nameof(Register));
            }

            // Revalidar por si los datos cambiaron durante la verificación.
            if (await _usuarioRepository.ExisteCorreoAsync(datos.Correo) ||
                await _usuarioRepository.ExisteNumeroDocumentoAsync(
                    datos.NumeroDocumento))
            {
                LimpiarRegistroPendiente();

                TempData["Mensaje"] =
                    "El correo o el documento ya está registrado. Comprueba los datos.";

                return RedirectToAction(nameof(Register));
            }

            var usuario = new Usuario
            {
                IdRol = datos.IdRol,
                IdTipoDocumento = datos.IdTipoDocumento,
                Nombres = datos.Nombres,
                Apellidos = datos.Apellidos,
                NumeroDocumento = datos.NumeroDocumento,
                Correo = datos.Correo,
                Telefono = datos.Telefono,
                Contrasena = datos.ContrasenaHash,
                Estado = true,
                IntentosFallidos = 0
            };

            await _usuarioRepository.CrearAsync(usuario);

            // El código ya fue validado y la cuenta ya existe.
            LimpiarRegistroPendiente();

            try
            {
                await _notificacionRepository.CrearAsync(
                    new Notificacion
                    {
                        IdUsuario = usuario.IdUsuario,
                        IdCanal = 2,
                        Titulo = "Registro exitoso",
                        Mensaje = "Tu correo fue verificado y tu cuenta de CMSE se creó correctamente.",
                        FechaCreacion = DateTime.Now,
                        Leida = false,
                        Estado = true
                    });
            }
            catch (Exception)
            {
                // La notificación no debe invalidar la cuenta creada.
            }

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
                            Mensaje = "Se intentará enviar el correo de bienvenida.",
                            FechaCreacion = DateTime.Now,
                            Leida = false,
                            Estado = true
                        });
            }
            catch (Exception)
            {
                // Se conserva la cuenta aunque falle la notificación.
            }

            try
            {
                await _emailManager.EnviarCorreoPlantillaAsync(
                    usuario.Correo,
                    "Bienvenido a CMSE",
                    "¡Tu correo ha sido verificado!",
                    usuario.Nombres,
                    "Tu cuenta se creó correctamente y tu correo fue verificado. " +
                    "Ya puedes iniciar sesión en CMSE.",
                    "Contacta al área de soporte de CMSE.");

                if (idNotificacionCorreo > 0)
                {
                    await _notificacionEnvioRepository.RegistrarEnvioAsync(
                        idNotificacionCorreo,
                        usuario.Correo,
                        "enviado");
                }

                TempData["Mensaje"] =
                    "Correo verificado y cuenta creada correctamente. Ya puedes iniciar sesión.";
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
                        // No se pudo registrar el intento de envío.
                    }
                }

                TempData["Mensaje"] =
                    "Correo verificado y cuenta creada. No pudimos enviar el correo de bienvenida.";
            }

            return RedirectToAction(nameof(Login));
        }

        private static string CalcularHashCodigo(string codigo)
        {
            byte[] hash = SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(codigo));

            return Convert.ToHexString(hash);
        }

        private void LimpiarRegistroPendiente()
        {
            HttpContext.Session.Remove(RegistroPendienteKey);
            HttpContext.Session.Remove(CodigoHashKey);
            HttpContext.Session.Remove(CodigoExpiraKey);
            HttpContext.Session.Remove(IntentosKey);
        }

        private sealed class RegistroPendiente
        {
            public int IdRol { get; set; }
            public int IdTipoDocumento { get; set; }
            public string Nombres { get; set; } = "";
            public string Apellidos { get; set; } = "";
            public string NumeroDocumento { get; set; } = "";
            public string Correo { get; set; } = "";
            public string Telefono { get; set; } = "";
            public string ContrasenaHash { get; set; } = "";
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
                return View(modelo);

            var usuario =
                await _usuarioRepository.ObtenerPorCorreoAsync(modelo.Correo);

            if (usuario == null ||
                !_passwordService.VerifyPassword(
                    modelo.Contrasena, usuario.Contrasena))
            {
                ModelState.AddModelError(
                    "", "Correo o contraseña incorrectos.");

                return View(modelo);
            }

            if (!usuario.Estado)
            {
                ModelState.AddModelError(
                    "", "Tu cuenta está desactivada. Contacta al administrador.");

                return View(modelo);
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UsuarioId", usuario.IdUsuario);
            HttpContext.Session.SetString(
                "UsuarioNombre", usuario.Nombres ?? "");
            HttpContext.Session.SetString(
                "UsuarioCorreo", usuario.Correo ?? "");
            HttpContext.Session.SetInt32("RolId", usuario.IdRol);
            HttpContext.Session.SetString(
                "RolNombre", usuario.IdRolNavigation?.Nombre ?? "");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction(nameof(Login));
        }
    }
}
