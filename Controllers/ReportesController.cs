
using cmse.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace cmse.Controllers
{
    public class ReportesController : Controller
    {
        private readonly CmseContext _contexto;

        public ReportesController(CmseContext contexto)
        {
            _contexto = contexto;
        }

        private bool EsAdministrador()
        {
            return HttpContext.Session.GetInt32("RolId") == 1
                && HttpContext.Session.GetInt32("UsuarioId").HasValue;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            var tiposEquipo = await _contexto.Equipos
                .Select(e => e.IdTipoEquipoNavigation)
                .Distinct()
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            var tecnicos = await _contexto.Usuarios
                .Where(u => u.IdRol == 2 && u.Estado)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

            var estados = await _contexto.Estados
                .Where(e => e.Modulo == "incidencia"
                         || e.Modulo == "mantenimiento")
                .OrderBy(e => e.Modulo)
                .ThenBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.TiposEquipo = tiposEquipo;
            ViewBag.Tecnicos = tecnicos;
            ViewBag.Estados = estados;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Generar(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            int? idTipoEquipo,
            int? idTecnico,
            int? idEstado)
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            if (fechaInicio.HasValue && fechaFin.HasValue
                && fechaInicio.Value.Date > fechaFin.Value.Date)
            {
                TempData["ErrorReporte"] =
                    "La fecha inicial no puede ser posterior a la fecha final.";

                return RedirectToAction(nameof(Index));
            }

            var fechaFinExclusiva = fechaFin?.Date.AddDays(1);

            var consultaIncidencias = _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.AsignacionesIncidencia)
                    .ThenInclude(a => a.IdTecnicoNavigation)
                .AsQueryable();

            if (fechaInicio.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.FechaRegistro >= fechaInicio.Value.Date);

            if (fechaFinExclusiva.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.FechaRegistro < fechaFinExclusiva.Value);

            if (idTipoEquipo.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.IdEquipoNavigation.IdTipoEquipo == idTipoEquipo.Value);

            if (idTecnico.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.AsignacionesIncidencia
                        .Any(a => a.IdTecnico == idTecnico.Value));

            if (idEstado.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.IdEstado == idEstado.Value);

            var incidencias = await consultaIncidencias
                .OrderByDescending(i => i.FechaRegistro)
                .Select(i => new
                {
                    i.IdIncidencia,
                    i.Titulo,
                    i.FechaRegistro,
                    i.FechaCierre,
                    Equipo = i.IdEquipoNavigation.Nombre,
                    TipoEquipo = i.IdEquipoNavigation.IdTipoEquipoNavigation.Nombre,
                    Estado = i.IdEstadoNavigation.Nombre,
                    Tecnicos = i.AsignacionesIncidencia
                        .Select(a => a.IdTecnicoNavigation.Nombres + " "
                            + a.IdTecnicoNavigation.Apellidos)
                        .ToList()
                })
                .ToListAsync();

            var consultaMantenimientos = _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .AsQueryable();

            if (fechaInicio.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => (m.FechaRealizacion ?? m.FechaProgramada)
                        >= fechaInicio.Value.Date);

            if (fechaFinExclusiva.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => (m.FechaRealizacion ?? m.FechaProgramada)
                        < fechaFinExclusiva.Value);

            if (idTipoEquipo.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => m.IdEquipoNavigation.IdTipoEquipo == idTipoEquipo.Value);

            if (idTecnico.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => m.IdTecnico == idTecnico.Value);

            if (idEstado.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => m.IdEstado == idEstado.Value);

            var mantenimientos = await consultaMantenimientos
                .OrderByDescending(m => m.FechaRealizacion ?? m.FechaProgramada)
                .Select(m => new
                {
                    m.IdMantenimiento,
                    Fecha = m.FechaRealizacion ?? m.FechaProgramada,
                    m.Descripcion,
                    m.Resultado,
                    Equipo = m.IdEquipoNavigation.Nombre,
                    TipoEquipo = m.IdEquipoNavigation.IdTipoEquipoNavigation.Nombre,
                    Tecnico = m.IdTecnicoNavigation.Nombres + " "
                        + m.IdTecnicoNavigation.Apellidos,
                    TipoMantenimiento = m.IdTipoMantenimientoNavigation.Nombre,
                    Estado = m.IdEstadoNavigation.Nombre
                })
                .ToListAsync();

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.IdTipoEquipo = idTipoEquipo;
            ViewBag.IdTecnico = idTecnico;
            ViewBag.IdEstado = idEstado;

            ViewBag.NombreTipoEquipo = idTipoEquipo.HasValue
                ? await _contexto.Equipos
                    .Where(e => e.IdTipoEquipo == idTipoEquipo.Value)
                    .Select(e => e.IdTipoEquipoNavigation.Nombre)
                    .FirstOrDefaultAsync()
                : null;

            ViewBag.NombreTecnico = idTecnico.HasValue
                ? await _contexto.Usuarios
                    .Where(u => u.IdUsuario == idTecnico.Value)
                    .Select(u => u.Nombres + " " + u.Apellidos)
                    .FirstOrDefaultAsync()
                : null;

            ViewBag.NombreEstado = idEstado.HasValue
                ? await _contexto.Estados
                    .Where(e => e.IdEstado == idEstado.Value)
                    .Select(e => e.Nombre)
                    .FirstOrDefaultAsync()
                : null;

            ViewBag.Incidencias = incidencias;
            ViewBag.Mantenimientos = mantenimientos;

            return View("Resultados");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPdf(
            DateTime? fechaInicio,
            DateTime? fechaFin,
            int? idTipoEquipo,
            int? idTecnico,
            int? idEstado)
        {
            if (!EsAdministrador())
            {
                if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
                    return RedirectToAction("Login", "Account");

                return RedirectToAction("Index", "Home");
            }

            if (fechaInicio.HasValue && fechaFin.HasValue
                && fechaInicio.Value.Date > fechaFin.Value.Date)
                return BadRequest("El rango de fechas no es válido.");

            var fechaFinExclusiva = fechaFin?.Date.AddDays(1);

            var consultaIncidencias = _contexto.Incidencias
                .Include(i => i.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(i => i.IdEstadoNavigation)
                .Include(i => i.AsignacionesIncidencia)
                    .ThenInclude(a => a.IdTecnicoNavigation)
                .AsQueryable();

            if (fechaInicio.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.FechaRegistro >= fechaInicio.Value.Date);

            if (fechaFinExclusiva.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.FechaRegistro < fechaFinExclusiva.Value);

            if (idTipoEquipo.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.IdEquipoNavigation.IdTipoEquipo == idTipoEquipo.Value);

            if (idTecnico.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.AsignacionesIncidencia
                        .Any(a => a.IdTecnico == idTecnico.Value));

            if (idEstado.HasValue)
                consultaIncidencias = consultaIncidencias
                    .Where(i => i.IdEstado == idEstado.Value);

            var incidencias = await consultaIncidencias
                .OrderByDescending(i => i.FechaRegistro)
                .Select(i => new
                {
                    i.IdIncidencia,
                    i.Titulo,
                    i.FechaRegistro,
                    i.FechaCierre,
                    Equipo = i.IdEquipoNavigation.Nombre,
                    TipoEquipo = i.IdEquipoNavigation.IdTipoEquipoNavigation.Nombre,
                    Estado = i.IdEstadoNavigation.Nombre,
                    Tecnicos = i.AsignacionesIncidencia
                        .Select(a => a.IdTecnicoNavigation.Nombres + " "
                            + a.IdTecnicoNavigation.Apellidos)
                        .ToList()
                })
                .ToListAsync();

            var consultaMantenimientos = _contexto.Mantenimientos
                .Include(m => m.IdEquipoNavigation)
                    .ThenInclude(e => e.IdTipoEquipoNavigation)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTecnicoNavigation)
                .Include(m => m.IdTipoMantenimientoNavigation)
                .AsQueryable();

            if (fechaInicio.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => (m.FechaRealizacion ?? m.FechaProgramada)
                        >= fechaInicio.Value.Date);

            if (fechaFinExclusiva.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => (m.FechaRealizacion ?? m.FechaProgramada)
                        < fechaFinExclusiva.Value);

            if (idTipoEquipo.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => m.IdEquipoNavigation.IdTipoEquipo == idTipoEquipo.Value);

            if (idTecnico.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => m.IdTecnico == idTecnico.Value);

            if (idEstado.HasValue)
                consultaMantenimientos = consultaMantenimientos
                    .Where(m => m.IdEstado == idEstado.Value);

            var mantenimientos = await consultaMantenimientos
                .OrderByDescending(m => m.FechaRealizacion ?? m.FechaProgramada)
                .Select(m => new
                {
                    m.IdMantenimiento,
                    Fecha = m.FechaRealizacion ?? m.FechaProgramada,
                    m.Descripcion,
                    m.Resultado,
                    Equipo = m.IdEquipoNavigation.Nombre,
                    TipoEquipo = m.IdEquipoNavigation.IdTipoEquipoNavigation.Nombre,
                    Tecnico = m.IdTecnicoNavigation.Nombres + " "
                        + m.IdTecnicoNavigation.Apellidos,
                    TipoMantenimiento = m.IdTipoMantenimientoNavigation.Nombre,
                    Estado = m.IdEstadoNavigation.Nombre
                })
                .ToListAsync();

            var nombreTipoEquipo = idTipoEquipo.HasValue
                ? await _contexto.Equipos
                    .Where(e => e.IdTipoEquipo == idTipoEquipo.Value)
                    .Select(e => e.IdTipoEquipoNavigation.Nombre)
                    .FirstOrDefaultAsync() ?? "No encontrado"
                : "Todos";

            var nombreTecnico = idTecnico.HasValue
                ? await _contexto.Usuarios
                    .Where(u => u.IdUsuario == idTecnico.Value)
                    .Select(u => u.Nombres + " " + u.Apellidos)
                    .FirstOrDefaultAsync() ?? "No encontrado"
                : "Todos";

            var nombreEstado = idEstado.HasValue
                ? await _contexto.Estados
                    .Where(e => e.IdEstado == idEstado.Value)
                    .Select(e => e.Nombre + " (" + e.Modulo + ")")
                    .FirstOrDefaultAsync() ?? "No encontrado"
                : "Todos";

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("CMSE")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                        header.Item().Text("Reporte de mantenimiento e incidencias")
                            .FontSize(15).Bold();

                        header.Item().Text(
                            $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}");

                        header.Item().PaddingTop(5).Text("Filtros aplicados").Bold();

                        header.Item().Text(
                            $"Fecha inicial: {fechaInicio?.ToString("dd/MM/yyyy") ?? "Sin límite"}");

                        header.Item().Text(
                            $"Fecha final: {fechaFin?.ToString("dd/MM/yyyy") ?? "Sin límite"}");

                        header.Item().Text($"Tipo de equipo: {nombreTipoEquipo}");
                        header.Item().Text($"Técnico: {nombreTecnico}");
                        header.Item().Text($"Estado: {nombreEstado}");
                    });

                    page.Content().PaddingTop(15).Column(column =>
                    {
                        column.Item().Text("INCIDENCIAS")
                            .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().Text($"Total de incidencias: {incidencias.Count}");

                        if (incidencias.Count == 0)
                        {
                            column.Item().PaddingVertical(8)
                                .Text("No se encontraron incidencias.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(35);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    foreach (var titulo in new[]
                                    {
                                        "ID", "Título", "Registro", "Cierre",
                                        "Equipo", "Tipo equipo", "Técnico(s)", "Estado"
                                    })
                                    {
                                        header.Cell().Element(EstiloEncabezado)
                                            .Text(titulo).Bold().FontColor(Colors.White);
                                    }
                                });

                                foreach (var incidencia in incidencias)
                                {
                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.IdIncidencia.ToString());

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.Titulo);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.FechaRegistro.ToString("dd/MM/yyyy"));

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.FechaCierre?.ToString("dd/MM/yyyy") ?? "Pendiente");

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.Equipo);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.TipoEquipo);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.Tecnicos.Count > 0
                                            ? string.Join(", ", incidencia.Tecnicos)
                                            : "Sin asignar");

                                    table.Cell().Element(EstiloCelda)
                                        .Text(incidencia.Estado);
                                }
                            });
                        }

                        column.Item().PaddingTop(20)
                            .Text("MANTENIMIENTOS")
                            .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().Text($"Total de mantenimientos: {mantenimientos.Count}");

                        if (mantenimientos.Count == 0)
                        {
                            column.Item().PaddingVertical(8)
                                .Text("No se encontraron mantenimientos.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(35);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    foreach (var titulo in new[]
                                    {
                                        "ID", "Fecha", "Equipo", "Tipo equipo",
                                        "Técnico", "Tipo mantenimiento", "Resultado", "Estado"
                                    })
                                    {
                                        header.Cell().Element(EstiloEncabezado)
                                            .Text(titulo).Bold().FontColor(Colors.White);
                                    }
                                });

                                foreach (var mantenimiento in mantenimientos)
                                {
                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.IdMantenimiento.ToString());

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.Fecha.ToString("dd/MM/yyyy"));

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.Equipo);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.TipoEquipo);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.Tecnico);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.TipoMantenimiento);

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.Resultado ?? "Sin resultado");

                                    table.Cell().Element(EstiloCelda)
                                        .Text(mantenimiento.Estado);
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"Reporte_CMSE_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        private static IContainer EstiloEncabezado(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken2)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(4);
        }

        private static IContainer EstiloCelda(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(4);
        }
    }
}
