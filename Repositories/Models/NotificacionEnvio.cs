
namespace cmse.Repositories.Models;

public class NotificacionEnvio
{
    public int IdEnvio { get; set; }

    public int IdNotificacion { get; set; }

    public string Destinatario { get; set; } = null!;

    public DateTime FechaIntento { get; set; }

    public string EstadoEnvio { get; set; } = "pendiente";

    public string? MensajeError { get; set; }
}
