using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class NotificacionesEnvio
{
    public int IdEnvio { get; set; }

    public int IdNotificacion { get; set; }

    public string Destinatario { get; set; } = null!;

    public DateTime FechaIntento { get; set; }

    public string EstadoEnvio { get; set; } = null!;

    public string? MensajeError { get; set; }

    public virtual Notificacione IdNotificacionNavigation { get; set; } = null!;
}
