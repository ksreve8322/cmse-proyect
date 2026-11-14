using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class Notificacione
{
    public int IdNotificacion { get; set; }

    public int IdUsuario { get; set; }

    public int IdCanal { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public bool Leida { get; set; }

    public bool Estado { get; set; }

    public virtual CanalesNotificacion IdCanalNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<NotificacionesEnvio> NotificacionesEnvios { get; set; } = new List<NotificacionesEnvio>();
}
