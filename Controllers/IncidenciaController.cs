using cmse.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cmse.Controllers
{
    public class IncidenciaController : Controller
    {
        private readonly CmseContext _context;

        public IncidenciaController(CmseContext context)
        {
            _context = context;
        }

        private int? ObtenerUsuarioId()
        {
            return HttpContext.Session.GetInt32("UsuarioId");
        }

        private async Task CargarListasAsync()
        {
            ViewBag.Equipos = await _context.Equipos
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Estados = await _context.Estados
                .Where(e => e.Modulo != null &&
                    (e.Modulo.ToLower() == "incidencia" ||
                     e.Modulo.ToLower() == "incidencias"))
                .OrderBy(e => e.Nombre)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            Console.WriteLine(
                $"[CMSE] Usuario en sesión: {idUsuario.Value}");

            var incidencias = await _context.Incidencias
                .Where(i => i.IdUsuario == idUsuario.Value)
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .OrderByDescending(i => i.FechaRegistro)
                .ToListAsync();

            Console.WriteLine(
                $"[CMSE] Incidencias encontradas: {incidencias.Count}");

            return View(incidencias);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!ObtenerUsuarioId().HasValue)
                return RedirectToAction("Login", "Account");

            await CargarListasAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string titulo,
            string descripcion,
            int idEquipo,
            int idEstado)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            titulo = titulo?.Trim() ?? "";
            descripcion = descripcion?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(titulo))
                ModelState.AddModelError(
                    "titulo",
                    "El título es obligatorio.");

            if (titulo.Length > 100)
                ModelState.AddModelError(
                    "titulo",
                    "El título no puede superar 100 caracteres.");

            if (string.IsNullOrWhiteSpace(descripcion))
                ModelState.AddModelError(
                    "descripcion",
                    "La descripción es obligatoria.");

            bool equipoValido = await _context.Equipos
                .AnyAsync(e => e.IdEquipo == idEquipo);

            if (!equipoValido)
                ModelState.AddModelError(
                    "idEquipo",
                    "Selecciona un equipo válido.");

            bool estadoValido = await _context.Estados
                .AnyAsync(e =>
                    e.IdEstado == idEstado &&
                    e.Modulo != null &&
                    (e.Modulo.ToLower() == "incidencia" ||
                     e.Modulo.ToLower() == "incidencias"));

            if (!estadoValido)
                ModelState.AddModelError(
                    "idEstado",
                    "Selecciona un estado válido para incidencias.");

            if (!ModelState.IsValid)
            {
                await CargarListasAsync();
                return View();
            }

            var incidencia = new Incidencia
            {
                IdUsuario = idUsuario.Value,
                IdEquipo = idEquipo,
                IdEstado = idEstado,
                Titulo = titulo,
                Descripcion = descripcion,
                FechaRegistro = DateTime.Now,
                FechaCierre = null
            };

            _context.Incidencias.Add(incidencia);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Incidencia registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            var incidencia = await _context.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .FirstOrDefaultAsync(i =>
                    i.IdIncidencia == id &&
                    i.IdUsuario == idUsuario.Value);

            if (incidencia == null)
                return NotFound();

            return View(incidencia);
        }
    }
}