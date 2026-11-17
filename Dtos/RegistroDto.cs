using System.ComponentModel.DataAnnotations;
namespace cmse.Dtos
{
    public class RegistroDto
    {
        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public int IdTipoDocumento { get; set; }

        public string NumeroDocumento { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener mínimo 6 caracteres.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe incluir al menos una letra y un número.")]

        public string Contrasena { get; set; } = string.Empty;
    }
}