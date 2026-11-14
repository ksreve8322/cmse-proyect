
using cmse.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace cmse.Repositories
{
    public class UsuarioRepository
    {
        private readonly CmseContext _context;

        public UsuarioRepository(CmseContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.Correo == correo);
        }

        public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario)
        {
            return await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.IdTipoDocumentoNavigation)
                .Include(u => u.Incidencia)
                    .ThenInclude(i => i.IdEquipoNavigation)
                .Include(u => u.Incidencia)
                    .ThenInclude(i => i.IdEstadoNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        }

        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.Correo == correo);
        }

        public async Task<bool> ExisteNumeroDocumentoAsync(string numeroDocumento)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.NumeroDocumento == numeroDocumento);
        }

        public async Task<List<TiposDocumento>> ObtenerTiposDocumentoAsync()
        {
            return await _context.TiposDocumentos
                .ToListAsync();
        }

        public async Task CrearAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Usuario>> ObtenerTodosPaginadosAsync(
            int pagina, int registrosPorPagina)
        {
            return await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.IdTipoDocumentoNavigation)
                .OrderBy(u => u.IdUsuario)
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();
        }

        public async Task<int> ContarUsuariosAsync()
        {
            return await _context.Usuarios.CountAsync();
        }

        public async Task<List<Role>> ObtenerRolesAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.IdRol)
                .ToListAsync();
        }

        public async Task<bool> ExisteCorreoEnOtroUsuarioAsync(
            string correo, int idUsuario)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.Correo == correo &&
                               u.IdUsuario != idUsuario);
        }

        public async Task<bool> ExisteDocumentoEnOtroUsuarioAsync(
            string numeroDocumento, int idUsuario)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.NumeroDocumento == numeroDocumento &&
                               u.IdUsuario != idUsuario);
        }

        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }

        // Comprobar si existe otro administrador activo.
        public async Task<bool> ExisteOtroAdministradorActivoAsync(int idUsuario)
        {
            return await _context.Usuarios
                .AnyAsync(u => u.IdRol == 1 &&
                               u.Estado &&
                               u.IdUsuario != idUsuario);
        }

        // Comprobar si el usuario tiene información operativa asociada.
        public async Task<bool> TieneRegistrosOperativosAsync(int idUsuario)
        {
            return await _context.Incidencias
                       .AnyAsync(i => i.IdUsuario == idUsuario)
                   || await _context.AsignacionesIncidencia
                       .AnyAsync(a => a.IdTecnico == idUsuario)
                   || await _context.Diagnosticos
                       .AnyAsync(d => d.IdTecnico == idUsuario)
                   || await _context.Mantenimientos
                       .AnyAsync(m => m.IdTecnico == idUsuario);
        }

        // Crear un nuevo token de recuperación.
        public async Task CrearRecuperacionContrasenaAsync(
            RecuperacionesContrasena recuperacion)
        {
            _context.RecuperacionesContrasenas.Add(recuperacion);
            await _context.SaveChangesAsync();
        }

        // Buscar una recuperación mediante el hash del token.
        public async Task<RecuperacionesContrasena?> ObtenerRecuperacionPorTokenAsync(
            string tokenHash)
        {
            return await _context.RecuperacionesContrasenas
                .Include(r => r.IdUsuarioNavigation)
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        }

        // Invalidar los tokens anteriores de un usuario.
        public async Task InvalidarRecuperacionesAnterioresAsync(int idUsuario)
        {
            var recuperaciones = await _context.RecuperacionesContrasenas
                .Where(r => r.IdUsuario == idUsuario && !r.Usado)
                .ToListAsync();

            foreach (var recuperacion in recuperaciones)
            {
                recuperacion.Usado = true;
            }

            await _context.SaveChangesAsync();
        }

        // Eliminar registros auxiliares y posteriormente la cuenta.
        public async Task EliminarCuentaAsync(int idUsuario)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var envios = await _context.NotificacionesEnvios
                    .Where(e => _context.Notificaciones
                        .Any(n => n.IdNotificacion == e.IdNotificacion &&
                                  n.IdUsuario == idUsuario))
                    .ToListAsync();

                _context.NotificacionesEnvios.RemoveRange(envios);

                var notificaciones = await _context.Notificaciones
                    .Where(n => n.IdUsuario == idUsuario)
                    .ToListAsync();

                _context.Notificaciones.RemoveRange(notificaciones);

                var preferencias = await _context.PreferenciasNotificacion
                    .Where(p => p.IdUsuario == idUsuario)
                    .ToListAsync();

                _context.PreferenciasNotificacion.RemoveRange(preferencias);

                var recuperaciones = await _context.RecuperacionesContrasenas
                    .Where(r => r.IdUsuario == idUsuario)
                    .ToListAsync();

                _context.RecuperacionesContrasenas.RemoveRange(recuperaciones);

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

                if (usuario == null)
                    throw new InvalidOperationException("El usuario no existe.");

                _context.Usuarios.Remove(usuario);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
