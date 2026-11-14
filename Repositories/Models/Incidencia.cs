
using cmse.TempScaffold;
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class Incidencia
{
    public int IdIncidencia { get; set; }

    public int IdUsuario { get; set; }

    public int IdEquipo { get; set; }

    public int IdEstado { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaCierre { get; set; }

    public int? IdTipoIncidencia { get; set; }

    public virtual ICollection<ArchivosIncidencium> ArchivosIncidencia { get; set; } = new List<ArchivosIncidencium>();

    public virtual ICollection<AsignacionesIncidencium> AsignacionesIncidencia { get; set; } = new List<AsignacionesIncidencium>();

    public virtual ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();

    public virtual Equipo IdEquipoNavigation { get; set; } = null!;

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual TiposIncidencium? IdTipoIncidenciaNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
}
