using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class CanalesNotificacion
{
    public int IdCanal { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual ICollection<PreferenciasNotificacion> PreferenciasNotificacions { get; set; } = new List<PreferenciasNotificacion>();
}
