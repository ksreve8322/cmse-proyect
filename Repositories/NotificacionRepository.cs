
using cmse.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace cmse.Repositories
{
    public class NotificacionRepository
    {
        private readonly CmseContext _context;

        public NotificacionRepository(CmseContext context)
        {
            _context = context;
        }

        // Obtiene las notificaciones activas de un usuario.
        public async Task<List<Notificacion>> ObtenerPorUsuarioAsync(
            int idUsuario)
        {
            return await _context.Notificaciones
                .Where(n =>
                    n.IdUsuario == idUsuario &&
                    n.Estado)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
        }

        // Cuenta las notificaciones que el usuario no ha leído.
        public async Task<int> ContarNoLeidasAsync(int idUsuario)
        {
            return await _context.Notificaciones
                .CountAsync(n =>
                    n.IdUsuario == idUsuario &&
                    n.Estado &&
                    !n.Leida);
        }

        // Marca una notificación como leída.
        // Verifica que pertenezca al usuario indicado.
        public async Task<bool> MarcarComoLeidaAsync(
            int idNotificacion,
            int idUsuario)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n =>
                    n.IdNotificacion == idNotificacion &&
                    n.IdUsuario == idUsuario &&
                    n.Estado);

            if (notificacion == null)
            {
                return false;
            }

            notificacion.Leida = true;

            await _context.SaveChangesAsync();

            return true;
        }

        // Guarda una notificación interna y devuelve su identificador.
        public async Task<int> CrearAsync(Notificacion notificacion)
        {
            notificacion.FechaCreacion = DateTime.Now;
            notificacion.Leida = false;
            notificacion.Estado = true;

            _context.Notificaciones.Add(notificacion);

            await _context.SaveChangesAsync();

            return notificacion.IdNotificacion;
        }
    }
}
    