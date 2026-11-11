using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class Equipo
{
    public int IdEquipo { get; set; }

    public int IdTipoEquipo { get; set; }

    public int IdEstado { get; set; }

    public string Nombre { get; set; } = null!;

    public string CodigoInventario { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? NumeroSerie { get; set; }

    public string? Ubicacion { get; set; }

    public DateOnly FechaRegistro { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual TiposEquipo IdTipoEquipoNavigation { get; set; } = null!;

    public virtual ICollection<Incidencia> Incidencia { get; set; } = new List<Incidencia>();

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
}
