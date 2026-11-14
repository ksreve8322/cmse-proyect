
using System;
using System.Collections.Generic;

namespace cmse.Repositories.Models;

public partial class MantenimientoRepuesto
{
    public int IdMantenimiento { get; set; }

    public int IdRepuesto { get; set; }

    public int Cantidad { get; set; }

    public string? Observaciones { get; set; }

    public decimal ValorUnitario { get; set; }

    public DateOnly FechaInstalacion { get; set; }

    public decimal? ValorTotal { get; set; }

    public virtual Mantenimiento IdMantenimientoNavigation { get; set; } = null!;

    public virtual Repuesto IdRepuestoNavigation { get; set; } = null!;
}
