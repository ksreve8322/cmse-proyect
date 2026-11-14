
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class EquiposImpresora
{
    public int IdEquipo { get; set; }

    public string TipoImpresora { get; set; } = null!;

    public string ResolucionImpresion { get; set; } = null!;

    public string? FormatosSoportados { get; set; }

    public bool ImpresionColor { get; set; }

    public string? Puertos { get; set; }

    public virtual Equipo IdEquipoNavigation { get; set; } = null!;
}
