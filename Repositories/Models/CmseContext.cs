using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace cmse.Repositories.Models;

public partial class CmseContext : DbContext
{
    public CmseContext()
    {
    }

    public CmseContext(DbContextOptions<CmseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AsignacionesIncidencium> AsignacionesIncidencia { get; set; }

    public virtual DbSet<Diagnostico> Diagnosticos { get; set; }

    public virtual DbSet<Equipo> Equipos { get; set; }

    public virtual DbSet<Estado> Estados { get; set; }

    public virtual DbSet<Incidencia> Incidencias { get; set; }

    public virtual DbSet<Mantenimiento> Mantenimientos { get; set; }

    public virtual DbSet<MantenimientoRepuesto> MantenimientoRepuestos { get; set; }

    public virtual DbSet<Repuesto> Repuestos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TiposDocumento> TiposDocumentos { get; set; }

    public virtual DbSet<TiposEquipo> TiposEquipos { get; set; }

    public virtual DbSet<TiposMantenimiento> TiposMantenimientos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Notificacion> Notificaciones { get; set; }

    public virtual DbSet<NotificacionEnvio> NotificacionesEnvios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Test");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AsignacionesIncidencium>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion).HasName("PK__asignaci__C3F7F966EC54F23A");

            entity.ToTable("asignaciones_incidencia");

            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.FechaAsignacion).HasColumnName("fecha_asignacion");
            entity.Property(e => e.FechaFinalizacion).HasColumnName("fecha_finalizacion");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.IdTecnico).HasColumnName("id_tecnico");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.AsignacionesIncidencia)
                .HasForeignKey(d => d.IdIncidencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__asignacio__id_in__01142BA1");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.AsignacionesIncidencia)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__asignacio__id_te__02084FDA");
        });

        modelBuilder.Entity<Diagnostico>(entity =>
        {
            entity.HasKey(e => e.IdDiagnostico).HasName("PK__diagnost__1384B745F898DDD3");

            entity.ToTable("diagnosticos");

            entity.Property(e => e.IdDiagnostico).HasColumnName("id_diagnostico");
            entity.Property(e => e.FechaDiagnostico).HasColumnName("fecha_diagnostico");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.IdTecnico).HasColumnName("id_tecnico");
            entity.Property(e => e.Resultado).HasColumnName("resultado");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.Diagnosticos)
                .HasForeignKey(d => d.IdIncidencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__diagnosti__id_in__7A672E12");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.Diagnosticos)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__diagnosti__id_te__7B5B524B");
        });

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.HasKey(e => e.IdEquipo).HasName("PK__equipos__EE01F88ABA4E0F78");

            entity.ToTable("equipos");

            entity.HasIndex(e => e.CodigoInventario, "UQ__equipos__2C4D9A17226023E5").IsUnique();

            entity.HasIndex(e => e.NumeroSerie, "UQ__equipos__D8D7353C16D175B5").IsUnique();

            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.CodigoInventario)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("codigo_inventario");
            entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdTipoEquipo).HasColumnName("id_tipo_equipo");
            entity.Property(e => e.Marca)
                .HasMaxLength(40)
                .HasColumnName("marca");
            entity.Property(e => e.Modelo)
                .HasMaxLength(50)
                .HasColumnName("modelo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
            entity.Property(e => e.NumeroSerie)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("numero_serie");
            entity.Property(e => e.Ubicacion)
                .HasMaxLength(100)
                .HasColumnName("ubicacion");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Equipos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_equipos_estados");

            entity.HasOne(d => d.IdTipoEquipoNavigation).WithMany(p => p.Equipos)
                .HasForeignKey(d => d.IdTipoEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__equipos__id_tipo__693CA210");
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PK__estados__86989FB2B5F095CF");

            entity.ToTable("estados");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.Modulo)
                .HasMaxLength(30)
                .HasColumnName("modulo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Incidencia>(entity =>
        {
            entity.HasKey(e => e.IdIncidencia).HasName("PK__incidenc__D7757E762075B755");

            entity.ToTable("incidencias");

            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCierre).HasColumnName("fecha_cierre");
            entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");
            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Titulo)
                .HasMaxLength(100)
                .HasColumnName("titulo");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__incidenci__id_eq__6EF57B66");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_incidencias_estados");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__incidenci__id_us__6E01572D");
        });

        modelBuilder.Entity<Mantenimiento>(entity =>
        {
            entity.HasKey(e => e.IdMantenimiento).HasName("PK__mantenim__707E5D16CAEC0C68");

            entity.ToTable("mantenimientos");

            entity.Property(e => e.IdMantenimiento).HasColumnName("id_mantenimiento");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaProgramada).HasColumnName("fecha_programada");
            entity.Property(e => e.FechaRealizacion).HasColumnName("fecha_realizacion");
            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.IdTecnico).HasColumnName("id_tecnico");
            entity.Property(e => e.IdTipoMantenimiento).HasColumnName("id_tipo_mantenimiento");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.Resultado).HasColumnName("resultado");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_eq__72C60C4A");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mantenimientos_estados");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdIncidencia)
                .HasConstraintName("FK__mantenimi__id_in__73BA3083");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_te__74AE54BC");

            entity.HasOne(d => d.IdTipoMantenimientoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdTipoMantenimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_ti__75A278F5");
        });

        modelBuilder.Entity<MantenimientoRepuesto>(entity =>
        {
            entity.HasKey(e => new { e.IdMantenimiento, e.IdRepuesto }).HasName("PK__mantenim__99A720059D2FB88D");

            entity.ToTable("mantenimiento_repuestos");

            entity.Property(e => e.IdMantenimiento).HasColumnName("id_mantenimiento");
            entity.Property(e => e.IdRepuesto).HasColumnName("id_repuesto");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");

            entity.HasOne(d => d.IdMantenimientoNavigation).WithMany(p => p.MantenimientoRepuestos)
                .HasForeignKey(d => d.IdMantenimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_ma__04E4BC85");

            entity.HasOne(d => d.IdRepuestoNavigation).WithMany(p => p.MantenimientoRepuestos)
                .HasForeignKey(d => d.IdRepuesto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_re__05D8E0BE");
        });

        modelBuilder.Entity<Repuesto>(entity =>
        {
            entity.HasKey(e => e.IdRepuesto).HasName("PK__repuesto__9D97D13F8AB5034D");

            entity.ToTable("repuestos");

            entity.Property(e => e.IdRepuesto).HasColumnName("id_repuesto");
            entity.Property(e => e.FechaInstalacion).HasColumnName("fecha_instalacion");
            entity.Property(e => e.Marca)
                .HasMaxLength(40)
                .HasColumnName("marca");
            entity.Property(e => e.Modelo)
                .HasMaxLength(50)
                .HasColumnName("modelo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
            entity.Property(e => e.NumeroSerie)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("numero_serie");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.ValorUnitario)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("valor_unitario");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK__roles__6ABCB5E022ABAFFE");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Nombre, "UQ__roles__72AFBCC618E4BAE0").IsUnique();

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposDocumento>(entity =>
        {
            entity.HasKey(e => e.IdTipoDocumento).HasName("PK__tipos_do__9F38507C89B86F2E");

            entity.ToTable("tipos_documento");

            entity.HasIndex(e => e.Nombre, "UQ__tipos_do__72AFBCC658B095E2").IsUnique();

            entity.Property(e => e.IdTipoDocumento).HasColumnName("id_tipo_documento");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposEquipo>(entity =>
        {
            entity.HasKey(e => e.IdTipoEquipo).HasName("PK__tipos_eq__82617B3287FF4BAD");

            entity.ToTable("tipos_equipo");

            entity.HasIndex(e => e.Nombre, "UQ__tipos_eq__72AFBCC6259AD838").IsUnique();

            entity.Property(e => e.IdTipoEquipo).HasColumnName("id_tipo_equipo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(40)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposMantenimiento>(entity =>
        {
            entity.HasKey(e => e.IdTipoMantenimiento).HasName("PK__tipos_ma__3BA53837650B2D22");

            entity.ToTable("tipos_mantenimiento");

            entity.HasIndex(e => e.Nombre, "UQ__tipos_ma__72AFBCC6A87AD642").IsUnique();

            entity.Property(e => e.IdTipoMantenimiento).HasColumnName("id_tipo_mantenimiento");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);

            entity.ToTable("notificaciones");

            entity.Property(e => e.IdNotificacion)
                .HasColumnName("id_notificacion");

            entity.Property(e => e.IdUsuario)
                .HasColumnName("id_usuario");

            entity.Property(e => e.IdCanal)
                .HasColumnName("id_canal");

            entity.Property(e => e.Titulo)
                .HasMaxLength(100)
                .HasColumnName("titulo");

            entity.Property(e => e.Mensaje)
                .HasMaxLength(500)
                .HasColumnName("mensaje");

            entity.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion");

            entity.Property(e => e.Leida)
                .HasColumnName("leida");

            entity.Property(e => e.Estado)
                .HasColumnName("estado");

            entity.HasOne(e => e.IdUsuarioNavigation)
                .WithMany()
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificaciones_usuarios");
        });
        modelBuilder.Entity<NotificacionEnvio>(entity =>
        {
            entity.HasKey(e => e.IdEnvio)
                .HasName("PK_notifica_8C48C8CAC8D91BD2");

            entity.ToTable("notificaciones_envios");

            entity.Property(e => e.IdEnvio)
                .HasColumnName("id_envio");

            entity.Property(e => e.IdNotificacion)
                .HasColumnName("id_notificacion");

            entity.Property(e => e.Destinatario)
                .HasMaxLength(254)
                .IsUnicode(false)
                .HasColumnName("destinatario");

            entity.Property(e => e.FechaIntento)
                .HasColumnType("datetime2")
                .HasColumnName("fecha_intento")
                .HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.EstadoEnvio)
                .HasMaxLength(40)
                .HasColumnName("estado_envio")
                .HasDefaultValue("pendiente");

            entity.Property(e => e.MensajeError)
                .HasColumnName("mensaje_error");

            entity.HasOne<Notificacion>()
                .WithMany()
                .HasForeignKey(e => e.IdNotificacion)
                .OnDelete(DeleteBehavior.ClientNoAction)
                .HasConstraintName("FK_notificaciones_envios_notificaciones");
        });
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__usuarios__4E3E04ADBEE5ED61");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo, "UQ__usuarios__2A586E0B65671984").IsUnique();

            entity.HasIndex(e => e.NumeroDocumento, "UQ__usuarios__7B886A6321E999BF").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(60)
                .HasColumnName("apellidos");
            entity.Property(e => e.Contrasena)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("contrasena");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("correo");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.IdTipoDocumento).HasColumnName("id_tipo_documento");
            entity.Property(e => e.IntentosFallidos).HasColumnName("intentos_fallidos");
            entity.Property(e => e.Nombres)
                .HasMaxLength(60)
                .HasColumnName("nombres");
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("numero_documento");
            entity.Property(e => e.Telefono)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__usuarios__id_rol__628FA481");

            entity.HasOne(d => d.IdTipoDocumentoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdTipoDocumento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__usuarios__id_tip__6383C8BA");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
