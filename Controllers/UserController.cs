
using cmse.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cmse.VistaModelos;
using cmse.Utilities;
using cmse.Repositories.Models;
namespace cmse.Controllers
{
    public class UserController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;
        private readonly PasswordService _passwordService;

        public UserController(
            UsuarioRepository usuarioRepository,
            PasswordService passwordService)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
        }

        private bool EsAdministrador()
        {
            return HttpContext.Session.GetInt32("RolId") == 1;
        }

        // GET: User/Index
        public async Task<IActionResult> Index(int pagina = 1)
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            const int registrosPorPagina = 10;

            int totalUsuarios =
                await _usuarioRepository.ContarUsuariosAsync();

            int totalPaginas = (int)Math.Ceiling(
                totalUsuarios / (double)registrosPorPagina);

            pagina = Math.Max(1, Math.Min(
                pagina, Math.Max(1, totalPaginas)));

            var usuarios =
                await _usuarioRepository.ObtenerTodosPaginadosAsync(
                    pagina, registrosPorPagina);

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalUsuarios = totalUsuarios;

            return View(usuarios);
        }
        // GET: User/Create
        public async Task<IActionResult> Create()
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            ViewBag.TiposDocumento = await _usuarioRepository.ObtenerTiposDocumentoAsync();
            ViewBag.Roles = await _usuarioRepository.ObtenerRolesAsync();

            return View(new RegistrarUsuarioViewModel());
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistrarUsuarioViewModel modelo)
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            var tiposDocumento = await _usuarioRepository.ObtenerTiposDocumentoAsync();
            var roles = await _usuarioRepository.ObtenerRolesAsync();

            ViewBag.TiposDocumento = tiposDocumento;
            ViewBag.Roles = roles;

            // Comprobar que el tipo de documento seleccionado exista.
            if (!tiposDocumento.Any(t => t.IdTipoDocumento == modelo.IdTipoDocumento))
            {
                ModelState.AddModelError("IdTipoDocumento", "Selecciona un tipo de documento válido.");
            }

            // Comprobar que el rol seleccionado exista.
            if (!roles.Any(r => r.IdRol == modelo.IdRol))
            {
                ModelState.AddModelError("IdRol", "Selecciona un rol válido.");
            }

            // Comprobar que el correo no esté registrado.
            if (await _usuarioRepository.ExisteCorreoAsync(modelo.Correo))
            {
                ModelState.AddModelError("Correo", "Este correo ya está registrado.");
            }

            // Comprobar que el documento no esté registrado.
            if (await _usuarioRepository.ExisteNumeroDocumentoAsync(modelo.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "Este número de documento ya está registrado.");
            }

            if (!ModelState.IsValid)
                return View(modelo);

            var usuario = new Usuario
            {
                IdRol = modelo.IdRol,
                IdTipoDocumento = modelo.IdTipoDocumento,
                Nombres = modelo.Nombres.Trim(),
                Apellidos = modelo.Apellidos.Trim(),
                NumeroDocumento = modelo.NumeroDocumento.Trim(),
                Correo = modelo.Correo.Trim(),
                Telefono = modelo.Telefono?.Trim(),
                Contrasena = _passwordService.HashPassword(modelo.Contrasena),
                Estado = true,
                IntentosFallidos = 0,
                BloqueoHasta = null
            };

            await _usuarioRepository.CrearAsync(usuario);

            TempData["Mensaje"] = "El usuario fue registrado correctamente.";
            TempData["TipoMensaje"] = "Exito";
            TempData["TituloModal"] = "Registro exitoso";

            return RedirectToAction(nameof(Index));
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario == null)
                return NotFound();

            var modelo = new EditarUsuarioViewModel
            {
                IdUsuario = usuario.IdUsuario,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                IdTipoDocumento = usuario.IdTipoDocumento,
                NumeroDocumento = usuario.NumeroDocumento,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                IdRol = usuario.IdRol,
                Estado = usuario.Estado
            };

            ViewBag.TiposDocumento =
                await _usuarioRepository.ObtenerTiposDocumentoAsync();

            ViewBag.Roles =
                await _usuarioRepository.ObtenerRolesAsync();

            return View(modelo);
        }

        // POST: User/Edit

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditarUsuarioViewModel modelo)
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.TiposDocumento =
                    await _usuarioRepository.ObtenerTiposDocumentoAsync();

                ViewBag.Roles =
                    await _usuarioRepository.ObtenerRolesAsync();

                return View(modelo);
            }

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(
                modelo.IdUsuario);

            if (usuario == null)
                return NotFound();

            // Validar que el rol seleccionado exista.
            var roles = await _usuarioRepository.ObtenerRolesAsync();

            if (!roles.Any(r => r.IdRol == modelo.IdRol))
            {
                ModelState.AddModelError(
                    nameof(modelo.IdRol),
                    "El rol seleccionado no es válido.");
            }

            // Validar que el tipo de documento exista.
            var tiposDocumento =
                await _usuarioRepository.ObtenerTiposDocumentoAsync();

            if (!tiposDocumento.Any(t =>
                t.IdTipoDocumento == modelo.IdTipoDocumento))
            {
                ModelState.AddModelError(
                    nameof(modelo.IdTipoDocumento),
                    "El tipo de documento seleccionado no es válido.");
            }

            // Evitar correos duplicados.
            if (await _usuarioRepository.ExisteCorreoEnOtroUsuarioAsync(
                modelo.Correo, modelo.IdUsuario))
            {
                ModelState.AddModelError(
                    nameof(modelo.Correo),
                    "Este correo ya está registrado.");
            }

            // Evitar documentos duplicados.
            if (await _usuarioRepository.ExisteDocumentoEnOtroUsuarioAsync(
                modelo.NumeroDocumento, modelo.IdUsuario))
            {
                ModelState.AddModelError(
                    nameof(modelo.NumeroDocumento),
                    "Este documento ya está registrado.");
            }

            // Evitar que se pierda el último administrador activo.
            if (usuario.IdRol == 1 && usuario.Estado &&
                (modelo.IdRol != 1 || !modelo.Estado))
            {
                if (!await _usuarioRepository.ExisteOtroAdministradorActivoAsync(
                    modelo.IdUsuario))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No puedes quitar el rol o desactivar al último administrador activo.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TiposDocumento = tiposDocumento;
                ViewBag.Roles = roles;

                return View(modelo);
            }

            usuario.Nombres = modelo.Nombres;
            usuario.Apellidos = modelo.Apellidos;
            usuario.IdTipoDocumento = modelo.IdTipoDocumento;
            usuario.NumeroDocumento = modelo.NumeroDocumento;
            usuario.Correo = modelo.Correo;
            usuario.Telefono = modelo.Telefono;
            usuario.IdRol = modelo.IdRol;
            usuario.Estado = modelo.Estado;

            await _usuarioRepository.ActualizarAsync(usuario);

            TempData["Mensaje"] = "Usuario actualizado correctamente.";

            return RedirectToAction(nameof(Details),
                new { id = usuario.IdUsuario });
        }


        // POST: User/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!EsAdministrador())
                return RedirectToAction("Login", "Account");

            // Evitar que el administrador elimine su propia cuenta.
            int? idUsuarioSesion = HttpContext.Session.GetInt32("UsuarioId");

            if (idUsuarioSesion == id)
            {
                TempData["Mensaje"] =
                    "No puedes eliminar tu propia cuenta desde este módulo.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario == null)
            {
                TempData["Mensaje"] = "El usuario que intentas eliminar no existe.";
                return RedirectToAction(nameof(Index));
            }

            // Proteger al último administrador activo.
            if (usuario.IdRol == 1 && usuario.Estado)
            {
                bool existeOtroAdministrador =
                    await _usuarioRepository.ExisteOtroAdministradorActivoAsync(id);

                if (!existeOtroAdministrador)
                {
                    TempData["Mensaje"] =
                        "No puedes eliminar al último administrador activo.";

                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // Evitar eliminar usuarios con información operativa.
            bool tieneRegistros =
                await _usuarioRepository.TieneRegistrosOperativosAsync(id);

            if (tieneRegistros)
            {
                TempData["Mensaje"] =
                    "No se puede eliminar este usuario porque tiene registros operativos asociados.";

                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                await _usuarioRepository.EliminarCuentaAsync(id);

                TempData["MensajeExito"] = "Usuario eliminado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["Mensaje"] =
                    "No se pudo eliminar el usuario porque tiene otros registros relacionados en el sistema.";

                return RedirectToAction(nameof(Details), new { id });
            }
        }

    }

}
