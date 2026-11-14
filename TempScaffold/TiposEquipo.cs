using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class TiposEquipo
{
    public int IdTipoEquipo { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();

    public virtual ICollection<TiposIncidencium> IdTipoIncidencia { get; set; } = new List<TiposIncidencium>();
}
