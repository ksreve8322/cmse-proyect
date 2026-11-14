using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class Mantenimiento
{
    public int IdMantenimiento { get; set; }

    public int IdEquipo { get; set; }

    public int? IdIncidencia { get; set; }

    public int IdTecnico { get; set; }

    public int IdTipoMantenimiento { get; set; }

    public int IdEstado { get; set; }

    public DateTime FechaProgramada { get; set; }

    public DateTime? FechaRealizacion { get; set; }

    public string? Descripcion { get; set; }

    public string? Observaciones { get; set; }

    public string? Resultado { get; set; }

    public string? MotivoCancelacion { get; set; }

    public virtual Equipo IdEquipoNavigation { get; set; } = null!;

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual Incidencia? IdIncidenciaNavigation { get; set; }

    public virtual Usuario IdTecnicoNavigation { get; set; } = null!;

    public virtual TiposMantenimiento IdTipoMantenimientoNavigation { get; set; } = null!;

    public virtual ICollection<MantenimientoRepuesto> MantenimientoRepuestos { get; set; } = new List<MantenimientoRepuesto>();
}
