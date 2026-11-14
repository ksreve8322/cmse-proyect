using System;
using System.Collections.Generic;

namespace cmse.TempScaffold;

public partial class PlantillasNotificacion
{
    public int IdPlantilla { get; set; }

    public string Nombre { get; set; } = null!;

    public string Asunto { get; set; } = null!;

    public string ContenidoHtml { get; set; } = null!;

    public string? ContenidoTexto { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }
}
