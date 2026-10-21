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
    }
}