
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
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

        // =====================================================
        // MÉTODO AUXILIAR PARA MENSAJES
        // =====================================================

        private void EstablecerMensaje(
            string mensaje,
            string tipo,
            string titulo)
        {
            TempData["Mensaje"] = mensaje;
            TempData["TipoMensaje"] = tipo;
            TempData["TituloModal"] = titulo;
        }

        // =====================================================
        // REGISTRO - CU-02
        // =====================================================

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
                    "Introdúcelo en la pantalla de verificación para completar tu registro.",
                    "Si no solicitaste este registro, puedes ignorar este correo.",
                    codigo);

                EstablecerMensaje(
                    "Te enviamos un código de verificación. Revisa tu correo.",
                    "Exito",
                    "Correo enviado");

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

        // Paso 2: validar el código y crear la cuenta.
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

                EstablecerMensaje(
                    "El registro ha caducado. Inténtalo nuevamente.",
                    "Advertencia",
                    "Registro caducado");

                return RedirectToAction(nameof(Register));
            }

            if (!long.TryParse(expiraTexto, out long expira) ||
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expira)
            {
                LimpiarRegistroPendiente();

                EstablecerMensaje(
                    "El código ha caducado. Regístrate nuevamente.",
                    "Advertencia",
                    "Código caducado");

                return RedirectToAction(nameof(Register));
            }

            int intentos = sesion.GetInt32(IntentosKey) ?? 0;

            if (intentos >= 5)
            {
                LimpiarRegistroPendiente();

                EstablecerMensaje(
                    "Superaste los intentos permitidos. Inicia el registro nuevamente.",
                    "Advertencia",
                    "Intentos agotados");

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

                    EstablecerMensaje(
                        "Superaste los intentos permitidos. Inicia el registro nuevamente.",
                        "Advertencia",
                        "Intentos agotados");

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

                EstablecerMensaje(
                    "No fue posible recuperar los datos del registro. Inténtalo nuevamente.",
                    "Error",
                    "Error en el registro");

                return RedirectToAction(nameof(Register));
            }

            if (datos == null)
            {
                LimpiarRegistroPendiente();

                EstablecerMensaje(
                    "No fue posible recuperar los datos del registro. Inténtalo nuevamente.",
                    "Error",
                    "Error en el registro");

                return RedirectToAction(nameof(Register));
            }

            if (await _usuarioRepository.ExisteCorreoAsync(datos.Correo) ||
                await _usuarioRepository.ExisteNumeroDocumentoAsync(
                    datos.NumeroDocumento))
            {
                LimpiarRegistroPendiente();

                EstablecerMensaje(
                    "El correo o el documento ya está registrado. Comprueba los datos.",
                    "Error",
                    "Datos ya registrados");

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

            // Enviar el correo de bienvenida.
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
            }

            EstablecerMensaje(
                "Registro exitoso",
                "Exito",
                "¡Bienvenido al sistema!");

            return RedirectToAction("Index", "Home");
        }

        // =====================================================
        // RECUPERACIÓN DE CONTRASEÑA - CU-04
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarRecuperacion(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                EstablecerMensaje(
                    "Ingresa tu correo electrónico.",
                    "Advertencia",
                    "Dato requerido");

                return RedirectToAction(nameof(Login));
            }

            correo = correo.Trim();

            var usuario =
                await _usuarioRepository.ObtenerPorCorreoAsync(correo);

            if (usuario == null)
            {
                EstablecerMensaje(
                    "Correo no registrado en el sistema.",
                    "Error",
                    "Correo no encontrado");

                return RedirectToAction(nameof(Login));
            }

            await _usuarioRepository
                .InvalidarRecuperacionesAnterioresAsync(usuario.IdUsuario);

            string token = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            string tokenHash = CalcularHashToken(token);

            var recuperacion = new RecuperacionesContrasena
            {
                IdUsuario = usuario.IdUsuario,
                TokenHash = tokenHash,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(30),
                Usado = false
            };

            await _usuarioRepository
                .CrearRecuperacionContrasenaAsync(recuperacion);

            string? enlace = Url.Action(
                nameof(RestablecerContrasena),
                "Account",
                new { token },
                Request.Scheme);

            if (string.IsNullOrWhiteSpace(enlace))
            {
                await _usuarioRepository
                    .InvalidarRecuperacionesAnterioresAsync(usuario.IdUsuario);

                EstablecerMensaje(
                    "No fue posible generar el enlace de recuperación.",
                    "Error",
                    "Error al generar el enlace");

                return RedirectToAction(nameof(Login));
            }

            try
            {
                string nombre = WebUtility.HtmlEncode(usuario.Nombres);
                string enlaceSeguro = WebUtility.HtmlEncode(enlace);

                string contenido = $@"
                    <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;'>
                        <h2>Restablecer contraseña - CMSE</h2>
                        <p>Hola, {nombre}.</p>
                        <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta.</p>
                        <p>Para continuar, utiliza el siguiente enlace:</p>
                        <p>
                            <a href='{enlaceSeguro}'
                               style='display:inline-block;padding:12px 20px;
                                      background-color:#2563eb;color:white;
                                      text-decoration:none;border-radius:6px;'>
                                Restablecer contraseña
                            </a>
                        </p>
                        <p>Este enlace tiene una validez de 30 minutos y solo puede utilizarse una vez.</p>
                        <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                        <p>Equipo CMSE</p>
                    </div>";

                await _emailManager.EnviarCorreoAsync(
                    usuario.Correo,
                    "Restablecimiento de contraseña - CMSE",
                    contenido);

                EstablecerMensaje(
                    "Te enviamos un enlace de recuperación. Revisa tu correo electrónico.",
                    "Exito",
                    "Correo enviado");
            }
            catch (Exception)
            {
                await _usuarioRepository
                    .InvalidarRecuperacionesAnterioresAsync(usuario.IdUsuario);

                EstablecerMensaje(
                    "No pudimos enviar el correo de recuperación. Inténtalo nuevamente.",
                    "Error",
                    "Error al enviar el correo");
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                EstablecerMensaje(
                    "El enlace de recuperación no es válido.",
                    "Error",
                    "Enlace inválido");

                return RedirectToAction(nameof(Login));
            }

            string tokenHash = CalcularHashToken(token);

            var recuperacion =
                await _usuarioRepository.ObtenerRecuperacionPorTokenAsync(
                    tokenHash);

            if (recuperacion == null ||
                recuperacion.Usado ||
                recuperacion.FechaExpiracion <= DateTime.UtcNow)
            {
                EstablecerMensaje(
                    "El enlace de recuperación no es válido o ha caducado.",
                    "Error",
                    "Enlace inválido o caducado");

                return RedirectToAction(nameof(Login));
            }

            ViewBag.Token = token;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(
            string token,
            string nuevaContrasena,
            string confirmarContrasena)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                EstablecerMensaje(
                    "El enlace de recuperación no es válido.",
                    "Error",
                    "Enlace inválido");

                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                ModelState.AddModelError(
                    "nuevaContrasena",
                    "Debes ingresar una nueva contraseña.");
            }

            if (string.IsNullOrWhiteSpace(confirmarContrasena))
            {
                ModelState.AddModelError(
                    "confirmarContrasena",
                    "Debes confirmar la nueva contraseña.");
            }
            else if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError(
                    "confirmarContrasena",
                    "Las contraseñas no coinciden.");
            }

            string tokenHash = CalcularHashToken(token);

            var recuperacion =
                await _usuarioRepository.ObtenerRecuperacionPorTokenAsync(
                    tokenHash);

            if (recuperacion == null ||
                recuperacion.Usado ||
                recuperacion.FechaExpiracion <= DateTime.UtcNow)
            {
                EstablecerMensaje(
                    "El enlace de recuperación no es válido o ha caducado.",
                    "Error",
                    "Enlace inválido o caducado");

                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Token = token;
                return View();
            }

            recuperacion.IdUsuarioNavigation.Contrasena =
                _passwordService.HashPassword(nuevaContrasena);

            recuperacion.Usado = true;

            await _usuarioRepository.ActualizarAsync(
                recuperacion.IdUsuarioNavigation);

            await _usuarioRepository.InvalidarRecuperacionesAnterioresAsync(
                recuperacion.IdUsuario);

            EstablecerMensaje(
                "Tu contraseña fue restablecida correctamente. Ya puedes iniciar sesión.",
                "Exito",
                "Contraseña actualizada");

            return RedirectToAction(nameof(Login));
        }

        private static string CalcularHashToken(string token)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(hash);
        }

        // =====================================================
        // MÉTODOS AUXILIARES DEL REGISTRO
        // =====================================================

        private static string CalcularHashCodigo(string codigo)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(codigo));

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
            public string? Telefono { get; set; }
            public string ContrasenaHash { get; set; } = "";
        }

        // =====================================================
        // INICIO DE SESIÓN - CU-03
        // =====================================================

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

            if (usuario == null)
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

            DateTime ahora = DateTime.Now;

            if (usuario.BloqueoHasta.HasValue &&
                usuario.BloqueoHasta.Value > ahora)
            {
                ModelState.AddModelError(
                    "",
                    $"Tu cuenta está bloqueada temporalmente. " +
                    $"Inténtalo nuevamente después de las " +
                    $"{usuario.BloqueoHasta.Value:HH:mm}.");

                return View(modelo);
            }

            if (usuario.BloqueoHasta.HasValue &&
                usuario.BloqueoHasta.Value <= ahora)
            {
                usuario.BloqueoHasta = null;
                usuario.IntentosFallidos = 0;

                await _usuarioRepository.ActualizarAsync(usuario);
            }

            bool contrasenaCorrecta =
                _passwordService.VerifyPassword(
                    modelo.Contrasena, usuario.Contrasena);

            if (!contrasenaCorrecta)
            {
                if (usuario.IntentosFallidos < 5)
                    usuario.IntentosFallidos++;

                if (usuario.IntentosFallidos >= 5)
                {
                    usuario.BloqueoHasta = ahora.AddMinutes(15);

                    await _usuarioRepository.ActualizarAsync(usuario);

                    ModelState.AddModelError(
                        "",
                        "Has superado los 5 intentos fallidos. " +
                        "Tu cuenta ha sido bloqueada durante 15 minutos.");

                    return View(modelo);
                }

                await _usuarioRepository.ActualizarAsync(usuario);

                int intentosRestantes = 5 - usuario.IntentosFallidos;

                ModelState.AddModelError(
                    "",
                    $"Correo o contraseña incorrectos. " +
                    $"Intentos restantes: {intentosRestantes}.");

                return View(modelo);
            }

            if (usuario.IntentosFallidos > 0 ||
                usuario.BloqueoHasta.HasValue)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueoHasta = null;

                await _usuarioRepository.ActualizarAsync(usuario);
            }

            HttpContext.Session.Clear();

            HttpContext.Session.SetInt32(
                "UsuarioId", usuario.IdUsuario);

            HttpContext.Session.SetString(
                "UsuarioNombre", usuario.Nombres ?? "");

            HttpContext.Session.SetString(
                "UsuarioCorreo", usuario.Correo ?? "");

            HttpContext.Session.SetInt32(
                "RolId", usuario.IdRol);

            HttpContext.Session.SetString(
                "RolNombre", usuario.IdRolNavigation?.Nombre ?? "");

            EstablecerMensaje(
                "Inicio de sesión exitoso",
                "Exito",
                "Bienvenido al sistema");

            return RedirectToAction("Index", "Home");
        }

        // =====================================================
        // CIERRE DE SESIÓN - CU-22
        // =====================================================

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
