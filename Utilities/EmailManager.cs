
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace cmse.Utilities
{
    public class EmailManager
    {
        private readonly IConfiguration _configuration;
        private readonly PlantillaCorreo _plantillaCorreo;

        public EmailManager(
            IConfiguration configuration,
            PlantillaCorreo plantillaCorreo)
        {
            _configuration = configuration;
            _plantillaCorreo = plantillaCorreo;
        }

        public async Task EnviarCorreoAsync(
            string destinatario,
            string asunto,
            string contenido,
            bool esHtml = true)
        {
            using var correo = new MailMessage();

            correo.From = ObtenerRemitente();
            correo.To.Add(destinatario);
            correo.Subject = asunto;
            correo.Body = contenido;
            correo.IsBodyHtml = esHtml;

            await EnviarMensajeAsync(correo);
        }

        public async Task EnviarCorreoPlantillaAsync(
            string destinatario,
            string asunto,
            string titulo,
            string nombre,
            string mensaje,
            string contactoSoporte)
        {
            string contenidoHtml = _plantillaCorreo.GenerarCorreo(
                titulo,
                nombre,
                mensaje,
                contactoSoporte);

            string rutaLogo = _plantillaCorreo.ObtenerRutaLogo();

            if (!File.Exists(rutaLogo))
            {
                throw new FileNotFoundException(
                    "No se encontró el logo de CMSE.",
                    rutaLogo);
            }

            using var correo = new MailMessage();

            correo.From = ObtenerRemitente();
            correo.To.Add(destinatario);
            correo.Subject = asunto;
            correo.Body = contenidoHtml;
            correo.IsBodyHtml = true;

            var vistaHtml = AlternateView.CreateAlternateViewFromString(
                contenidoHtml,
                null,
                MediaTypeNames.Text.Html);

            var logo = new LinkedResource(
                rutaLogo,
                MediaTypeNames.Image.Jpeg)
            {
                ContentId = "logoCmse",
                TransferEncoding = TransferEncoding.Base64
            };

            vistaHtml.LinkedResources.Add(logo);
            correo.AlternateViews.Add(vistaHtml);

            await EnviarMensajeAsync(correo);
        }

        private MailAddress ObtenerRemitente()
        {
            string remitente =
                _configuration["EmailSettings:SenderEmail"]!;

            string nombreRemitente =
                _configuration["EmailSettings:SenderName"]!;

            return new MailAddress(remitente, nombreRemitente);
        }

        private async Task EnviarMensajeAsync(MailMessage correo)
        {
            string servidor =
                _configuration["EmailSettings:SmtpServer"]!;

            int puerto = int.Parse(
                _configuration["EmailSettings:SmtpPort"]!);

            string usuario =
                _configuration["EmailSettings:Username"]!;

            string contrasena =
                _configuration["EmailSettings:Password"]!;

            bool usarSsl = bool.Parse(
                _configuration["EmailSettings:EnableSsl"]!);

            using var cliente = new SmtpClient(servidor, puerto);

            cliente.Credentials = new NetworkCredential(
                usuario,
                contrasena);

            cliente.EnableSsl = usarSsl;

            await cliente.SendMailAsync(correo);
        }
    }
}
