using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class EquiposProyectore
{
    public int IdEquipo { get; set; }

    public string TecnologiaProyeccion { get; set; } = null!;

    public string FuenteIluminacion { get; set; } = null!;

    public string ResolucionNativa { get; set; } = null!;

    public string? ResolucionMaxima { get; set; }

    public int BrilloLumenes { get; set; }

    public string? RelacionContraste { get; set; }

    public string? RelacionAspecto { get; set; }

    public string? TamanoProyeccion { get; set; }

    public string? DistanciaProyeccion { get; set; }

    public string? TipoLente { get; set; }

    public string? CorreccionTrapezoidal { get; set; }

    public string? Puertos { get; set; }

    public int? HorasUso { get; set; }

    public bool? AltavocesIntegrados { get; set; }

    public virtual Equipo IdEquipoNavigation { get; set; } = null!;
}
