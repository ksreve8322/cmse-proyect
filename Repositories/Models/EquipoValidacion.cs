
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace cmse.Repositories.Models;

[ModelMetadataType(typeof(EquipoValidacionMetadata))]
public partial class Equipo
{
}

public class EquipoValidacionMetadata
{
    [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El código de inventario es obligatorio.")]
    [StringLength(30, ErrorMessage = "El código de inventario no puede superar los 30 caracteres.")]
    public string CodigoInventario { get; set; } = null!;

    [Required(ErrorMessage = "Debes seleccionar un tipo de equipo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un tipo de equipo válido.")]
    public int IdTipoEquipo { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un estado.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un estado válido.")]
    public int IdEstado { get; set; }
}
