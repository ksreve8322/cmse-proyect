using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class AsignacionesIncidencium
{
    public int IdAsignacion { get; set; }

    public int IdIncidencia { get; set; }

    public int IdTecnico { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public DateTime? FechaFinalizacion { get; set; }

    public virtual Incidencia IdIncidenciaNavigation { get; set; } = null!;

    public virtual Usuario IdTecnicoNavigation { get; set; } = null!;
}
