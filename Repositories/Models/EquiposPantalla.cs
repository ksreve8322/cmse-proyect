
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class EquiposPantalla
{
    public int IdEquipo { get; set; }

    public decimal TamanoPulgadas { get; set; }

    public string Resolucion { get; set; } = null!;

    public int FrecuenciaActualizacion { get; set; }

    public string TecnologiaPanel { get; set; } = null!;

    public bool? Hdr { get; set; }

    public string? SistemaOperativo { get; set; }

    public string? Puertos { get; set; }

    public virtual Equipo IdEquipoNavigation { get; set; } = null!;
}
