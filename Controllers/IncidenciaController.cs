
using cmse.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cmse.Controllers
{
    public class IncidenciaController : Controller
    {
        private readonly CmseContext _context;
        private readonly IWebHostEnvironment _environment;

        private const int EstadoPendiente = 5;
        private const int EstadoCerrada = 10;
        private const int MaximoArchivos = 5;
        private const long TamanoMaximoArchivo = 10 * 1024 * 1024;

        private static readonly HashSet<string> ExtensionesPermitidas =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "jpg", "jpeg", "png", "pdf", "txt", "xml", "html"
            };

        public IncidenciaController(
            CmseContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int? ObtenerUsuarioId()
        {
            return HttpContext.Session.GetInt32("UsuarioId");
        }

        private bool EsUsuario()
        {
            return HttpContext.Session.GetInt32("RolId") == 3;
        }

        private async Task CargarListasAsync()
        {
            ViewBag.Equipos = await _context.Equipos
                .OrderBy(e => e.Nombre)
                .ToListAsync();
        }

        private bool IncidenciaCerrada(Incidencia incidencia)
        {
            return incidencia.IdEstado == EstadoCerrada
                || incidencia.FechaCierre.HasValue
                || (
                    incidencia.IdEstadoNavigation.Modulo == "incidencia"
                    && incidencia.IdEstadoNavigation.Nombre
                        .Equals("Cerrada", StringComparison.OrdinalIgnoreCase)
                );
        }

        private string ObtenerCarpetaArchivos()
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(webRoot, "uploads", "incidencias");
        }

        // CU-09: Consultar incidencias propias
        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            int registrosPorPagina = 10;

            var consulta = _context.Incidencias
                .Where(i => i.IdUsuario == idUsuario.Value)
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .OrderByDescending(i => i.FechaRegistro);

            int totalRegistros = await consulta.CountAsync();

            int totalPaginas = (int)Math.Ceiling(
                totalRegistros / (double)registrosPorPagina);

            pagina = Math.Max(
                1,
                Math.Min(pagina, Math.Max(1, totalPaginas)));

            var incidencias = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;

            return View(incidencias);
        }

        // CU-08: Mostrar formulario de registro
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!ObtenerUsuarioId().HasValue)
                return RedirectToAction("Login", "Account");

            if (!EsUsuario())
                return Forbid();

            await CargarListasAsync();

            return View();
        }

        // CU-08: Registrar incidencia con archivos adjuntos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? titulo,
            string? descripcion,
            int idEquipo,
            List<IFormFile>? archivos)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            if (!EsUsuario())
                return Forbid();

            titulo = titulo?.Trim() ?? "";
            descripcion = descripcion?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(titulo))
            {
                ModelState.AddModelError(
                    "titulo",
                    "El título es obligatorio.");
            }

            if (titulo.Length > 100)
            {
                ModelState.AddModelError(
                    "titulo",
                    "El título no puede superar 100 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                ModelState.AddModelError(
                    "descripcion",
                    "La descripción es obligatoria.");
            }

            bool equipoValido = await _context.Equipos
                .AnyAsync(e => e.IdEquipo == idEquipo);

            if (!equipoValido)
            {
                ModelState.AddModelError(
                    "idEquipo",
                    "Selecciona un equipo válido.");
            }

            var estadoInicial = await _context.Estados
                .FirstOrDefaultAsync(e =>
                    e.IdEstado == EstadoPendiente &&
                    e.Modulo == "incidencia");

            if (estadoInicial == null)
            {
                ModelState.AddModelError(
                    "",
                    "No se encontró el estado inicial pendiente para incidencias.");
            }

            var archivosValidos = archivos?
                .Where(a => a != null && a.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (archivosValidos.Count > MaximoArchivos)
            {
                ModelState.AddModelError(
                    "archivos",
                    $"Puedes adjuntar máximo {MaximoArchivos} archivos.");
            }

            foreach (var archivo in archivosValidos)
            {
                string extension = Path.GetExtension(archivo.FileName)
                    .TrimStart('.')
                    .ToLowerInvariant();

                if (!ExtensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        "archivos",
                        $"El archivo {Path.GetFileName(archivo.FileName)} tiene una extensión no permitida.");
                }

                if (archivo.Length > TamanoMaximoArchivo)
                {
                    ModelState.AddModelError(
                        "archivos",
                        $"El archivo {Path.GetFileName(archivo.FileName)} supera el límite de 10 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync();
                return View();
            }

            var incidencia = new Incidencia
            {
                IdUsuario = idUsuario.Value,
                IdEquipo = idEquipo,
                IdEstado = estadoInicial!.IdEstado,
                Titulo = titulo,
                Descripcion = descripcion,
                FechaRegistro = DateTime.Now,
                FechaCierre = null
            };

            var rutasGuardadas = new List<string>();

            try
            {
                if (archivosValidos.Count > 0)
                {
                    string carpeta = ObtenerCarpetaArchivos();

                    Directory.CreateDirectory(carpeta);

                    foreach (var archivo in archivosValidos)
                    {
                        string nombreOriginal = Path.GetFileName(
                            archivo.FileName);

                        string extension = Path.GetExtension(nombreOriginal)
                            .TrimStart('.')
                            .ToLowerInvariant();

                        string nombreFisico =
                            $"{Guid.NewGuid():N}.{extension}";

                        string rutaFisica = Path.Combine(
                            carpeta,
                            nombreFisico);

                        string rutaRelativa =
                            $"/uploads/incidencias/{nombreFisico}";

                        await using (var stream = new FileStream(
                            rutaFisica,
                            FileMode.CreateNew))
                        {
                            await archivo.CopyToAsync(stream);
                        }

                        rutasGuardadas.Add(rutaFisica);

                        incidencia.ArchivosIncidencia.Add(
                            new ArchivosIncidencium
                            {
                                NombreArchivo = nombreOriginal,
                                RutaArchivo = rutaRelativa,
                                Extension = extension,
                                FechaSubida = DateTime.Now
                            });
                    }
                }

                _context.Incidencias.Add(incidencia);

                await _context.SaveChangesAsync();
            }
            catch
            {
                foreach (string ruta in rutasGuardadas)
                {
                    if (System.IO.File.Exists(ruta))
                        System.IO.File.Delete(ruta);
                }

                throw;
            }

            TempData["Mensaje"] =
                "Incidencia registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // CU-09: Consultar detalles de una incidencia propia
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            var incidencia = await _context.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdTipoIncidenciaNavigation)
                .Include(i => i.ArchivosIncidencia)
                .FirstOrDefaultAsync(i =>
                    i.IdIncidencia == id &&
                    i.IdUsuario == idUsuario.Value);

            if (incidencia == null)
                return NotFound();

            return View(incidencia);
        }

        // CU-10: Mostrar formulario de actualización
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            if (!EsUsuario())
                return Forbid();

            var incidencia = await _context.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.ArchivosIncidencia)
                .FirstOrDefaultAsync(i =>
                    i.IdIncidencia == id &&
                    i.IdUsuario == idUsuario.Value);

            if (incidencia == null)
                return NotFound();

            if (IncidenciaCerrada(incidencia))
            {
                TempData["Mensaje"] =
                    "No puedes actualizar una incidencia que ya está cerrada.";

                return RedirectToAction(nameof(Details), new { id });
            }

            return View(incidencia);
        }

        // CU-10: Guardar actualización de una incidencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEdicion(
            int id,
            string? titulo,
            string? descripcion,
            List<IFormFile>? archivos)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            if (!EsUsuario())
                return Forbid();

            var incidencia = await _context.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.ArchivosIncidencia)
                .FirstOrDefaultAsync(i =>
                    i.IdIncidencia == id &&
                    i.IdUsuario == idUsuario.Value);

            if (incidencia == null)
                return NotFound();

            if (IncidenciaCerrada(incidencia))
            {
                TempData["Mensaje"] =
                    "No puedes actualizar una incidencia que ya está cerrada.";

                return RedirectToAction(nameof(Details), new { id });
            }

            titulo = titulo?.Trim() ?? "";
            descripcion = descripcion?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(titulo))
            {
                ModelState.AddModelError(
                    "titulo",
                    "El título es obligatorio.");
            }

            if (titulo.Length > 100)
            {
                ModelState.AddModelError(
                    "titulo",
                    "El título no puede superar 100 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                ModelState.AddModelError(
                    "descripcion",
                    "La descripción es obligatoria.");
            }

            var archivosValidos = archivos?
                .Where(a => a != null && a.Length > 0)
                .ToList() ?? new List<IFormFile>();

            int archivosExistentes = incidencia.ArchivosIncidencia.Count;
            int totalArchivos = archivosExistentes + archivosValidos.Count;

            if (totalArchivos > MaximoArchivos)
            {
                ModelState.AddModelError(
                    "archivos",
                    $"La incidencia puede tener máximo {MaximoArchivos} archivos en total. Actualmente tiene {archivosExistentes}.");
            }

            foreach (var archivo in archivosValidos)
            {
                string extension = Path.GetExtension(archivo.FileName)
                    .TrimStart('.')
                    .ToLowerInvariant();

                if (!ExtensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        "archivos",
                        $"El archivo {Path.GetFileName(archivo.FileName)} tiene una extensión no permitida.");
                }

                if (archivo.Length > TamanoMaximoArchivo)
                {
                    ModelState.AddModelError(
                        "archivos",
                        $"El archivo {Path.GetFileName(archivo.FileName)} supera el límite de 10 MB.");
                }
            }

            if (!ModelState.IsValid)
                return View("Editar", incidencia);

            // No se modifica IdEquipo ni IdEstado.
            incidencia.Titulo = titulo;
            incidencia.Descripcion = descripcion;

            var rutasGuardadas = new List<string>();

            try
            {
                if (archivosValidos.Count > 0)
                {
                    string carpeta = ObtenerCarpetaArchivos();

                    Directory.CreateDirectory(carpeta);

                    foreach (var archivo in archivosValidos)
                    {
                        string nombreOriginal = Path.GetFileName(
                            archivo.FileName);

                        string extension = Path.GetExtension(nombreOriginal)
                            .TrimStart('.')
                            .ToLowerInvariant();

                        string nombreFisico =
                            $"{Guid.NewGuid():N}.{extension}";

                        string rutaFisica = Path.Combine(
                            carpeta,
                            nombreFisico);

                        string rutaRelativa =
                            $"/uploads/incidencias/{nombreFisico}";

                        await using (var stream = new FileStream(
                            rutaFisica,
                            FileMode.CreateNew))
                        {
                            await archivo.CopyToAsync(stream);
                        }

                        rutasGuardadas.Add(rutaFisica);

                        incidencia.ArchivosIncidencia.Add(
                            new ArchivosIncidencium
                            {
                                NombreArchivo = nombreOriginal,
                                RutaArchivo = rutaRelativa,
                                Extension = extension,
                                FechaSubida = DateTime.Now
                            });
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch
            {
                foreach (string ruta in rutasGuardadas)
                {
                    if (System.IO.File.Exists(ruta))
                        System.IO.File.Delete(ruta);
                }

                throw;
            }

            TempData["Mensaje"] = "Actualización exitosa";

            return RedirectToAction("Index", "Home");
        }

        // CU-10: Eliminar un archivo adjunto de una incidencia propia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarArchivo(
            int idIncidencia,
            int idArchivo)
        {
            int? idUsuario = ObtenerUsuarioId();

            if (!idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            if (!EsUsuario())
                return Forbid();

            var incidencia = await _context.Incidencias
                .Include(i => i.IdEstadoNavigation)
                .FirstOrDefaultAsync(i =>
                    i.IdIncidencia == idIncidencia &&
                    i.IdUsuario == idUsuario.Value);

            if (incidencia == null)
                return NotFound();

            if (IncidenciaCerrada(incidencia))
            {
                TempData["Mensaje"] =
                    "No puedes eliminar archivos de una incidencia cerrada.";

                return RedirectToAction(nameof(Details),
                    new { id = idIncidencia });
            }

            var archivo = await _context.ArchivosIncidencia
                .FirstOrDefaultAsync(a =>
                    a.IdArchivo == idArchivo &&
                    a.IdIncidencia == idIncidencia);

            if (archivo == null)
                return NotFound();

            // Se obtiene únicamente el nombre del archivo almacenado.
            // Así se evita utilizar rutas externas o manipuladas.
            string nombreFisico = Path.GetFileName(
                archivo.RutaArchivo.Replace('\\', '/'));

            string rutaFisica = Path.Combine(
                ObtenerCarpetaArchivos(),
                nombreFisico);

            // Primero se elimina el registro de la base de datos.
            _context.ArchivosIncidencia.Remove(archivo);

            await _context.SaveChangesAsync();

            // Después de guardar correctamente, se elimina el archivo físico.
            try
            {
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);
            }
            catch (IOException)
            {
                TempData["Mensaje"] =
                    "El archivo se quitó de la incidencia, pero no se pudo eliminar físicamente del servidor.";

                return RedirectToAction(nameof(Editar),
                    new { id = idIncidencia });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Mensaje"] =
                    "El archivo se quitó de la incidencia, pero el servidor no permitió eliminar el archivo físico.";

                return RedirectToAction(nameof(Editar),
                    new { id = idIncidencia });
            }

            TempData["Mensaje"] = "Archivo eliminado correctamente.";

            return RedirectToAction(nameof(Editar),
                new { id = idIncidencia });
        }
    }
}
