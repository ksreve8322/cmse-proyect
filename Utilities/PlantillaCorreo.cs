
using System.Net;

namespace cmse.Utilities
{
    public class PlantillaCorreo
    {
        private readonly IWebHostEnvironment _environment;

        public PlantillaCorreo(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public string ObtenerRutaLogo()
        {
            return Path.Combine(
                _environment.WebRootPath,
                "images",
                "logocmse.jpeg");
        }

        public string GenerarCorreo(
            string titulo,
            string nombre,
            string mensaje,
            string contactoSoporte)
        {
            // imagen incrustada mediante CID.
            // EmailManager debe adjuntar el logo con ContentId = "logoCmse".

            string imagenHtml = """
                <img
                    src="cid:logoCmse"
                    alt="Logo CMSE"
                    width="150"
                    style="display:block;max-width:150px;
                           height:auto;border:0;">
                """;

            /*
            VERSION ANTERIOR: imagen en Base64.

            Se conserva como referencia para consultar con el profesor.
            Este código NO se ejecuta.

            string rutaLogo = ObtenerRutaLogo();
            string logoBase64 = string.Empty;

            if (File.Exists(rutaLogo))
            {
                byte[] imagen = File.ReadAllBytes(rutaLogo);
                logoBase64 = Convert.ToBase64String(imagen);
            }

            string imagenHtml = string.Empty;

            if (!string.IsNullOrEmpty(logoBase64))
            {
                imagenHtml = $"""
                    <img
                        src="data:image/jpeg;base64,{logoBase64}"
                        alt="Logo CMSE"
                        width="150"
                        style="display:block;max-width:150px;
                               height:auto;border:0;">
                    """;
            }
            */

            string tituloSeguro = WebUtility.HtmlEncode(titulo);
            string nombreSeguro = WebUtility.HtmlEncode(nombre);

            string mensajeSeguro = WebUtility.HtmlEncode(mensaje)
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>");

            string contactoSeguro =
                WebUtility.HtmlEncode(contactoSoporte);

            return $"""
                <!DOCTYPE html>
                <html lang="es">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport"
                          content="width=device-width, initial-scale=1.0">
                    <title>{tituloSeguro}</title>
                </head>
                <body style="margin:0;padding:0;
                background-color:#17365d;
                font-family:Arial,Helvetica,sans-serif;
                color:#263445;">

                    <table role="presentation" width="100%"
                        cellpadding="0" cellspacing="0" border="0"
                        style="background-color:#24476b;
                background-image:linear-gradient(135deg,#10243e 0%,#24476b 50%,#426b91 100%);
                padding:32px 10px;">
                <tr>
                            <td align="center">

                                <table role="presentation" width="600"
                                    cellpadding="0" cellspacing="0" border="0"
                                    style="width:100%;max-width:600px;
                                    background-color:#ffffff;
                                    border-radius:10px;overflow:hidden;">

                                    <tr>
                                        <td align="center"
                                            style="background-color:#123b67;
                                            padding:28px 20px;color:#ffffff;">

                                            {imagenHtml}

                                            <h1 style="margin:16px 0 6px;
                                                font-size:24px;color:#ffffff;">
                                                CMSE
                                            </h1>

                                            <p style="margin:0;font-size:14px;
                                                line-height:1.5;color:#e5edf7;">
                                                Control y Mantenimiento
                                                de Sistemas y Equipos
                                            </p>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style="padding:30px 26px;">

                                            <h2 style="margin:0 0 20px;
                                                font-size:22px;
                                                line-height:1.4;
                                                color:#123b67;">
                                                {tituloSeguro}
                                            </h2>

                                            <p style="font-size:16px;
                                                line-height:1.7;">
                                                Hola, <strong>{nombreSeguro}</strong>.
                                            </p>

                                            <p style="font-size:15px;
                                                line-height:1.8;color:#405166;">
                                                {mensajeSeguro}
                                            </p>

                                            <p style="font-size:14px;
                                                line-height:1.7;color:#64748b;">
                                                Este mensaje fue generado
                                                automáticamente por CMSE.
                                            </p>

                                        </td>
                                    </tr>

                                    <tr>
                                        <td style="background-color:#eaf2fb;
                                            padding:20px 26px;
                                            border-top:1px solid #d7e3f0;">

                                            <p style="margin:0 0 8px;
                                                font-size:14px;font-weight:bold;
                                                color:#123b67;">
                                                ¿Necesitas ayuda?
                                            </p>

                                            <p style="margin:0;font-size:13px;
                                                line-height:1.7;color:#405166;">
                                                Contacta al área de soporte:
                                                {contactoSeguro}
                                            </p>

                                        </td>
                                    </tr>

                                    <tr>
                                        <td align="center"
                                            style="background-color:#123b67;
                                            padding:18px 20px;">

                                            <p style="margin:0 0 6px;
                                                font-size:12px;
                                                line-height:1.6;color:#ffffff;">
                                                © CMSE — Control y Mantenimiento
                                                de Sistemas y Equipos
                                            </p>

                                            <p style="margin:0;font-size:12px;
                                                line-height:1.6;color:#dbe7f5;">
                                                Este correo es automático.
                                                Por favor, no respondas a este mensaje.
                                            </p>

                                        </td>
                                    </tr>

                                </table>

                            </td>
                        </tr>
                    </table>

                </body>
                </html>
                """;
        }
    }
}
