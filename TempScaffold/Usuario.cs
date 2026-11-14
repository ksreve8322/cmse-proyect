using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public int IdTipoDocumento { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public string Contrasena { get; set; } = null!;

    public bool Estado { get; set; }

    public byte IntentosFallidos { get; set; }

    public virtual ICollection<AsignacionesIncidencium> AsignacionesIncidencia { get; set; } = new List<AsignacionesIncidencium>();

    public virtual ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual TiposDocumento IdTipoDocumentoNavigation { get; set; } = null!;

    public virtual ICollection<Incidencia> Incidencia { get; set; } = new List<Incidencia>();

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<PreferenciasNotificacion> PreferenciasNotificacions { get; set; } = new List<PreferenciasNotificacion>();

    public virtual ICollection<RecuperacionesContrasena> RecuperacionesContrasenas { get; set; } = new List<RecuperacionesContrasena>();
}
