
using System;
namespace cmse.Repositories.Models;
public class Notificacion
{
    public int IdNotificacion { get; set; }

    public int IdUsuario { get; set; }

    public int IdCanal { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public bool Leida { get; set; }

    public bool Estado { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
