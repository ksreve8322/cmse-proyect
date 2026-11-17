
using cmse.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cmse.Controllers
{
    public class EquipoController : Controller
    {
        private readonly CmseContext _context;

        public EquipoController(CmseContext context)
        {
            _context = context;
        }

        private bool TieneSesion()
        {
            return HttpContext.Session.GetInt32("UsuarioId").HasValue;
        }

        private bool EsAdministrador()
        {
            return HttpContext.Session.GetInt32("RolId") == 1;
        }

        private IActionResult? ValidarAdministrador()
        {
            if (!TieneSesion())
                return RedirectToAction("Login", "Account");

            if (!EsAdministrador())
                return RedirectToAction(nameof(Index));

            return null;
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

        // =========================================================
        // M02 - LISTAR EQUIPOS (TODOS LOS ROLES)
        // =========================================================

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

            pagina = Math.Max(1, Math.Min(
                pagina, Math.Max(1, totalPaginas)));

            var equipos = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;

            return View(equipos);
        }

        // =========================================================
        // M03 - CONSULTAR EQUIPO (TODOS LOS ROLES)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!TieneSesion())
                return RedirectToAction("Login", "Account");

            var equipo = await _context.Equipos
                .Include(e => e.IdTipoEquipoNavigation)
                .Include(e => e.IdEstadoNavigation)
                .Include(e => e.EquiposComputadore)
                    .ThenInclude(c => c!.AlmacenamientosEquipos)
                .Include(e => e.EquiposImpresora)
                .Include(e => e.EquiposPantalla)
                .Include(e => e.EquiposProyectore)
                .Include(e => e.Incidencia)
                    .ThenInclude(i => i.IdEstadoNavigation)
                .Include(e => e.Incidencia)
                    .ThenInclude(i => i.IdTipoIncidenciaNavigation)
                .Include(e => e.Incidencia)
                    .ThenInclude(i => i.AsignacionesIncidencia)
                        .ThenInclude(a => a.IdTecnicoNavigation)
                .FirstOrDefaultAsync(e => e.IdEquipo == id);

            if (equipo == null)
                return NotFound();

            return View(equipo);
        }

        // =========================================================
        // M04 - ACTUALIZAR EQUIPO (SOLO ADMINISTRADOR)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var acceso = ValidarAdministrador();
            if (acceso != null)
                return acceso;

            var equipo = await ObtenerEquipoCompletoAsync(id);

            if (equipo == null)
                return NotFound();

            await CargarListasAsync();

            return View("Edit", equipo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEdicion(int id, Equipo equipo)
        {
            var acceso = ValidarAdministrador();
            if (acceso != null)
                return acceso;

            var equipoExistente = await ObtenerEquipoCompletoAsync(id);

            if (equipoExistente == null)
                return NotFound();

            ModelState.Remove(nameof(Equipo.IdEquipo));
            ModelState.Remove(nameof(Equipo.IdTipoEquipo));
            ModelState.Remove(nameof(Equipo.CodigoInventario));
            ModelState.Remove(nameof(Equipo.FechaRegistro));
            ModelState.Remove(nameof(Equipo.IdEstadoNavigation));
            ModelState.Remove(nameof(Equipo.IdTipoEquipoNavigation));
            ModelState.Remove(nameof(Equipo.Incidencia));
            ModelState.Remove(nameof(Equipo.Mantenimientos));

            foreach (var clave in ModelState.Keys
                .Where(k => k.EndsWith("Navigation", StringComparison.Ordinal))
                .ToList())
            {
                ModelState.Remove(clave);
            }
            // Las especificaciones se validan según el tipo de equipo.
            ModelState.Remove(nameof(Equipo.EquiposComputadore));
            ModelState.Remove(nameof(Equipo.EquiposImpresora));
            ModelState.Remove(nameof(Equipo.EquiposPantalla));
            ModelState.Remove(nameof(Equipo.EquiposProyectore));

            equipo.Nombre = equipo.Nombre?.Trim() ?? "";
            equipo.Marca = equipo.Marca?.Trim();
            equipo.Modelo = equipo.Modelo?.Trim();
            equipo.NumeroSerie = string.IsNullOrWhiteSpace(equipo.NumeroSerie)
                ? null
                : equipo.NumeroSerie.Trim();
            equipo.Ubicacion = equipo.Ubicacion?.Trim();
            equipo.Observaciones = equipo.Observaciones?.Trim();

            bool estadoValido = await _context.Estados.AnyAsync(e =>
                e.IdEstado == equipo.IdEstado &&
                e.Modulo != null &&
                (e.Modulo.ToLower() == "equipo" ||
                 e.Modulo.ToLower() == "equipos"));

            if (!estadoValido)
            {
                ModelState.AddModelError(
                    nameof(equipo.IdEstado),
                    "Selecciona un estado válido para equipos.");
            }

            if (equipo.NumeroSerie != null &&
                await _context.Equipos.AnyAsync(e =>
                    e.IdEquipo != id &&
                    e.NumeroSerie == equipo.NumeroSerie))
            {
                ModelState.AddModelError(
                    nameof(equipo.NumeroSerie),
                    "Ese número de serie ya está registrado en otro equipo.");
            }

            switch (equipoExistente.IdTipoEquipo)
            {
                case 1:
                case 2:
                    {
                        var computador = equipo.EquiposComputadore;

                        if (computador == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "No se recibieron las especificaciones del computador.");
                            break;
                        }

                        computador.Procesador = computador.Procesador?.Trim() ?? "";
                        computador.TipoRam = computador.TipoRam?.Trim() ?? "";
                        computador.Gpu = computador.Gpu?.Trim() ?? "";
                        computador.SistemaOperativo =
                            computador.SistemaOperativo?.Trim() ?? "";

                        if (string.IsNullOrWhiteSpace(computador.Procesador))
                            ModelState.AddModelError(
                                "EquiposComputadore.Procesador",
                                "El procesador es obligatorio.");

                        if (computador.CapacidadRamGb <= 0)
                            ModelState.AddModelError(
                                "EquiposComputadore.CapacidadRamGb",
                                "La capacidad de RAM debe ser mayor que cero.");

                        if (string.IsNullOrWhiteSpace(computador.TipoRam))
                            ModelState.AddModelError(
                                "EquiposComputadore.TipoRam",
                                "El tipo de RAM es obligatorio.");

                        if (string.IsNullOrWhiteSpace(computador.Gpu))
                            ModelState.AddModelError(
                                "EquiposComputadore.Gpu",
                                "La GPU es obligatoria.");

                        if (string.IsNullOrWhiteSpace(computador.SistemaOperativo))
                            ModelState.AddModelError(
                                "EquiposComputadore.SistemaOperativo",
                                "El sistema operativo es obligatorio.");

                        var almacenamientos = computador.AlmacenamientosEquipos
                            ?? new List<AlmacenamientosEquipo>();

                        var filasConDatos = almacenamientos.Where(a =>
                            a.Capacidad != 0 ||
                            !string.IsNullOrWhiteSpace(a.Unidad) ||
                            !string.IsNullOrWhiteSpace(a.TipoAlmacenamiento))
                            .ToList();

                        if (filasConDatos.Count == 0)
                        {
                            ModelState.AddModelError(
                                "",
                                "Debes conservar o registrar al menos un almacenamiento.");
                        }

                        foreach (var almacenamiento in filasConDatos)
                        {
                            if (almacenamiento.Capacidad <= 0)
                                ModelState.AddModelError(
                                    "",
                                    "La capacidad de cada almacenamiento debe ser mayor que cero.");

                            if (string.IsNullOrWhiteSpace(almacenamiento.Unidad))
                                ModelState.AddModelError(
                                    "",
                                    "Debes indicar la unidad de cada almacenamiento.");

                            if (string.IsNullOrWhiteSpace(
                                almacenamiento.TipoAlmacenamiento))
                            {
                                ModelState.AddModelError(
                                    "",
                                    "Debes indicar el tipo de cada almacenamiento.");
                            }
                        }

                        var idsEnviados = filasConDatos
                            .Where(a => a.IdAlmacenamiento > 0)
                            .Select(a => a.IdAlmacenamiento)
                            .ToList();

                        if (idsEnviados.Count != idsEnviados.Distinct().Count())
                        {
                            ModelState.AddModelError(
                                "",
                                "Hay unidades de almacenamiento duplicadas.");
                        }

                        var idsExistentes = equipoExistente.EquiposComputadore?
                            .AlmacenamientosEquipos
                            .Select(a => a.IdAlmacenamiento)
                            .ToHashSet() ?? new HashSet<int>();

                        if (idsEnviados.Any(idAlmacenamiento =>
                            !idsExistentes.Contains(idAlmacenamiento)))
                        {
                            ModelState.AddModelError(
                                "",
                                "Se encontró un almacenamiento que no pertenece a este equipo.");
                        }

                        break;
                    }

                case 3:
                    {
                        var proyector = equipo.EquiposProyectore;

                        if (proyector == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "No se recibieron las especificaciones del proyector.");
                            break;
                        }

                        proyector.TecnologiaProyeccion =
                            proyector.TecnologiaProyeccion?.Trim() ?? "";
                        proyector.FuenteIluminacion =
                            proyector.FuenteIluminacion?.Trim() ?? "";
                        proyector.ResolucionNativa =
                            proyector.ResolucionNativa?.Trim() ?? "";

                        if (string.IsNullOrWhiteSpace(proyector.TecnologiaProyeccion))
                            ModelState.AddModelError(
                                "EquiposProyectore.TecnologiaProyeccion",
                                "La tecnología de proyección es obligatoria.");

                        if (string.IsNullOrWhiteSpace(proyector.FuenteIluminacion))
                            ModelState.AddModelError(
                                "EquiposProyectore.FuenteIluminacion",
                                "La fuente de iluminación es obligatoria.");

                        if (string.IsNullOrWhiteSpace(proyector.ResolucionNativa))
                            ModelState.AddModelError(
                                "EquiposProyectore.ResolucionNativa",
                                "La resolución nativa es obligatoria.");

                        if (proyector.BrilloLumenes <= 0)
                            ModelState.AddModelError(
                                "EquiposProyectore.BrilloLumenes",
                                "El brillo debe ser mayor que cero.");

                        break;
                    }

                case 4:
                    {
                        var pantalla = equipo.EquiposPantalla;

                        if (pantalla == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "No se recibieron las especificaciones de la pantalla.");
                            break;
                        }

                        pantalla.Resolucion = pantalla.Resolucion?.Trim() ?? "";
                        pantalla.TecnologiaPanel =
                            pantalla.TecnologiaPanel?.Trim() ?? "";

                        if (pantalla.TamanoPulgadas <= 0)
                            ModelState.AddModelError(
                                "EquiposPantalla.TamanoPulgadas",
                                "El tamaño debe ser mayor que cero.");

                        if (string.IsNullOrWhiteSpace(pantalla.Resolucion))
                            ModelState.AddModelError(
                                "EquiposPantalla.Resolucion",
                                "La resolución es obligatoria.");

                        if (pantalla.FrecuenciaActualizacion <= 0)
                            ModelState.AddModelError(
                                "EquiposPantalla.FrecuenciaActualizacion",
                                "La frecuencia debe ser mayor que cero.");

                        if (string.IsNullOrWhiteSpace(pantalla.TecnologiaPanel))
                            ModelState.AddModelError(
                                "EquiposPantalla.TecnologiaPanel",
                                "La tecnología del panel es obligatoria.");

                        break;
                    }

                case 5:
                    {
                        var impresora = equipo.EquiposImpresora;

                        if (impresora == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "No se recibieron las especificaciones de la impresora.");
                            break;
                        }

                        impresora.TipoImpresora =
                            impresora.TipoImpresora?.Trim() ?? "";
                        impresora.ResolucionImpresion =
                            impresora.ResolucionImpresion?.Trim() ?? "";

                        if (string.IsNullOrWhiteSpace(impresora.TipoImpresora))
                            ModelState.AddModelError(
                                "EquiposImpresora.TipoImpresora",
                                "El tipo de impresora es obligatorio.");

                        if (string.IsNullOrWhiteSpace(
                            impresora.ResolucionImpresion))
                        {
                            ModelState.AddModelError(
                                "EquiposImpresora.ResolucionImpresion",
                                "La resolución de impresión es obligatoria.");
                        }

                        break;
                    }
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync();

                equipo.IdEquipo = equipoExistente.IdEquipo;
                equipo.IdTipoEquipo = equipoExistente.IdTipoEquipo;
                equipo.CodigoInventario = equipoExistente.CodigoInventario;
                equipo.IdTipoEquipoNavigation =
                    equipoExistente.IdTipoEquipoNavigation;

                return View("Edit", equipo);
            }

            equipoExistente.Nombre = equipo.Nombre;
            equipoExistente.IdEstado = equipo.IdEstado;
            equipoExistente.Marca = equipo.Marca;
            equipoExistente.Modelo = equipo.Modelo;
            equipoExistente.NumeroSerie = equipo.NumeroSerie;
            equipoExistente.Ubicacion = equipo.Ubicacion;
            equipoExistente.Observaciones = equipo.Observaciones;

            if (equipoExistente.IdTipoEquipo == 1 ||
                equipoExistente.IdTipoEquipo == 2)
            {
                var datos = equipo.EquiposComputadore!;
                var computador = equipoExistente.EquiposComputadore;

                if (computador == null)
                {
                    computador = new EquiposComputadore();
                    equipoExistente.EquiposComputadore = computador;
                }

                computador.Procesador = datos.Procesador.Trim();
                computador.CapacidadRamGb = datos.CapacidadRamGb;
                computador.TipoRam = datos.TipoRam.Trim();
                computador.Gpu = datos.Gpu.Trim();
                computador.SistemaOperativo = datos.SistemaOperativo.Trim();
                computador.Refrigeracion = datos.Refrigeracion?.Trim();
                computador.Puertos = datos.Puertos?.Trim();
                computador.FuentePoder = datos.FuentePoder?.Trim();

                if (equipoExistente.IdTipoEquipo == 2)
                {
                    computador.Bateria = datos.Bateria?.Trim();
                    computador.Pantalla = datos.Pantalla?.Trim();
                }
                else
                {
                    computador.Bateria = null;
                    computador.Pantalla = null;
                }

                var almacenamientosRecibidos =
                    (datos.AlmacenamientosEquipos
                        ?? new List<AlmacenamientosEquipo>())
                    .Where(a =>
                        a.Capacidad != 0 ||
                        !string.IsNullOrWhiteSpace(a.Unidad) ||
                        !string.IsNullOrWhiteSpace(a.TipoAlmacenamiento))
                    .ToList();

                var almacenamientosActuales =
                    computador.AlmacenamientosEquipos.ToList();

                var idsRecibidos = almacenamientosRecibidos
                    .Where(a => a.IdAlmacenamiento > 0)
                    .Select(a => a.IdAlmacenamiento)
                    .ToHashSet();

                foreach (var actual in almacenamientosActuales)
                {
                    if (!idsRecibidos.Contains(actual.IdAlmacenamiento))
                    {
                        _context.Set<AlmacenamientosEquipo>().Remove(actual);
                    }
                }

                foreach (var recibido in almacenamientosRecibidos)
                {
                    if (recibido.IdAlmacenamiento > 0)
                    {
                        var existente = almacenamientosActuales
                            .FirstOrDefault(a =>
                                a.IdAlmacenamiento ==
                                recibido.IdAlmacenamiento);

                        if (existente != null)
                        {
                            existente.Capacidad = recibido.Capacidad;
                            existente.Unidad = recibido.Unidad.Trim();
                            existente.TipoAlmacenamiento =
                                recibido.TipoAlmacenamiento.Trim();
                        }
                    }
                    else
                    {
                        computador.AlmacenamientosEquipos.Add(
                            new AlmacenamientosEquipo
                            {
                                Capacidad = recibido.Capacidad,
                                Unidad = recibido.Unidad.Trim(),
                                TipoAlmacenamiento =
                                    recibido.TipoAlmacenamiento.Trim(),
                                IdEquipoNavigation = computador
                            });
                    }
                }
            }

            if (equipoExistente.IdTipoEquipo == 3)
            {
                var datos = equipo.EquiposProyectore!;
                var proyector = equipoExistente.EquiposProyectore;

                if (proyector == null)
                {
                    proyector = new EquiposProyectore();
                    equipoExistente.EquiposProyectore = proyector;
                }

                proyector.TecnologiaProyeccion =
                    datos.TecnologiaProyeccion.Trim();
                proyector.FuenteIluminacion =
                    datos.FuenteIluminacion.Trim();
                proyector.ResolucionNativa =
                    datos.ResolucionNativa.Trim();
                proyector.ResolucionMaxima = datos.ResolucionMaxima?.Trim();
                proyector.BrilloLumenes = datos.BrilloLumenes;
                proyector.RelacionContraste = datos.RelacionContraste?.Trim();
                proyector.RelacionAspecto = datos.RelacionAspecto?.Trim();
                proyector.TamanoProyeccion = datos.TamanoProyeccion?.Trim();
                proyector.DistanciaProyeccion =
                    datos.DistanciaProyeccion?.Trim();
                proyector.TipoLente = datos.TipoLente?.Trim();
                proyector.CorreccionTrapezoidal =
                    datos.CorreccionTrapezoidal?.Trim();
                proyector.Puertos = datos.Puertos?.Trim();
                proyector.HorasUso = datos.HorasUso;
                proyector.AltavocesIntegrados = datos.AltavocesIntegrados;
            }

            if (equipoExistente.IdTipoEquipo == 4)
            {
                var datos = equipo.EquiposPantalla!;
                var pantalla = equipoExistente.EquiposPantalla;

                if (pantalla == null)
                {
                    pantalla = new EquiposPantalla();
                    equipoExistente.EquiposPantalla = pantalla;
                }

                pantalla.TamanoPulgadas = datos.TamanoPulgadas;
                pantalla.Resolucion = datos.Resolucion.Trim();
                pantalla.FrecuenciaActualizacion =
                    datos.FrecuenciaActualizacion;
                pantalla.TecnologiaPanel = datos.TecnologiaPanel.Trim();
                pantalla.Hdr = datos.Hdr;
                pantalla.SistemaOperativo = datos.SistemaOperativo?.Trim();
                pantalla.Puertos = datos.Puertos?.Trim();
            }

            if (equipoExistente.IdTipoEquipo == 5)
            {
                var datos = equipo.EquiposImpresora!;
                var impresora = equipoExistente.EquiposImpresora;

                if (impresora == null)
                {
                    impresora = new EquiposImpresora();
                    equipoExistente.EquiposImpresora = impresora;
                }

                impresora.TipoImpresora = datos.TipoImpresora.Trim();
                impresora.ResolucionImpresion =
                    datos.ResolucionImpresion.Trim();
                impresora.FormatosSoportados =
                    datos.FormatosSoportados?.Trim();
                impresora.ImpresionColor = datos.ImpresionColor;
                impresora.Puertos = datos.Puertos?.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Equipo actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // M05 - REGISTRAR EQUIPO (SOLO ADMINISTRADOR)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var acceso = ValidarAdministrador();
            if (acceso != null)
                return acceso;

            await CargarListasAsync();

            return View(new Equipo());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipo equipo)
        {
            var acceso = ValidarAdministrador();
            if (acceso != null)
                return acceso;

            ModelState.Remove(nameof(Equipo.IdEquipo));
            ModelState.Remove(nameof(Equipo.FechaRegistro));
            ModelState.Remove(nameof(Equipo.IdEstadoNavigation));
            ModelState.Remove(nameof(Equipo.IdTipoEquipoNavigation));
            ModelState.Remove(nameof(Equipo.Incidencia));
            ModelState.Remove(nameof(Equipo.Mantenimientos));

            foreach (var clave in ModelState.Keys
                .Where(k => k.EndsWith("Navigation", StringComparison.Ordinal))
                .ToList())
            {
                ModelState.Remove(clave);
            }
            var prefijosNoSeleccionados = equipo.IdTipoEquipo switch
            {
                1 or 2 => new[] { "EquiposImpresora", "EquiposPantalla", "EquiposProyectore" },
                3 => new[] { "EquiposComputadore", "EquiposImpresora", "EquiposPantalla" },
                4 => new[] { "EquiposComputadore", "EquiposImpresora", "EquiposProyectore" },
                5 => new[] { "EquiposComputadore", "EquiposPantalla", "EquiposProyectore" },
                _ => new[] { "EquiposComputadore", "EquiposImpresora", "EquiposPantalla", "EquiposProyectore" }
            };

            foreach (var prefijo in prefijosNoSeleccionados)
            {
                foreach (var clave in ModelState.Keys
                    .Where(k => k.Equals(prefijo, StringComparison.OrdinalIgnoreCase) ||
                                k.StartsWith(prefijo + ".", StringComparison.OrdinalIgnoreCase))
                    .ToList())
                {
                    ModelState.Remove(clave);
                }
            }
            // Evitar validar especificaciones de tipos de equipo no seleccionados.
            ModelState.Remove(nameof(Equipo.EquiposComputadore));
            ModelState.Remove(nameof(Equipo.EquiposImpresora));
            ModelState.Remove(nameof(Equipo.EquiposPantalla));
            ModelState.Remove(nameof(Equipo.EquiposProyectore));

            equipo.Nombre = equipo.Nombre?.Trim() ?? "";
            equipo.CodigoInventario =
                equipo.CodigoInventario?.Trim() ?? "";
            equipo.Marca = equipo.Marca?.Trim();
            equipo.Modelo = equipo.Modelo?.Trim();

            equipo.NumeroSerie = string.IsNullOrWhiteSpace(equipo.NumeroSerie)
                ? null
                : equipo.NumeroSerie.Trim();

            equipo.Ubicacion = equipo.Ubicacion?.Trim();
            equipo.Observaciones = equipo.Observaciones?.Trim();

            bool tipoValido = await _context.TiposEquipos.AnyAsync(
                t => t.IdTipoEquipo == equipo.IdTipoEquipo);

            if (!tipoValido)
            {
                ModelState.AddModelError(
                    nameof(equipo.IdTipoEquipo),
                    "Selecciona un tipo de equipo válido.");
            }

            bool estadoValido = await _context.Estados.AnyAsync(e =>
                e.IdEstado == equipo.IdEstado &&
                e.Modulo != null &&
                (e.Modulo.ToLower() == "equipo" ||
                 e.Modulo.ToLower() == "equipos"));

            if (!estadoValido)
            {
                ModelState.AddModelError(
                    nameof(equipo.IdEstado),
                    "Selecciona un estado válido para equipos.");
            }

            if (!string.IsNullOrWhiteSpace(equipo.CodigoInventario) &&
                await _context.Equipos.AnyAsync(
                    e => e.CodigoInventario == equipo.CodigoInventario))
            {
                ModelState.AddModelError(
                    nameof(equipo.CodigoInventario),
                    "Ese código de inventario ya existe.");
            }

            if (equipo.NumeroSerie != null &&
                await _context.Equipos.AnyAsync(
                    e => e.NumeroSerie == equipo.NumeroSerie))
            {
                ModelState.AddModelError(
                    nameof(equipo.NumeroSerie),
                    "Ese número de serie ya está registrado.");
            }

            switch (equipo.IdTipoEquipo)
            {
                case 1:
                case 2:
                    {
                        var computador = equipo.EquiposComputadore;

                        if (computador == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "Debes completar las especificaciones del computador.");
                            break;
                        }

                        computador.Procesador =
                            computador.Procesador?.Trim() ?? "";
                        computador.TipoRam = computador.TipoRam?.Trim() ?? "";
                        computador.Gpu = computador.Gpu?.Trim() ?? "";
                        computador.SistemaOperativo =
                            computador.SistemaOperativo?.Trim() ?? "";

                        var almacenamientos =
                            (computador.AlmacenamientosEquipos
                                ?? new List<AlmacenamientosEquipo>())
                            .Where(a => a.Capacidad > 0 &&
                                !string.IsNullOrWhiteSpace(a.Unidad) &&
                                !string.IsNullOrWhiteSpace(a.TipoAlmacenamiento))
                            .ToList();

                        if (string.IsNullOrWhiteSpace(computador.Procesador))
                            ModelState.AddModelError(
                                "EquiposComputadore.Procesador",
                                "El procesador es obligatorio.");

                        if (computador.CapacidadRamGb <= 0)
                            ModelState.AddModelError(
                                "EquiposComputadore.CapacidadRamGb",
                                "La capacidad de RAM debe ser mayor que cero.");

                        if (string.IsNullOrWhiteSpace(computador.TipoRam))
                            ModelState.AddModelError(
                                "EquiposComputadore.TipoRam",
                                "El tipo de RAM es obligatorio.");

                        if (string.IsNullOrWhiteSpace(computador.Gpu))
                            ModelState.AddModelError(
                                "EquiposComputadore.Gpu",
                                "La GPU es obligatoria.");

                        if (string.IsNullOrWhiteSpace(
                            computador.SistemaOperativo))
                        {
                            ModelState.AddModelError(
                                "EquiposComputadore.SistemaOperativo",
                                "El sistema operativo es obligatorio.");
                        }

                        if (almacenamientos.Count == 0)
                        {
                            ModelState.AddModelError(
                                "",
                                "Debes registrar al menos una unidad de almacenamiento.");
                        }

                        break;
                    }

                case 3:
                    {
                        var proyector = equipo.EquiposProyectore;

                        if (proyector == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "Debes completar las especificaciones del proyector.");
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(
                            proyector.TecnologiaProyeccion))
                            ModelState.AddModelError(
                                "EquiposProyectore.TecnologiaProyeccion",
                                "La tecnología de proyección es obligatoria.");

                        if (string.IsNullOrWhiteSpace(
                            proyector.FuenteIluminacion))
                            ModelState.AddModelError(
                                "EquiposProyectore.FuenteIluminacion",
                                "La fuente de iluminación es obligatoria.");

                        if (string.IsNullOrWhiteSpace(
                            proyector.ResolucionNativa))
                            ModelState.AddModelError(
                                "EquiposProyectore.ResolucionNativa",
                                "La resolución nativa es obligatoria.");

                        if (proyector.BrilloLumenes <= 0)
                            ModelState.AddModelError(
                                "EquiposProyectore.BrilloLumenes",
                                "El brillo debe ser mayor que cero.");

                        break;
                    }

                case 4:
                    {
                        var pantalla = equipo.EquiposPantalla;

                        if (pantalla == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "Debes completar las especificaciones de la pantalla.");
                            break;
                        }

                        if (pantalla.TamanoPulgadas <= 0)
                            ModelState.AddModelError(
                                "EquiposPantalla.TamanoPulgadas",
                                "El tamaño debe ser mayor que cero.");

                        if (string.IsNullOrWhiteSpace(pantalla.Resolucion))
                            ModelState.AddModelError(
                                "EquiposPantalla.Resolucion",
                                "La resolución es obligatoria.");

                        if (pantalla.FrecuenciaActualizacion <= 0)
                            ModelState.AddModelError(
                                "EquiposPantalla.FrecuenciaActualizacion",
                                "La frecuencia debe ser mayor que cero.");

                        if (string.IsNullOrWhiteSpace(pantalla.TecnologiaPanel))
                            ModelState.AddModelError(
                                "EquiposPantalla.TecnologiaPanel",
                                "La tecnología del panel es obligatoria.");

                        break;
                    }

                case 5:
                    {
                        var impresora = equipo.EquiposImpresora;

                        if (impresora == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "Debes completar las especificaciones de la impresora.");
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(impresora.TipoImpresora))
                            ModelState.AddModelError(
                                "EquiposImpresora.TipoImpresora",
                                "El tipo de impresora es obligatorio.");

                        if (string.IsNullOrWhiteSpace(
                            impresora.ResolucionImpresion))
                        {
                            ModelState.AddModelError(
                                "EquiposImpresora.ResolucionImpresion",
                                "La resolución de impresión es obligatoria.");
                        }

                        break;
                    }
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync();
                return View(equipo);
            }

            equipo.FechaRegistro =
                DateOnly.FromDateTime(DateTime.Today);

            switch (equipo.IdTipoEquipo)
            {
                case 1:
                case 2:
                    {
                        var datos = equipo.EquiposComputadore!;

                        var computador = new EquiposComputadore
                        {
                            Procesador = datos.Procesador.Trim(),
                            CapacidadRamGb = datos.CapacidadRamGb,
                            TipoRam = datos.TipoRam.Trim(),
                            Gpu = datos.Gpu.Trim(),
                            SistemaOperativo = datos.SistemaOperativo.Trim(),
                            Refrigeracion = datos.Refrigeracion,
                            Puertos = datos.Puertos,
                            FuentePoder = datos.FuentePoder,
                            Bateria = equipo.IdTipoEquipo == 2
                                ? datos.Bateria
                                : null,
                            Pantalla = equipo.IdTipoEquipo == 2
                                ? datos.Pantalla
                                : null,
                            IdEquipoNavigation = equipo
                        };

                        foreach (var almacenamiento in
                            (datos.AlmacenamientosEquipos
                                ?? new List<AlmacenamientosEquipo>())
                            .Where(a => a.Capacidad > 0 &&
                                !string.IsNullOrWhiteSpace(a.Unidad) &&
                                !string.IsNullOrWhiteSpace(a.TipoAlmacenamiento)))
                        {
                            computador.AlmacenamientosEquipos.Add(
                                new AlmacenamientosEquipo
                                {
                                    Capacidad = almacenamiento.Capacidad,
                                    Unidad = almacenamiento.Unidad.Trim(),
                                    TipoAlmacenamiento =
                                        almacenamiento.TipoAlmacenamiento.Trim(),
                                    IdEquipoNavigation = computador
                                });
                        }

                        equipo.EquiposComputadore = computador;
                        break;
                    }

                case 3:
                    {
                        var datos = equipo.EquiposProyectore!;

                        equipo.EquiposProyectore = new EquiposProyectore
                        {
                            TecnologiaProyeccion =
                                datos.TecnologiaProyeccion.Trim(),
                            FuenteIluminacion =
                                datos.FuenteIluminacion.Trim(),
                            ResolucionNativa = datos.ResolucionNativa.Trim(),
                            ResolucionMaxima = datos.ResolucionMaxima,
                            BrilloLumenes = datos.BrilloLumenes,
                            RelacionContraste = datos.RelacionContraste,
                            RelacionAspecto = datos.RelacionAspecto,
                            TamanoProyeccion = datos.TamanoProyeccion,
                            DistanciaProyeccion = datos.DistanciaProyeccion,
                            TipoLente = datos.TipoLente,
                            CorreccionTrapezoidal =
                                datos.CorreccionTrapezoidal,
                            Puertos = datos.Puertos,
                            HorasUso = datos.HorasUso,
                            AltavocesIntegrados = datos.AltavocesIntegrados,
                            IdEquipoNavigation = equipo
                        };

                        break;
                    }

                case 4:
                    {
                        var datos = equipo.EquiposPantalla!;

                        equipo.EquiposPantalla = new EquiposPantalla
                        {
                            TamanoPulgadas = datos.TamanoPulgadas,
                            Resolucion = datos.Resolucion.Trim(),
                            FrecuenciaActualizacion =
                                datos.FrecuenciaActualizacion,
                            TecnologiaPanel = datos.TecnologiaPanel.Trim(),
                            Hdr = datos.Hdr,
                            SistemaOperativo = datos.SistemaOperativo,
                            Puertos = datos.Puertos,
                            IdEquipoNavigation = equipo
                        };

                        break;
                    }

                case 5:
                    {
                        var datos = equipo.EquiposImpresora!;

                        equipo.EquiposImpresora = new EquiposImpresora
                        {
                            TipoImpresora = datos.TipoImpresora.Trim(),
                            ResolucionImpresion =
                                datos.ResolucionImpresion.Trim(),
                            FormatosSoportados = datos.FormatosSoportados,
                            ImpresionColor = datos.ImpresionColor,
                            Puertos = datos.Puertos,
                            IdEquipoNavigation = equipo
                        };

                        break;
                    }
            }

            _context.Equipos.Add(equipo);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Equipo registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MÉTODO AUXILIAR - OBTENER EQUIPO COMPLETO
        // =========================================================

        private async Task<Equipo?> ObtenerEquipoCompletoAsync(int id)
        {
            return await _context.Equipos
                .Include(e => e.IdTipoEquipoNavigation)
                .Include(e => e.IdEstadoNavigation)
                .Include(e => e.EquiposComputadore)
                    .ThenInclude(c => c!.AlmacenamientosEquipos)
                .Include(e => e.EquiposImpresora)
                .Include(e => e.EquiposPantalla)
                .Include(e => e.EquiposProyectore)
                .FirstOrDefaultAsync(e => e.IdEquipo == id);
        }

        // =========================================================
        // M07 - CONFIRMAR ELIMINACIÓN (SOLO ADMINISTRADOR)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var acceso = ValidarAdministrador();
            if (acceso != null)
                return acceso;

            var equipo = await _context.Equipos
                .Include(e => e.IdTipoEquipoNavigation)
                .Include(e => e.IdEstadoNavigation)
                .FirstOrDefaultAsync(e => e.IdEquipo == id);

            if (equipo == null)
                return NotFound();

            return View("Delete", equipo);
        }


        // =========================================================
        // M08 - DAR DE BAJA EQUIPO (SOLO ADMINISTRADOR)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEliminar(int id)
        {
            var acceso = ValidarAdministrador();
            if (acceso != null)
                return acceso;

            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.IdEquipo == id);

            if (equipo == null)
                return NotFound();

            if (equipo.IdEstado == 4)
            {
                TempData["Error"] = "Este equipo ya se encuentra dado de baja.";
                return RedirectToAction(nameof(Index));
            }

            equipo.IdEstado = 4;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Equipo dado de baja correctamente.";

            return RedirectToAction(nameof(Index));
        }

    }
}
