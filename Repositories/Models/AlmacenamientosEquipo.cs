
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class AlmacenamientosEquipo
{
    public int IdAlmacenamiento { get; set; }

    public int IdEquipo { get; set; }

    public decimal Capacidad { get; set; }

    public string Unidad { get; set; } = null!;

    public string TipoAlmacenamiento { get; set; } = null!;

    public virtual EquiposComputadore IdEquipoNavigation { get; set; } = null!;
}
