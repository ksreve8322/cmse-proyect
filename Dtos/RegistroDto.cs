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

        public string Contrasena { get; set; } = string.Empty;
    }
}