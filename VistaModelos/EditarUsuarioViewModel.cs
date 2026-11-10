
using System.ComponentModel.DataAnnotations;

namespace cmse.VistaModelos
{
    public class EditarUsuarioViewModel
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        public int IdTipoDocumento { get; set; }

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        public string Correo { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public int IdRol { get; set; }

        public bool Estado { get; set; }
    }
}
