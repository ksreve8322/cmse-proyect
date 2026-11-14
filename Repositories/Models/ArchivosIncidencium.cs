
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class ArchivosIncidencium
{
    public int IdArchivo { get; set; }

    public int IdIncidencia { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string RutaArchivo { get; set; } = null!;

    public string Extension { get; set; } = null!;

    public DateTime FechaSubida { get; set; }

    public virtual Incidencia IdIncidenciaNavigation { get; set; } = null!;
}
