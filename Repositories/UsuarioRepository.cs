using cmse.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace cmse.Repositories
{
    public class UsuarioRepository
    {
        private readonly CMSEContext _context;

        public UsuarioRepository(CMSEContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
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
        public async Task<List<TipoDocumento>> ObtenerTiposDocumentoAsync()
        {
            return await _context.TiposDocumento
                .ToListAsync();
        }

        public async Task CrearAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }
    }
}