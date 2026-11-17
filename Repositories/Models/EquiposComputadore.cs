using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class EquiposComputadore
{
    public int IdEquipo { get; set; }

    public string Procesador { get; set; } = null!;

    public int CapacidadRamGb { get; set; }

    public string TipoRam { get; set; } = null!;

    public string Gpu { get; set; } = null!;

    public string SistemaOperativo { get; set; } = null!;

    public string? Refrigeracion { get; set; }

    public string? Puertos { get; set; }

    public string? FuentePoder { get; set; }

    public string? Bateria { get; set; }

    public string? Pantalla { get; set; }

    public virtual ICollection<AlmacenamientosEquipo> AlmacenamientosEquipos { get; set; } = new List<AlmacenamientosEquipo>();

    public virtual Equipo IdEquipoNavigation { get; set; } = null!;
}
