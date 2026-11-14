
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class TiposIncidencium
{
    public int IdTipoIncidencia { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Incidencia> Incidencia { get; set; } = new List<Incidencia>();

    public virtual ICollection<TiposEquipo> IdTipoEquipos { get; set; } = new List<TiposEquipo>();
}
