using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class TiposDocumento
{
    public int IdTipoDocumento { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
