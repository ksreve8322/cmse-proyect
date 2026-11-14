
using cmse.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace cmse.Controllers
{
    public class MantenimientoController : Controller
    {
        private readonly CmseContext _contexto;

        public MantenimientoController(CmseContext contexto)
        {
            _contexto = contexto;
        }
        public class RepuestoTemporal
        {
            public string Nombre { get; set; } = "";
            public string Codigo { get; set; } = "";
            public string? Marca { get; set; }
            public string? Modelo { get; set; }
            public string? NumeroSerie { get; set; }
            public int Cantidad { get; set; }
            public decimal ValorUnitario { get; set; }
            public DateOnly FechaInstalacion { get; set; }
            public string? Observaciones { get; set; }
        }

        private string ObtenerClaveRepuestos(int idMantenimiento)
        {
            return $"RepuestosTemporales_{idMantenimiento}";
        }

        private List<RepuestoTemporal> ObtenerRepuestosTemporales(int idMantenimiento)
        {
            string? datos = HttpContext.Session.GetString(
                ObtenerClaveRepuestos(idMantenimiento));

            if (string.IsNullOrWhiteSpace(datos))
                return new List<RepuestoTemporal>();

            return JsonSerializer.Deserialize<List<RepuestoTemporal>>(datos)
                ?? new List<RepuestoTemporal>();
        }

        private void GuardarRepuestosTemporales(
            int idMantenimiento,
            List<RepuestoTemporal> repuestos)
        {
            HttpContext.Session.SetString(
                ObtenerClaveRepuestos(idMantenimiento),
                JsonSerializer.Serialize(repuestos));
        }

        // Verificar que el usuario sea administrador
        private bool EsAdministrador()
        {
            return HttpContext.Session.GetInt32("RolId") == 1
                && HttpContext.Session.GetInt32("UsuarioId").HasValue;
        }

        // Verificar que el usuario sea técnico
        private bool EsTecnico()
        {
            return HttpContext.Session.GetInt32("RolId") == 2
                && HttpContext.Session.GetInt32("UsuarioId").HasValue;
        }

        // Consultar incidencias pendientes de asignación
        public async Task<IActionResult> Index()
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var incidencias = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdUsuarioNavigation)
                .Where(i =>
                    i.IdEstado == 5 &&
                    i.FechaCierre == null &&
                    !i.AsignacionesIncidencia.Any(a =>
                        a.FechaFinalizacion == null))
                .OrderBy(i => i.FechaRegistro)
                .ToListAsync();

            return View(incidencias);
        }

        // Consultar todos los mantenimientos registrados
        public async Task<IActionResult> Consultar()
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var mantenimientos = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .Include(m => m.IdIncidenciaNavigation)
                .OrderByDescending(m => m.FechaProgramada)
                .ToListAsync();

            return View(mantenimientos);
        }

        // Consultar los mantenimientos asignados al técnico
        [HttpGet]
        public async Task<IActionResult> MisMantenimientos()
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            var mantenimientos = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .Include(m => m.IdIncidenciaNavigation)
                .Where(m => m.IdTecnico == idTecnico)
                .OrderByDescending(m => m.FechaProgramada)
                .ToListAsync();

            return View("MisMantenimientos", mantenimientos);
        }

        // Consultar equipos disponibles para revisar su historial de mantenimiento
        [HttpGet]
        public async Task<IActionResult> Historial()
        {
            if (!EsAdministrador() && !EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var equipos = await _contexto.Equipos
                .Include(e => e.IdTipoEquipoNavigation)
                .Where(e => _contexto.Mantenimientos.Any(m =>
                    m.IdEquipo == e.IdEquipo &&
                    m.FechaRealizacion != null))
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            if (!equipos.Any())
            {
                TempData["Error"] =
                    "No hay equipos con mantenimientos realizados para consultar.";

                return RedirectToAction("Index", "Home");
            }

            return View(equipos);
        }
        // Consultar el historial completo de mantenimiento de un equipo
        [HttpGet]
        public async Task<IActionResult> VerHistorial(int id)
        {
            if (!EsAdministrador() && !EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var equipo = await _contexto.Equipos
                .Include(e => e.IdTipoEquipoNavigation)
                .FirstOrDefaultAsync(e => e.IdEquipo == id);

            if (equipo == null)
            {
                TempData["Error"] = "No se encontró el equipo seleccionado.";
                return RedirectToAction(nameof(Historial));
            }

            var mantenimientos = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .Include(m => m.IdIncidenciaNavigation)
                .Include(m => m.MantenimientoRepuestos)
                    .ThenInclude(mr => mr.IdRepuestoNavigation)
                .Where(m =>
                    m.IdEquipo == id &&
                    m.FechaRealizacion != null)
                .OrderBy(m => m.FechaRealizacion)
                .ToListAsync();

            if (!mantenimientos.Any())
            {
                TempData["Error"] =
                    "El equipo seleccionado no tiene mantenimientos realizados.";

                return RedirectToAction(nameof(Historial));
            }

            ViewBag.Equipo = equipo;

            return View(mantenimientos);
        }
        // Consultar mantenimientos disponibles para cancelar
        [HttpGet]
        public async Task<IActionResult> Cancelar()
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var mantenimientos = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .Where(m =>
                    m.IdEstado == 11 &&
                    m.FechaRealizacion == null)
                .OrderBy(m => m.FechaProgramada)
                .ToListAsync();

            return View(mantenimientos);
        }

        // Cancelar un mantenimiento programado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(
            int id,
            string motivoCancelacion)
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            if (id <= 0)
            {
                TempData["Error"] = "Selecciona un mantenimiento válido.";
                return RedirectToAction(nameof(Cancelar));
            }

            if (string.IsNullOrWhiteSpace(motivoCancelacion))
            {
                TempData["Error"] = "Debes ingresar el motivo de la cancelación.";
                return RedirectToAction(nameof(Cancelar));
            }

            var mantenimiento = await _contexto.Mantenimientos
                .FirstOrDefaultAsync(m =>
                    m.IdMantenimiento == id &&
                    m.IdEstado == 11 &&
                    m.FechaRealizacion == null);

            if (mantenimiento == null)
            {
                TempData["Error"] =
                    "No se puede cancelar el mantenimiento. Es posible que ya haya sido realizado, cancelado o que no exista.";

                return RedirectToAction(nameof(Cancelar));
            }

            mantenimiento.IdEstado = 14;
            mantenimiento.MotivoCancelacion = motivoCancelacion.Trim();

            try
            {
                await _contexto.SaveChangesAsync();

                TempData["Exito"] = "Mantenimiento cancelado exitosamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No fue posible cancelar el mantenimiento. Verifica los datos e inténtalo nuevamente.";
            }

            return RedirectToAction(nameof(Cancelar));
        }

        // Consultar incidencias asignadas
        // El administrador ve todas y el técnico solamente las propias.
        public async Task<IActionResult> ConsultarAsignaciones()
        {
            if (!EsAdministrador() && !EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var asignacionesQuery = _contexto.AsignacionesIncidencia
                .Include(a => a.IdIncidenciaNavigation)
                    .ThenInclude(i => i.IdEquipoNavigation)
                .Include(a => a.IdIncidenciaNavigation)
                    .ThenInclude(i => i.IdUsuarioNavigation)
                .Include(a => a.IdIncidenciaNavigation)
                    .ThenInclude(i => i.IdEstadoNavigation)
                .Include(a => a.IdTecnicoNavigation)
                .Where(a => a.FechaFinalizacion == null);

            if (EsTecnico())
            {
                int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

                asignacionesQuery = asignacionesQuery
                    .Where(a => a.IdTecnico == idTecnico);
            }

            var asignaciones = await asignacionesQuery
                .OrderByDescending(a => a.FechaAsignacion)
                .ToListAsync();

            return View(asignaciones);
        }

        // Mostrar formulario para actualizar el estado de una incidencia
        [HttpGet]
        public async Task<IActionResult> ActualizarEstado(int id)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            bool tieneAsignacion = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == id &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacion)
            {
                TempData["Error"] =
                    "No tienes autorización para actualizar esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            var incidencia = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.Diagnosticos)
                .FirstOrDefaultAsync(i => i.IdIncidencia == id);

            if (incidencia == null)
            {
                TempData["Error"] = "No se encontró la incidencia.";
                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (incidencia.FechaCierre.HasValue ||
                incidencia.IdEstado < 6 ||
                incidencia.IdEstado >= 9)
            {
                TempData["Error"] =
                    "Esta incidencia no permite actualizar su estado.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            int siguienteEstado = incidencia.IdEstado + 1;

            var estadosDisponibles = await _contexto.Estados
                .Where(e =>
                    e.Modulo == "incidencia" &&
                    e.IdEstado == siguienteEstado)
                .ToListAsync();

            ViewBag.EstadosDisponibles = estadosDisponibles;

            return View(incidencia);
        }

        // Guardar el nuevo estado de una incidencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstado(
            int id,
            int idEstado)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            bool tieneAsignacion = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == id &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacion)
            {
                TempData["Error"] =
                    "No tienes autorización para modificar esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            var incidencia = await _contexto.Incidencias
                .FirstOrDefaultAsync(i => i.IdIncidencia == id);

            if (incidencia == null)
            {
                TempData["Error"] = "No se encontró la incidencia.";
                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (incidencia.FechaCierre.HasValue ||
                incidencia.IdEstado < 6 ||
                incidencia.IdEstado >= 9)
            {
                TempData["Error"] =
                    "La incidencia no permite más cambios de estado.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            int siguienteEstado = incidencia.IdEstado + 1;

            bool estadoValido = await _contexto.Estados
                .AnyAsync(e =>
                    e.IdEstado == idEstado &&
                    e.Modulo == "incidencia" &&
                    e.IdEstado == siguienteEstado);

            if (!estadoValido)
            {
                TempData["Error"] =
                    "El estado seleccionado no es válido para esta incidencia.";

                return RedirectToAction(nameof(ActualizarEstado), new { id });
            }

            if (idEstado == 9)
            {
                bool tieneDiagnostico = await _contexto.Diagnosticos
                    .AnyAsync(d => d.IdIncidencia == id);

                if (!tieneDiagnostico)
                {
                    TempData["Error"] =
                        "No puedes marcar la incidencia como resuelta porque aún no tiene un diagnóstico registrado.";

                    return RedirectToAction(nameof(ActualizarEstado), new { id });
                }
            }

            incidencia.IdEstado = idEstado;

            try
            {
                await _contexto.SaveChangesAsync();

                TempData["ConfirmacionActualizarEstado"] = true;
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No fue posible actualizar el estado. Inténtalo nuevamente.";
            }

            return RedirectToAction(nameof(ConsultarAsignaciones));
        }

        // Mostrar formulario para registrar el diagnóstico de una incidencia
        [HttpGet]
        public async Task<IActionResult> RegistrarDiagnostico(int id)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            bool tieneAsignacion = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == id &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacion)
            {
                TempData["Error"] =
                    "No tienes autorización para registrar el diagnóstico de esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            var incidencia = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdTipoIncidenciaNavigation)
                .FirstOrDefaultAsync(i => i.IdIncidencia == id);

            if (incidencia == null)
            {
                TempData["Error"] = "No se encontró la incidencia seleccionada.";
                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (incidencia.FechaCierre.HasValue ||
                incidencia.IdEstado < 6 ||
                incidencia.IdEstado >= 8)
            {
                TempData["Error"] =
                    "Esta incidencia no permite registrar un diagnóstico.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            return View(incidencia);
        }

        // Guardar el diagnóstico realizado por el técnico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarDiagnostico(
            int id,
            string hallazgos,
            string causaFalla,
            string? observaciones)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            bool tieneAsignacion = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == id &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacion)
            {
                TempData["Error"] =
                    "No tienes autorización para registrar el diagnóstico de esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            var incidencia = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdTipoIncidenciaNavigation)
                .FirstOrDefaultAsync(i => i.IdIncidencia == id);

            if (incidencia == null)
            {
                TempData["Error"] = "No se encontró la incidencia seleccionada.";
                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (incidencia.FechaCierre.HasValue ||
                incidencia.IdEstado < 6 ||
                incidencia.IdEstado >= 8)
            {
                TempData["Error"] =
                    "Esta incidencia no permite registrar un diagnóstico.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (string.IsNullOrWhiteSpace(hallazgos))
            {
                ModelState.AddModelError(
                    "hallazgos",
                    "Debes ingresar los hallazgos encontrados.");
            }

            if (string.IsNullOrWhiteSpace(causaFalla))
            {
                ModelState.AddModelError(
                    "causaFalla",
                    "Debes indicar la causa de la falla.");
            }

            if (!ModelState.IsValid)
                return View(incidencia);

            string resultado =
                $"HALLAZGOS:\n{hallazgos.Trim()}\n\n" +
                $"CAUSA DE LA FALLA:\n{causaFalla.Trim()}";

            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                resultado +=
                    $"\n\nOBSERVACIONES:\n{observaciones.Trim()}";
            }

            var diagnostico = new Diagnostico
            {
                IdIncidencia = incidencia.IdIncidencia,
                IdTecnico = idTecnico,
                FechaDiagnostico = DateTime.Now,
                Resultado = resultado
            };

            _contexto.Diagnosticos.Add(diagnostico);

            incidencia.IdEstado = 7;

            try
            {
                await _contexto.SaveChangesAsync();

                TempData["Exito"] =
                    "El diagnóstico fue registrado exitosamente.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No fue posible registrar el diagnóstico. Verifica los datos e inténtalo nuevamente.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }
        }

        // Consultar los detalles de una incidencia seleccionada
        [HttpGet]
        public async Task<IActionResult> ConsultarIncidencia(int id)
        {
            if (!EsAdministrador() && !EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var incidencia = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdUsuarioNavigation)
                .Include(i => i.AsignacionesIncidencia)
                    .ThenInclude(a => a.IdTecnicoNavigation)
                .Include(i => i.Diagnosticos)
                    .ThenInclude(d => d.IdTecnicoNavigation)
                .FirstOrDefaultAsync(i => i.IdIncidencia == id);

            if (incidencia == null)
            {
                TempData["Error"] = "No se encontró la incidencia solicitada.";
                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (EsTecnico())
            {
                int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

                bool tieneAsignacion = incidencia.AsignacionesIncidencia
                    .Any(a =>
                        a.IdTecnico == idTecnico &&
                        a.FechaFinalizacion == null);

                if (!tieneAsignacion)
                {
                    TempData["Error"] =
                        "No tienes autorización para consultar esta incidencia.";

                    return RedirectToAction(nameof(ConsultarAsignaciones));
                }
            }

            return View(incidencia);
        }

        // Consultar los detalles de un mantenimiento seleccionado
        [HttpGet]
        public async Task<IActionResult> ConsultarMantenimiento(int id)
        {
            if (!EsAdministrador() && !EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var mantenimiento = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .Include(m => m.IdIncidenciaNavigation)
                .FirstOrDefaultAsync(m => m.IdMantenimiento == id);

            if (mantenimiento == null)
            {
                TempData["Error"] = "No se encontró el mantenimiento solicitado.";
                return RedirectToAction(nameof(Consultar));
            }

            if (EsTecnico())
            {
                int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

                if (mantenimiento.IdTecnico != idTecnico)
                {
                    TempData["Error"] =
                        "No tienes autorización para consultar este mantenimiento.";

                    return RedirectToAction(nameof(ConsultarAsignaciones));
                }
            }

            return View(mantenimiento);
        }

        // Mostrar formulario para asignar un técnico
        [HttpGet]
        public async Task<IActionResult> Asignar(int id)
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var incidencia = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdUsuarioNavigation)
                .FirstOrDefaultAsync(i =>
                    i.IdIncidencia == id &&
                    i.IdEstado == 5 &&
                    i.FechaCierre == null &&
                    !i.AsignacionesIncidencia.Any(a =>
                        a.FechaFinalizacion == null));

            if (incidencia == null)
            {
                TempData["Error"] =
                    "La incidencia no existe o ya no está disponible para asignación.";

                return RedirectToAction(nameof(Index));
            }

            await CargarTecnicos();

            return View(incidencia);
        }

        // Guardar la asignación del técnico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int id, int idTecnico)
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var incidencia = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.IdUsuarioNavigation)
                .FirstOrDefaultAsync(i => i.IdIncidencia == id);

            if (incidencia == null ||
                incidencia.IdEstado != 5 ||
                incidencia.FechaCierre != null)
            {
                TempData["Error"] =
                    "La incidencia no está disponible para asignación.";

                return RedirectToAction(nameof(Index));
            }

            bool yaTieneTecnico = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == id &&
                    a.FechaFinalizacion == null);

            if (yaTieneTecnico)
            {
                TempData["Error"] =
                    "Esta incidencia ya tiene un técnico asignado.";

                return RedirectToAction(nameof(Index));
            }

            var tecnico = await _contexto.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idTecnico &&
                    u.IdRol == 2 &&
                    u.Estado);

            if (tecnico == null)
            {
                ModelState.AddModelError(
                    "idTecnico",
                    "Selecciona un técnico activo válido.");

                await CargarTecnicos();

                return View(incidencia);
            }

            var asignacion = new AsignacionesIncidencium
            {
                IdIncidencia = incidencia.IdIncidencia,
                IdTecnico = tecnico.IdUsuario,
                FechaAsignacion = DateTime.Now
            };

            incidencia.IdEstado = 6;

            _contexto.AsignacionesIncidencia.Add(asignacion);

            try
            {
                await _contexto.SaveChangesAsync();

                TempData["Exito"] =
                    "El técnico fue asignado correctamente a la incidencia.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No fue posible realizar la asignación. Es posible que otro administrador ya haya asignado esta incidencia.";

                return RedirectToAction(nameof(Index));
            }
        }

        // Mostrar formulario para programar un mantenimiento
        [HttpGet]
        public async Task<IActionResult> Programar()
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            await CargarDatosProgramacion();
            

            return View();
        }

        // Registrar un mantenimiento programado y asociarlo a una incidencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Programar(
            int idIncidencia,
            int idEquipo,
            int idTecnico,
            int idTipoMantenimiento,
            DateTime fechaProgramada,
            decimal duracionEstimadaHoras,
            string? descripcion)
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            if (fechaProgramada <= DateTime.Now)
            {
                ModelState.AddModelError(
                    "fechaProgramada",
                    "La fecha programada debe ser posterior a la fecha y hora actuales.");
            }

            if (duracionEstimadaHoras <= 0 ||
                duracionEstimadaHoras > 999.99m)
            {
                ModelState.AddModelError(
                    "duracionEstimadaHoras",
                    "La duración debe ser mayor que cero y no superar 999,99 horas.");
            }

            var asignacion = await _contexto.AsignacionesIncidencia
                .Include(a => a.IdIncidenciaNavigation)
                .FirstOrDefaultAsync(a =>
                    a.IdIncidencia == idIncidencia &&
                    a.FechaFinalizacion == null &&
                    a.IdIncidenciaNavigation.FechaCierre == null &&
                    (a.IdIncidenciaNavigation.IdEstado == 7 ||
                     a.IdIncidenciaNavigation.IdEstado == 8) &&
                    a.IdIncidenciaNavigation.Diagnosticos.Any());

            if (asignacion == null)
            {
                ModelState.AddModelError(
                    "idIncidencia",
                    "Selecciona una incidencia asignada, con diagnóstico y disponible para mantenimiento.");
            }
            else
            {
                if (asignacion.IdIncidenciaNavigation.IdEquipo != idEquipo)
                {
                    ModelState.AddModelError(
                        "idEquipo",
                        "El equipo seleccionado no corresponde a la incidencia.");
                }

                if (asignacion.IdTecnico != idTecnico)
                {
                    ModelState.AddModelError(
                        "idTecnico",
                        "El técnico seleccionado no corresponde a la asignación de la incidencia.");
                }

                bool yaTieneMantenimiento = await _contexto.Mantenimientos
                    .AnyAsync(m =>
                        m.IdIncidencia == idIncidencia &&
                        (m.IdEstado == 11 || m.IdEstado == 12));

                if (yaTieneMantenimiento)
                {
                    ModelState.AddModelError(
                        "idIncidencia",
                        "Esta incidencia ya tiene un mantenimiento programado o en proceso.");
                }
            }

            bool equipoValido = await _contexto.Equipos
                .AnyAsync(e =>
                    e.IdEquipo == idEquipo &&
                    e.IdEstado == 1);

            if (!equipoValido)
            {
                ModelState.AddModelError(
                    "idEquipo",
                    "Selecciona un equipo activo válido.");
            }

            bool tecnicoValido = await _contexto.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idTecnico &&
                    u.IdRol == 2 &&
                    u.Estado);

            if (!tecnicoValido)
            {
                ModelState.AddModelError(
                    "idTecnico",
                    "Selecciona un técnico activo válido.");
            }

            bool tipoValido = await _contexto.TiposMantenimientos
                .AnyAsync(t =>
                    t.IdTipoMantenimiento == idTipoMantenimiento);

            if (!tipoValido)
            {
                ModelState.AddModelError(
                    "idTipoMantenimiento",
                    "Selecciona un tipo de mantenimiento válido.");
            }

            DateTime fechaFinalizacion = fechaProgramada;

            if (duracionEstimadaHoras > 0 &&
                duracionEstimadaHoras <= 999.99m)
            {
                try
                {
                    fechaFinalizacion = fechaProgramada.AddHours(
                        (double)duracionEstimadaHoras);
                }
                catch (ArgumentOutOfRangeException)
                {
                    ModelState.AddModelError(
                        "duracionEstimadaHoras",
                        "La duración indicada genera una fecha de finalización no válida.");
                }
            }

            if (ModelState.IsValid)
            {
                var mantenimientosExistentes = await _contexto.Mantenimientos
                    .Where(m =>
                        m.IdEstado == 11 &&
                        (
                            m.IdTecnico == idTecnico ||
                            (idTipoMantenimiento == 1 &&
                             m.IdEquipo == idEquipo &&
                             m.IdTipoMantenimiento == 1)
                        ))
                    .ToListAsync();

                foreach (var existente in mantenimientosExistentes)
                {
                    if (!existente.DuracionEstimadaHoras.HasValue)
                    {
                        bool mismoDia =
                            existente.FechaProgramada.Date ==
                            fechaProgramada.Date;

                        if (mismoDia &&
                            existente.IdTecnico == idTecnico)
                        {
                            ModelState.AddModelError(
                                "fechaProgramada",
                                "El técnico ya tiene un mantenimiento programado ese día cuyo horario no se puede determinar.");
                        }

                        if (mismoDia &&
                            idTipoMantenimiento == 1 &&
                            existente.IdTipoMantenimiento == 1 &&
                            existente.IdEquipo == idEquipo)
                        {
                            ModelState.AddModelError(
                                "fechaProgramada",
                                "El equipo ya tiene un mantenimiento preventivo programado ese día cuyo horario no se puede determinar.");
                        }

                        continue;
                    }

                    DateTime inicioExistente = existente.FechaProgramada;
                    DateTime finExistente = inicioExistente.AddHours(
                        (double)existente.DuracionEstimadaHoras.Value);

                    bool hayCruce =
                        inicioExistente < fechaFinalizacion &&
                        finExistente > fechaProgramada;

                    if (!hayCruce)
                        continue;

                    if (existente.IdTecnico == idTecnico)
                    {
                        ModelState.AddModelError(
                            "fechaProgramada",
                            "El técnico ya tiene otro mantenimiento programado en ese horario.");
                    }

                    if (idTipoMantenimiento == 1 &&
                        existente.IdTipoMantenimiento == 1 &&
                        existente.IdEquipo == idEquipo)
                    {
                        ModelState.AddModelError(
                            "fechaProgramada",
                            "El equipo ya tiene otro mantenimiento preventivo programado en ese horario.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosProgramacion();

                return View(new Mantenimiento
                {
                    IdIncidencia = idIncidencia,
                    IdEquipo = idEquipo,
                    IdTecnico = idTecnico,
                    IdTipoMantenimiento = idTipoMantenimiento,
                    FechaProgramada = fechaProgramada,
                    DuracionEstimadaHoras = duracionEstimadaHoras,
                    Descripcion = descripcion
                });
            }

            var mantenimiento = new Mantenimiento
            {
                IdIncidencia = idIncidencia,
                IdEquipo = idEquipo,
                IdTecnico = idTecnico,
                IdTipoMantenimiento = idTipoMantenimiento,
                IdEstado = 11,
                FechaProgramada = fechaProgramada,
                DuracionEstimadaHoras = duracionEstimadaHoras,
                Descripcion = descripcion
            };

            _contexto.Mantenimientos.Add(mantenimiento);

            try
            {
                await _contexto.SaveChangesAsync();

                TempData["Exito"] =
                    "El mantenimiento fue programado correctamente.";

                return RedirectToAction(nameof(Consultar));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "No fue posible guardar el mantenimiento. Verifica los datos e inténtalo nuevamente.";

                await CargarDatosProgramacion();

                return View(mantenimiento);
            }
        }

        // Mostrar el formulario para registrar la ejecución de un mantenimiento
        [HttpGet]
        public async Task<IActionResult> RegistrarMantenimiento(int id)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            var mantenimiento = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .Include(m => m.IdIncidenciaNavigation)
                .FirstOrDefaultAsync(m =>
                    m.IdIncidencia == id &&
                    m.IdTecnico == idTecnico &&
                    m.IdEstado == 11 &&
                    m.FechaRealizacion == null);

            if (mantenimiento == null)
            {
                TempData["Error"] =
                    "No tienes un mantenimiento programado disponible para esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            bool tieneAsignacionActiva = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == id &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacionActiva)
            {
                TempData["Error"] =
                    "Ya no tienes una asignación activa para esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            // Cargar los repuestos temporales para mostrarlos en la vista.
            ViewBag.RepuestosTemporales =
                ObtenerRepuestosTemporales(mantenimiento.IdMantenimiento);

            return View(mantenimiento);
        }

        // Guardar la ejecución del mantenimiento

        // Guardar la ejecución del mantenimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMantenimiento(
    int idMantenimiento,
    string? actividades,
    string? resultado)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            var mantenimiento = await _contexto.Mantenimientos
                .FirstOrDefaultAsync(m =>
                    m.IdMantenimiento == idMantenimiento &&
                    m.IdTecnico == idTecnico &&
                    m.IdEstado == 11 &&
                    m.FechaRealizacion == null);

            if (mantenimiento == null)
            {
                TempData["Error"] =
                    "El mantenimiento no existe, ya fue registrado o no está asignado a tu usuario.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (!mantenimiento.IdIncidencia.HasValue)
            {
                TempData["Error"] =
                    "El mantenimiento no está asociado a una incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            int idIncidencia = mantenimiento.IdIncidencia.Value;

            bool tieneAsignacionActiva = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == idIncidencia &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacionActiva)
            {
                TempData["Error"] =
                    "No tienes una asignación activa para esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            bool tieneDiagnostico = await _contexto.Diagnosticos
                .AnyAsync(d => d.IdIncidencia == idIncidencia);

            if (!tieneDiagnostico)
            {
                TempData["Error"] =
                    "No se puede registrar el mantenimiento porque la incidencia no tiene un diagnóstico.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (string.IsNullOrWhiteSpace(actividades) ||
    string.IsNullOrWhiteSpace(resultado) ||
    !new[] {
        "Satisfactorio",
        "Con observaciones",
        "Requiere seguimiento"
    }.Contains(resultado))
            {
                TempData["Error"] =
                    "Debes ingresar las actividades y seleccionar un resultado válido.";

                return RedirectToAction(
                    nameof(RegistrarMantenimiento),
                    new { id = idIncidencia });
            }

            var repuestos = ObtenerRepuestosTemporales(idMantenimiento);

            await using var transaccion =
                await _contexto.Database.BeginTransactionAsync();

            try
            {
                mantenimiento.FechaRealizacion = DateTime.Now;
                mantenimiento.Descripcion = actividades.Trim();
                mantenimiento.Resultado = resultado.Trim();
                mantenimiento.IdEstado = 13;

                var repuestosAgrupados = repuestos
                    .GroupBy(r => r.Codigo.Trim().ToUpperInvariant())
                    .Select(g => new
                    {
                        Repuesto = g.First(),
                        Cantidad = g.Sum(r => r.Cantidad)
                    })
                    .ToList();

                foreach (var item in repuestosAgrupados)
                {
                    var datos = item.Repuesto;
                    string codigo = datos.Codigo.Trim();

                    var repuesto = await _contexto.Repuestos
                        .FirstOrDefaultAsync(r => r.Codigo == codigo);

                    if (repuesto == null)
                    {
                        repuesto = new Repuesto
                        {
                            Nombre = datos.Nombre.Trim(),
                            Codigo = codigo,
                            Marca = datos.Marca?.Trim(),
                            Modelo = datos.Modelo?.Trim(),
                            NumeroSerie = datos.NumeroSerie?.Trim(),
                            Observaciones = datos.Observaciones?.Trim()
                        };

                        _contexto.Repuestos.Add(repuesto);
                        await _contexto.SaveChangesAsync();
                    }

                    var registroExistente =
                        await _contexto.MantenimientoRepuestos
                            .FirstOrDefaultAsync(mr =>
                                mr.IdMantenimiento == idMantenimiento &&
                                mr.IdRepuesto == repuesto.IdRepuesto);

                    if (registroExistente != null)
                    {
                        registroExistente.Cantidad += item.Cantidad;
                        registroExistente.ValorUnitario = datos.ValorUnitario;
                        registroExistente.FechaInstalacion = datos.FechaInstalacion;
                        registroExistente.Observaciones = datos.Observaciones?.Trim();
                    }
                    else
                    {
                        var nuevoRegistro = new MantenimientoRepuesto
                        {
                            IdMantenimientoNavigation = mantenimiento,
                            IdRepuestoNavigation = repuesto,
                            Cantidad = item.Cantidad,
                            ValorUnitario = datos.ValorUnitario,
                            FechaInstalacion = datos.FechaInstalacion,
                            Observaciones = datos.Observaciones?.Trim()
                        };

                        _contexto.MantenimientoRepuestos.Add(nuevoRegistro);
                    }
                }

                await _contexto.SaveChangesAsync();
                await transaccion.CommitAsync();

                HttpContext.Session.Remove(
                    ObtenerClaveRepuestos(idMantenimiento));

                TempData["ConfirmacionRegistroMantenimiento"] = true;

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();

                TempData["Error"] =
                    "Error al guardar: " + ex.GetBaseException().Message;

                return RedirectToAction(
                    nameof(RegistrarMantenimiento),
                    new { id = idIncidencia });
            }
        }

        // Mostrar el formulario para registrar repuestos utilizados
        [HttpGet]
        public async Task<IActionResult> RegistrarRepuestos(int id)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            var mantenimiento = await _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(m => m.IdIncidenciaNavigation)
                .FirstOrDefaultAsync(m =>
                    m.IdMantenimiento == id &&
                    m.IdTecnico == idTecnico &&
                    m.IdEstado == 11 &&
                    m.FechaRealizacion == null);

            if (mantenimiento == null)
            {
                TempData["Error"] =
                    "No se encontró un mantenimiento programado disponible para registrar repuestos.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            if (!mantenimiento.IdIncidencia.HasValue)
            {
                TempData["Error"] =
                    "El mantenimiento no está asociado a una incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            bool tieneAsignacionActiva = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == mantenimiento.IdIncidencia.Value &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            if (!tieneAsignacionActiva)
            {
                TempData["Error"] =
                    "No tienes una asignación activa para esta incidencia.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            bool tieneDiagnostico = await _contexto.Diagnosticos
                .AnyAsync(d =>
                    d.IdIncidencia == mantenimiento.IdIncidencia.Value);

            if (!tieneDiagnostico)
            {
                TempData["Error"] =
                    "No puedes registrar repuestos porque la incidencia aún no tiene un diagnóstico.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            return View(mantenimiento);
        }

        // Agregar un repuesto temporalmente a la sesión del técnico.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarRepuestoTemporal(
            int idMantenimiento,
            string nombre,
            string codigo,
            string? marca,
            string? modelo,
            string? numeroSerie,
            int cantidad,
            decimal valorUnitario,
            DateOnly fechaInstalacion,
            string? observaciones)
        {
            if (!EsTecnico())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            int idTecnico = HttpContext.Session.GetInt32("UsuarioId")!.Value;

            var mantenimiento = await _contexto.Mantenimientos
                .FirstOrDefaultAsync(m =>
                    m.IdMantenimiento == idMantenimiento &&
                    m.IdTecnico == idTecnico &&
                    m.IdEstado == 11 &&
                    m.FechaRealizacion == null);

            if (mantenimiento == null ||
                !mantenimiento.IdIncidencia.HasValue)
            {
                TempData["Error"] =
                    "No se encontró un mantenimiento programado disponible.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            int idIncidencia = mantenimiento.IdIncidencia.Value;

            bool tieneAsignacionActiva = await _contexto.AsignacionesIncidencia
                .AnyAsync(a =>
                    a.IdIncidencia == idIncidencia &&
                    a.IdTecnico == idTecnico &&
                    a.FechaFinalizacion == null);

            bool tieneDiagnostico = await _contexto.Diagnosticos
                .AnyAsync(d => d.IdIncidencia == idIncidencia);

            if (!tieneAsignacionActiva || !tieneDiagnostico)
            {
                TempData["Error"] =
                    "No puedes agregar repuestos porque la asignación o el diagnóstico no están disponibles.";

                return RedirectToAction(nameof(ConsultarAsignaciones));
            }

            nombre = nombre?.Trim() ?? "";
            codigo = codigo?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(codigo) ||
                cantidad <= 0 ||
                valorUnitario <= 0 ||
                fechaInstalacion == default ||
                fechaInstalacion > DateOnly.FromDateTime(DateTime.Today))
            {
                TempData["Error"] =
                    "Verifica los datos del repuesto. La cantidad y el valor deben ser mayores que cero y la fecha no puede ser futura.";

                return RedirectToAction(
                    nameof(RegistrarRepuestos),
                    new { id = idMantenimiento });
            }

            var repuestos = ObtenerRepuestosTemporales(idMantenimiento);

            repuestos.Add(new RepuestoTemporal
            {
                Nombre = nombre,
                Codigo = codigo,
                Marca = marca?.Trim(),
                Modelo = modelo?.Trim(),
                NumeroSerie = numeroSerie?.Trim(),
                Cantidad = cantidad,
                ValorUnitario = valorUnitario,
                FechaInstalacion = fechaInstalacion,
                Observaciones = observaciones?.Trim()
            });

            GuardarRepuestosTemporales(idMantenimiento, repuestos);

            TempData["Exito"] = "Repuesto agregado temporalmente.";

            return RedirectToAction(
                nameof(RegistrarMantenimiento),
                new { id = idIncidencia });
        }


        // Cargar equipos, técnicos, tipos e incidencias para programar
        private async Task CargarDatosProgramacion()
        {
            ViewBag.Equipos = await _contexto.Equipos
                .Where(e => e.IdEstado == 1)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Tecnicos = await _contexto.Usuarios
                .Where(u => u.IdRol == 2 && u.Estado)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

            ViewBag.TiposMantenimiento = await _contexto.TiposMantenimientos
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            ViewBag.Incidencias = await _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                .Where(i =>
                    (i.IdEstado == 7 || i.IdEstado == 8) &&
                    i.FechaCierre == null &&
                    i.Diagnosticos.Any() &&
                    i.AsignacionesIncidencia.Any(a =>
                        a.FechaFinalizacion == null) &&
                    !i.Mantenimientos.Any(m =>
                        m.IdEstado == 11 || m.IdEstado == 12))
                .OrderByDescending(i => i.FechaRegistro)
                .ToListAsync();
        }

        // Cargar técnicos activos para el formulario de asignación
        private async Task CargarTecnicos()
        {
            ViewBag.Tecnicos = await _contexto.Usuarios
                .Where(u => u.IdRol == 2 && u.Estado)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();
        }
    }
}
