
using cmse.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cmse.Controllers
{
    public class EquipmentController : Controller
    {
        private readonly CmseContext _context;

        public EquipmentController(CmseContext context)
        {
            _context = context;
        }

        private bool TieneSesion()
        {
            return HttpContext.Session.GetInt32("UsuarioId").HasValue;
        }

        private async Task CargarListasAsync()
        {
            ViewBag.TiposEquipo = await _context.TiposEquipos
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            ViewBag.Estados = await _context.Estados
                .Where(e => e.Modulo != null &&
                    (e.Modulo.ToLower() == "equipo" ||
                     e.Modulo.ToLower() == "equipos"))
                .OrderBy(e => e.Nombre)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            if (!TieneSesion())
                return RedirectToAction("Login", "Account");

            int registrosPorPagina = 10;

            var consulta = _context.Equipos
                .Include(e => e.IdTipoEquipoNavigation)
                .Include(e => e.IdEstadoNavigation)
                .OrderBy(e => e.Nombre);

            int totalRegistros = await consulta.CountAsync();

            int totalPaginas = (int)Math.Ceiling(
                totalRegistros / (double)registrosPorPagina);

            pagina = Math.Max(1, Math.Min(pagina, Math.Max(1, totalPaginas)));

            var equipos = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;

            return View(equipos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!TieneSesion())
                return RedirectToAction("Login", "Account");

            await CargarListasAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipo equipo)
        {
            if (!TieneSesion())
                return RedirectToAction("Login", "Account");

            // Estos campos se asignan desde el servidor.
            ModelState.Remove(nameof(Equipo.IdEquipo));
            ModelState.Remove(nameof(Equipo.FechaRegistro));
            ModelState.Remove(nameof(Equipo.IdEstadoNavigation));
            ModelState.Remove(nameof(Equipo.IdTipoEquipoNavigation));
            ModelState.Remove(nameof(Equipo.Incidencia));
            ModelState.Remove(nameof(Equipo.Mantenimientos));

            equipo.NumeroSerie = string.IsNullOrWhiteSpace(equipo.NumeroSerie)
                ? null
                : equipo.NumeroSerie.Trim();

            equipo.Nombre = equipo.Nombre?.Trim() ?? "";
            equipo.CodigoInventario =
                equipo.CodigoInventario?.Trim() ?? "";

            if (!await _context.TiposEquipos.AnyAsync(
                t => t.IdTipoEquipo == equipo.IdTipoEquipo))
            {
                ModelState.AddModelError(
                    nameof(equipo.IdTipoEquipo),
                    "Selecciona un tipo de equipo válido.");
            }

            if (!await _context.Estados.AnyAsync(e =>
                e.IdEstado == equipo.IdEstado &&
                e.Modulo != null &&
                (e.Modulo.ToLower() == "equipo" ||
                 e.Modulo.ToLower() == "equipos")))
            {
                ModelState.AddModelError(
                    nameof(equipo.IdEstado),
                    "Selecciona un estado válido para equipos.");
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync();
                return View(equipo);
            }

            bool codigoExiste = await _context.Equipos.AnyAsync(
                e => e.CodigoInventario == equipo.CodigoInventario);

            if (codigoExiste)
            {
                ModelState.AddModelError(
                    nameof(equipo.CodigoInventario),
                    "Ese código de inventario ya existe.");

                await CargarListasAsync();
                return View(equipo);
            }

            if (equipo.NumeroSerie != null &&
                await _context.Equipos.AnyAsync(
                    e => e.NumeroSerie == equipo.NumeroSerie))
            {
                ModelState.AddModelError(
                    nameof(equipo.NumeroSerie),
                    "Ese número de serie ya está registrado.");

                await CargarListasAsync();
                return View(equipo);
            }

            equipo.FechaRegistro = DateOnly.FromDateTime(DateTime.Today);

            _context.Equipos.Add(equipo);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Equipo registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}
