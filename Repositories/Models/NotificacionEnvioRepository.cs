
using cmse.Repositories.Models;

namespace cmse.Repositories
{
    public class NotificacionEnvioRepository
    {
        private readonly CmseContext _context;

        public NotificacionEnvioRepository(CmseContext context)
        {
            _context = context;
        }

        public async Task RegistrarEnvioAsync(
            int idNotificacion,
            string destinatario,
            string estadoEnvio,
            string? mensajeError = null)
        {
            var envio = new NotificacionEnvio
            {
                IdNotificacion = idNotificacion,
                Destinatario = destinatario,
                FechaIntento = DateTime.Now,
                EstadoEnvio = estadoEnvio,
                MensajeError = mensajeError
            };

            _context.Set<NotificacionEnvio>().Add(envio);

            await _context.SaveChangesAsync();
        }
    }
}
