using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class TiposMantenimiento
{
    public int IdTipoMantenimiento { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
}
