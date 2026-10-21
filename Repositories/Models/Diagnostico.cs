using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class Diagnostico
{
    public int IdDiagnostico { get; set; }

    public int IdIncidencia { get; set; }

    public int IdTecnico { get; set; }

    public DateTime FechaDiagnostico { get; set; }

    public string Resultado { get; set; } = null!;

    public virtual Incidencia IdIncidenciaNavigation { get; set; } = null!;

    public virtual Usuario IdTecnicoNavigation { get; set; } = null!;
}
