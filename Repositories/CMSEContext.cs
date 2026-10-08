using cmse.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace cmse.Repositories
{
    public class CMSEContext : DbContext
    {
        public CMSEContext(DbContextOptions<CMSEContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoDocumento> TiposDocumento { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");

                entity.HasKey(u => u.IdUsuario);

                entity.Property(u => u.IdUsuario)
                    .HasColumnName("id_usuario");

                entity.HasOne<Rol>(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.IdRol);

                entity.Property(u => u.IdRol)
                    .HasColumnName("id_rol");

                entity.Property(u => u.IdTipoDocumento)
                    .HasColumnName("id_tipo_documento");

                entity.Property(u => u.Nombres)
                    .HasColumnName("nombres");

                entity.Property(u => u.Apellidos)
                    .HasColumnName("apellidos");

                entity.Property(u => u.NumeroDocumento)
                    .HasColumnName("numero_documento");

                entity.Property(u => u.Correo)
                    .HasColumnName("correo");

                entity.Property(u => u.Telefono)
                    .HasColumnName("telefono");

                entity.Property(u => u.Contrasena)
                    .HasColumnName("contrasena");

                entity.Property(u => u.Estado)
                    .HasColumnName("estado");

                entity.Property(u => u.IntentosFallidos)
                    .HasColumnName("intentos_fallidos");

            });
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("roles");

                entity.HasKey(r => r.IdRol);

                entity.Property(r => r.IdRol)
                    .HasColumnName("id_rol");

                entity.Property(r => r.Nombre)
                    .HasColumnName("nombre");
            });
            modelBuilder.Entity<TipoDocumento>(entity =>
            {
                entity.ToTable("tipos_documento");

                entity.HasKey(t => t.IdTipoDocumento);

                entity.Property(t => t.IdTipoDocumento)
                    .HasColumnName("id_tipo_documento");

                entity.Property(t => t.Nombre)
                    .HasColumnName("nombre");
            });
        }
    }
}