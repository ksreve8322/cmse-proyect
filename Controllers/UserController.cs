
using cmse.Repositories;
using Microsoft.AspNetCore.Mvc;
using cmse.VistaModelos;
namespace cmse.Controllers
{
    public class UserController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;

        public UserController(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
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

            if (await _usuarioRepository.ExisteCorreoEnOtroUsuarioAsync(
                modelo.Correo, modelo.IdUsuario))
            {
                ModelState.AddModelError(
                    nameof(modelo.Correo),
                    "Este correo ya está registrado.");

                ViewBag.TiposDocumento =
                    await _usuarioRepository.ObtenerTiposDocumentoAsync();

                ViewBag.Roles =
                    await _usuarioRepository.ObtenerRolesAsync();

                return View(modelo);
            }

            if (await _usuarioRepository.ExisteDocumentoEnOtroUsuarioAsync(
                modelo.NumeroDocumento, modelo.IdUsuario))
            {
                ModelState.AddModelError(
                    nameof(modelo.NumeroDocumento),
                    "Este documento ya está registrado.");

                ViewBag.TiposDocumento =
                    await _usuarioRepository.ObtenerTiposDocumentoAsync();

                ViewBag.Roles =
                    await _usuarioRepository.ObtenerRolesAsync();

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

            return RedirectToAction(nameof(Details), new { id = usuario.IdUsuario });
        }

    }
}
