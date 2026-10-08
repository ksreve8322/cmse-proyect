namespace cmse.Repositories.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public int IdRol { get; set; }

        public Rol? Rol { get; set; }

        public int IdTipoDocumento { get; set; }

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string NumeroDocumento { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string Contrasena { get; set; } = string.Empty;

        public bool Estado { get; set; }

        public byte IntentosFallidos { get; set; }
    }
}