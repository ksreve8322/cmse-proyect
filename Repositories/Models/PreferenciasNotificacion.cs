
using System;

namespace cmse.Repositories.Models;

public partial class PreferenciasNotificacion
{
    public int IdPreferencia { get; set; }

    public int IdUsuario { get; set; }

    public int IdCanal { get; set; }

    public bool Habilitada { get; set; }

    public virtual CanalesNotificacion IdCanalNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
