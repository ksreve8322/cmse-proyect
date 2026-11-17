
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
    public virtual DbSet<PreferenciasNotificacion> PreferenciasNotificacion { get; set; }
    public virtual DbSet<NotificacionEnvio> NotificacionesEnvios { get; set; }
    public virtual DbSet<AlmacenamientosEquipo> AlmacenamientosEquipos { get; set; }
    public virtual DbSet<ArchivosIncidencium> ArchivosIncidencia { get; set; }
    public virtual DbSet<CanalesNotificacion> CanalesNotificacions { get; set; }
    public virtual DbSet<EquiposComputadore> EquiposComputadores { get; set; }
    public virtual DbSet<EquiposImpresora> EquiposImpresoras { get; set; }
    public virtual DbSet<EquiposPantalla> EquiposPantallas { get; set; }
    public virtual DbSet<EquiposProyectore> EquiposProyectores { get; set; }
    public virtual DbSet<PlantillasNotificacion> PlantillasNotificacions { get; set; }
    public virtual DbSet<RecuperacionesContrasena> RecuperacionesContrasenas { get; set; }
    public virtual DbSet<TiposIncidencium> TiposIncidencia { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Test");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlmacenamientosEquipo>(entity =>
        {
            entity.HasKey(e => e.IdAlmacenamiento);
            entity.ToTable("almacenamientos_equipo");

            entity.Property(e => e.IdAlmacenamiento).HasColumnName("id_almacenamiento");
            entity.Property(e => e.Capacidad)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("capacidad");
            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.TipoAlmacenamiento)
                .HasMaxLength(20)
                .HasColumnName("tipo_almacenamiento");
            entity.Property(e => e.Unidad)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("unidad");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.AlmacenamientosEquipos)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_almacenamientos_computadores");
        });

        modelBuilder.Entity<PreferenciasNotificacion>(entity =>
        {
            entity.HasKey(e => e.IdPreferencia);
            entity.ToTable("preferencias_notificacion");

            entity.Property(e => e.IdPreferencia).HasColumnName("id_preferencia");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.IdCanal).HasColumnName("id_canal");
            entity.Property(e => e.Habilitada).HasColumnName("habilitada");

            entity.HasOne(d => d.IdCanalNavigation)
                .WithMany(p => p.PreferenciasNotificacions)
                .HasForeignKey(d => d.IdCanal);

            entity.HasOne(d => d.IdUsuarioNavigation)
                .WithMany()
                .HasForeignKey(d => d.IdUsuario);
        });

        modelBuilder.Entity<ArchivosIncidencium>(entity =>
        {
            entity.HasKey(e => e.IdArchivo);
            entity.ToTable("archivos_incidencia");

            entity.Property(e => e.IdArchivo).HasColumnName("id_archivo");
            entity.Property(e => e.Extension)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("extension");
            entity.Property(e => e.FechaSubida)
                .HasDefaultValueSql("(sysdatetime())", "DF_archivos_incidencia_fecha")
                .HasColumnName("fecha_subida");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(255)
                .HasColumnName("nombre_archivo");
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(500)
                .HasColumnName("ruta_archivo");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.ArchivosIncidencia)
                .HasForeignKey(d => d.IdIncidencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_archivos_incidencia_incidencias");
        });

        modelBuilder.Entity<AsignacionesIncidencium>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion);
            entity.ToTable("asignaciones_incidencia");

            entity.HasIndex(e => e.IdIncidencia, "UX_asignaciones_incidencia_activa")
                .IsUnique()
                .HasFilter("([fecha_finalizacion] IS NULL)");

            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.FechaAsignacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fecha_asignacion");
            entity.Property(e => e.FechaFinalizacion).HasColumnName("fecha_finalizacion");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.IdTecnico).HasColumnName("id_tecnico");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.AsignacionesIncidencia)
                .HasForeignKey(d => d.IdIncidencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__asignacio__id_in__03F0984C");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.AsignacionesIncidencia)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__asignacio__id_te__04E4BC85");
        });

        modelBuilder.Entity<CanalesNotificacion>(entity =>
        {
            entity.HasKey(e => e.IdCanal);
            entity.ToTable("canales_notificacion");

            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.Property(e => e.IdCanal).HasColumnName("id_canal");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Diagnostico>(entity =>
        {
            entity.HasKey(e => e.IdDiagnostico);
            entity.ToTable("diagnosticos");

            entity.Property(e => e.IdDiagnostico).HasColumnName("id_diagnostico");
            entity.Property(e => e.FechaDiagnostico)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fecha_diagnostico");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.IdTecnico).HasColumnName("id_tecnico");
            entity.Property(e => e.Resultado).HasColumnName("resultado");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.Diagnosticos)
                .HasForeignKey(d => d.IdIncidencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__diagnosti__id_in__05D8E0BE");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.Diagnosticos)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__diagnosti__id_te__06CD04F7");
        });

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.HasKey(e => e.IdEquipo);
            entity.ToTable("equipos");

            entity.HasIndex(e => e.CodigoInventario).IsUnique();
            entity.HasIndex(e => e.NumeroSerie).IsUnique();

            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.CodigoInventario)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("codigo_inventario");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(CONVERT([date],getdate()))")
                .HasColumnName("fecha_registro");
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
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
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
                .HasConstraintName("FK__equipos__id_tipo__07C12930");
        });

        modelBuilder.Entity<EquiposComputadore>(entity =>
        {
            entity.HasKey(e => e.IdEquipo);
            entity.ToTable("equipos_computadores");

            entity.Property(e => e.IdEquipo)
                .ValueGeneratedNever()
                .HasColumnName("id_equipo");
            entity.Property(e => e.Bateria)
                .HasMaxLength(100)
                .HasColumnName("bateria");
            entity.Property(e => e.CapacidadRamGb).HasColumnName("capacidad_ram_gb");
            entity.Property(e => e.FuentePoder)
                .HasMaxLength(100)
                .HasColumnName("fuente_poder");
            entity.Property(e => e.Gpu)
                .HasMaxLength(150)
                .HasColumnName("gpu");
            entity.Property(e => e.Pantalla)
                .HasMaxLength(150)
                .HasColumnName("pantalla");
            entity.Property(e => e.Procesador)
                .HasMaxLength(150)
                .HasColumnName("procesador");
            entity.Property(e => e.Puertos).HasColumnName("puertos");
            entity.Property(e => e.Refrigeracion)
                .HasMaxLength(150)
                .HasColumnName("refrigeracion");
            entity.Property(e => e.SistemaOperativo)
                .HasMaxLength(100)
                .HasColumnName("sistema_operativo");
            entity.Property(e => e.TipoRam)
                .HasMaxLength(20)
                .HasColumnName("tipo_ram");

            entity.HasOne(d => d.IdEquipoNavigation).WithOne(p => p.EquiposComputadore)
                .HasForeignKey<EquiposComputadore>(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_equipos_computadores_equipos");
        });

        modelBuilder.Entity<EquiposImpresora>(entity =>
        {
            entity.HasKey(e => e.IdEquipo);
            entity.ToTable("equipos_impresoras");

            entity.Property(e => e.IdEquipo)
                .ValueGeneratedNever()
                .HasColumnName("id_equipo");
            entity.Property(e => e.FormatosSoportados).HasColumnName("formatos_soportados");
            entity.Property(e => e.ImpresionColor).HasColumnName("impresion_color");
            entity.Property(e => e.Puertos).HasColumnName("puertos");
            entity.Property(e => e.ResolucionImpresion)
                .HasMaxLength(50)
                .HasColumnName("resolucion_impresion");
            entity.Property(e => e.TipoImpresora)
                .HasMaxLength(100)
                .HasColumnName("tipo_impresora");

            entity.HasOne(d => d.IdEquipoNavigation).WithOne(p => p.EquiposImpresora)
                .HasForeignKey<EquiposImpresora>(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_equipos_impresoras_equipos");
        });

        modelBuilder.Entity<EquiposPantalla>(entity =>
        {
            entity.HasKey(e => e.IdEquipo);
            entity.ToTable("equipos_pantallas");

            entity.Property(e => e.IdEquipo)
                .ValueGeneratedNever()
                .HasColumnName("id_equipo");
            entity.Property(e => e.FrecuenciaActualizacion).HasColumnName("frecuencia_actualizacion");
            entity.Property(e => e.Hdr).HasColumnName("hdr");
            entity.Property(e => e.Puertos).HasColumnName("puertos");
            entity.Property(e => e.Resolucion)
                .HasMaxLength(50)
                .HasColumnName("resolucion");
            entity.Property(e => e.SistemaOperativo)
                .HasMaxLength(100)
                .HasColumnName("sistema_operativo");
            entity.Property(e => e.TamanoPulgadas)
                .HasColumnType("decimal(6, 2)")
                .HasColumnName("tamano_pulgadas");
            entity.Property(e => e.TecnologiaPanel)
                .HasMaxLength(100)
                .HasColumnName("tecnologia_panel");

            entity.HasOne(d => d.IdEquipoNavigation).WithOne(p => p.EquiposPantalla)
                .HasForeignKey<EquiposPantalla>(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_equipos_pantallas_equipos");
        });

        modelBuilder.Entity<EquiposProyectore>(entity =>
        {
            entity.HasKey(e => e.IdEquipo);
            entity.ToTable("equipos_proyectores");

            entity.Property(e => e.IdEquipo)
                .ValueGeneratedNever()
                .HasColumnName("id_equipo");
            entity.Property(e => e.AltavocesIntegrados).HasColumnName("altavoces_integrados");
            entity.Property(e => e.BrilloLumenes).HasColumnName("brillo_lumenes");
            entity.Property(e => e.CorreccionTrapezoidal)
                .HasMaxLength(100)
                .HasColumnName("correccion_trapezoidal");
            entity.Property(e => e.DistanciaProyeccion)
                .HasMaxLength(100)
                .HasColumnName("distancia_proyeccion");
            entity.Property(e => e.FuenteIluminacion)
                .HasMaxLength(100)
                .HasColumnName("fuente_iluminacion");
            entity.Property(e => e.HorasUso).HasColumnName("horas_uso");
            entity.Property(e => e.Puertos).HasColumnName("puertos");
            entity.Property(e => e.RelacionAspecto)
                .HasMaxLength(50)
                .HasColumnName("relacion_aspecto");
            entity.Property(e => e.RelacionContraste)
                .HasMaxLength(50)
                .HasColumnName("relacion_contraste");
            entity.Property(e => e.ResolucionMaxima)
                .HasMaxLength(50)
                .HasColumnName("resolucion_maxima");
            entity.Property(e => e.ResolucionNativa)
                .HasMaxLength(50)
                .HasColumnName("resolucion_nativa");
            entity.Property(e => e.TamanoProyeccion)
                .HasMaxLength(100)
                .HasColumnName("tamano_proyeccion");
            entity.Property(e => e.TecnologiaProyeccion)
                .HasMaxLength(100)
                .HasColumnName("tecnologia_proyeccion");
            entity.Property(e => e.TipoLente)
                .HasMaxLength(100)
                .HasColumnName("tipo_lente");

            entity.HasOne(d => d.IdEquipoNavigation).WithOne(p => p.EquiposProyectore)
                .HasForeignKey<EquiposProyectore>(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_equipos_proyectores_equipos");
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.IdEstado);
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
            entity.HasKey(e => e.IdIncidencia);
            entity.ToTable("incidencias", tb => tb.UseSqlOutputClause(false));

            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCierre).HasColumnName("fecha_cierre");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdTipoIncidencia).HasColumnName("id_tipo_incidencia");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Titulo)
                .HasMaxLength(100)
                .HasColumnName("titulo");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__incidenci__id_eq__09A971A2");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_incidencias_estados");

            entity.HasOne(d => d.IdTipoIncidenciaNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdTipoIncidencia)
                .HasConstraintName("FK_incidencias_tipos_incidencia");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Incidencia)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__incidenci__id_us__0A9D95DB");
        });

        modelBuilder.Entity<Mantenimiento>(entity =>
        {
            entity.HasKey(e => e.IdMantenimiento);
            entity.ToTable("mantenimientos", tb => tb.UseSqlOutputClause(false));

            entity.Property(e => e.IdMantenimiento).HasColumnName("id_mantenimiento");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaProgramada).HasColumnName("fecha_programada");

            entity.Property(e => e.DuracionEstimadaHoras)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("duracion_estimada_horas");

            entity.Property(e => e.FechaRealizacion).HasColumnName("fecha_realizacion");
            entity.Property(e => e.IdEquipo).HasColumnName("id_equipo");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdIncidencia).HasColumnName("id_incidencia");
            entity.Property(e => e.IdTecnico).HasColumnName("id_tecnico");
            entity.Property(e => e.IdTipoMantenimiento).HasColumnName("id_tipo_mantenimiento");
            entity.Property(e => e.MotivoCancelacion).HasColumnName("motivo_cancelacion");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.Resultado).HasColumnName("resultado");

            entity.HasOne(d => d.IdEquipoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdEquipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_eq__0E6E26BF");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mantenimientos_estados");

            entity.HasOne(d => d.IdIncidenciaNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdIncidencia)
                .HasConstraintName("FK__mantenimi__id_in__0F624AF8");

            entity.HasOne(d => d.IdTecnicoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdTecnico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_te__10566F31");

            entity.HasOne(d => d.IdTipoMantenimientoNavigation).WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.IdTipoMantenimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_ti__114A936A");
        });

        modelBuilder.Entity<MantenimientoRepuesto>(entity =>
        {
            entity.HasKey(e => new { e.IdMantenimiento, e.IdRepuesto });
            entity.ToTable("mantenimiento_repuestos");

            entity.Property(e => e.IdMantenimiento).HasColumnName("id_mantenimiento");
            entity.Property(e => e.IdRepuesto).HasColumnName("id_repuesto");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.FechaInstalacion).HasColumnName("fecha_instalacion");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.ValorTotal)
                .HasComputedColumnSql("([cantidad]*[valor_unitario])", true)
                .HasColumnType("decimal(23, 2)")
                .HasColumnName("valor_total");
            entity.Property(e => e.ValorUnitario)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("valor_unitario");

            entity.HasOne(d => d.IdMantenimientoNavigation).WithMany(p => p.MantenimientoRepuestos)
                .HasForeignKey(d => d.IdMantenimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_ma__0C85DE4D");

            entity.HasOne(d => d.IdRepuestoNavigation).WithMany(p => p.MantenimientoRepuestos)
                .HasForeignKey(d => d.IdRepuesto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__mantenimi__id_re__0D7A0286");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);
            entity.ToTable("notificaciones");

            entity.Property(e => e.IdNotificacion).HasColumnName("id_notificacion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.IdCanal).HasColumnName("id_canal");
            entity.Property(e => e.Titulo)
                .HasMaxLength(100)
                .HasColumnName("titulo");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(500)
                .HasColumnName("mensaje");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.Estado).HasColumnName("estado");

            entity.HasOne<CanalesNotificacion>()
                .WithMany(p => p.Notificaciones)
                .HasForeignKey(e => e.IdCanal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificaciones_canales");

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

            entity.Property(e => e.IdEnvio).HasColumnName("id_envio");
            entity.Property(e => e.IdNotificacion).HasColumnName("id_notificacion");
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
            entity.Property(e => e.MensajeError).HasColumnName("mensaje_error");

            entity.HasOne<Notificacion>()
                .WithMany()
                .HasForeignKey(e => e.IdNotificacion)
                .OnDelete(DeleteBehavior.ClientNoAction)
                .HasConstraintName("FK_notificaciones_envios_notificaciones");
        });

        modelBuilder.Entity<PlantillasNotificacion>(entity =>
        {
            entity.HasKey(e => e.IdPlantilla);
            entity.ToTable("plantillas_notificacion");

            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.Property(e => e.IdPlantilla).HasColumnName("id_plantilla");
            entity.Property(e => e.Asunto)
                .HasMaxLength(200)
                .HasColumnName("asunto");
            entity.Property(e => e.ContenidoHtml).HasColumnName("contenido_html");
            entity.Property(e => e.ContenidoTexto).HasColumnName("contenido_texto");
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<RecuperacionesContrasena>(entity =>
        {
            entity.HasKey(e => e.IdRecuperacion);
            entity.ToTable("recuperaciones_contrasena");

            entity.Property(e => e.IdRecuperacion).HasColumnName("id_recuperacion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysdatetime())", "DF_recuperaciones_contrasena_fecha_creacion")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaExpiracion).HasColumnName("fecha_expiracion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("token_hash");
            entity.Property(e => e.Usado).HasColumnName("usado");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.RecuperacionesContrasenas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_recuperaciones_contrasena_usuarios");
        });

        modelBuilder.Entity<Repuesto>(entity =>
        {
            entity.HasKey(e => e.IdRepuesto);
            entity.ToTable("repuestos");

            entity.Property(e => e.IdRepuesto).HasColumnName("id_repuesto");
            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .HasColumnName("codigo");
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
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol);
            entity.ToTable("roles");

            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposDocumento>(entity =>
        {
            entity.HasKey(e => e.IdTipoDocumento);
            entity.ToTable("tipos_documento");

            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.Property(e => e.IdTipoDocumento).HasColumnName("id_tipo_documento");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposEquipo>(entity =>
        {
            entity.HasKey(e => e.IdTipoEquipo);
            entity.ToTable("tipos_equipo");

            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.Property(e => e.IdTipoEquipo).HasColumnName("id_tipo_equipo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(40)
                .HasColumnName("nombre");

            entity.HasMany(d => d.IdTipoIncidencia).WithMany(p => p.IdTipoEquipos)
                .UsingEntity<Dictionary<string, object>>(
                    "TiposIncidenciaEquipo",
                    r => r.HasOne<TiposIncidencium>().WithMany()
                        .HasForeignKey("IdTipoIncidencia")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_tipos_incidencia_equipo_tipo_incidencia"),
                    l => l.HasOne<TiposEquipo>().WithMany()
                        .HasForeignKey("IdTipoEquipo")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_tipos_incidencia_equipo_tipo_equipo"),
                    j =>
                    {
                        j.HasKey("IdTipoEquipo", "IdTipoIncidencia");
                        j.ToTable("tipos_incidencia_equipo");
                        j.IndexerProperty<int>("IdTipoEquipo").HasColumnName("id_tipo_equipo");
                        j.IndexerProperty<int>("IdTipoIncidencia").HasColumnName("id_tipo_incidencia");
                    });
        });

        modelBuilder.Entity<TiposIncidencium>(entity =>
        {
            entity.HasKey(e => e.IdTipoIncidencia);
            entity.ToTable("tipos_incidencia");

            entity.HasIndex(e => e.Nombre, "UQ_tipos_incidencia_nombre").IsUnique();

            entity.Property(e => e.IdTipoIncidencia).HasColumnName("id_tipo_incidencia");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposMantenimiento>(entity =>
        {
            entity.HasKey(e => e.IdTipoMantenimiento);
            entity.ToTable("tipos_mantenimiento");

            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.Property(e => e.IdTipoMantenimiento).HasColumnName("id_tipo_mantenimiento");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);
            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo).IsUnique();
            entity.HasIndex(e => e.NumeroDocumento).IsUnique();

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
            entity.Property(e => e.Estado)
                .HasDefaultValue(true)
                .HasColumnName("estado");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.IdTipoDocumento).HasColumnName("id_tipo_documento");
            entity.Property(e => e.IntentosFallidos)
                .HasColumnName("intentos_fallidos");

            entity.Property(e => e.BloqueoHasta)
                .HasColumnType("datetime2")
                .HasColumnName("bloqueo_hasta");

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
                .HasConstraintName("FK__usuarios__id_rol__17F790F9");

            entity.HasOne(d => d.IdTipoDocumentoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdTipoDocumento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__usuarios__id_tip__18EBB532");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
