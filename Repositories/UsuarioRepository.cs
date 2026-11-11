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

    }

}