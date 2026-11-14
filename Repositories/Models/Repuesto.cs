
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class Repuesto
{
    public int IdRepuesto { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? NumeroSerie { get; set; }

    public string? Observaciones { get; set; }

    public string Codigo { get; set; } = null!;

    public virtual ICollection<MantenimientoRepuesto> MantenimientoRepuestos { get; set; } = new List<MantenimientoRepuesto>();
}
