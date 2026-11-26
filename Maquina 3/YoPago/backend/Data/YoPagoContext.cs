using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class YoPagoContext : DbContext
{
    public YoPagoContext(DbContextOptions<YoPagoContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<UsuarioDirectorio> Directorio => Set<UsuarioDirectorio>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MensajeSalida> MensajesSalida => Set<MensajeSalida>();

    // Las columnas salen en snake_case (UseSnakeCaseNamingConvention); Data/schema.sql debe mantenerse alineado.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.Property(x => x.Cedula).HasMaxLength(12);
            e.HasIndex(x => x.Cedula).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Correo).HasMaxLength(200);
            e.Property(x => x.TokenRecuperacionHash).HasMaxLength(64);
        });

        modelBuilder.Entity<UsuarioDirectorio>(e =>
        {
            e.ToTable("directorio_activo");
            e.HasKey(x => x.Cedula);
            e.Property(x => x.Cedula).HasMaxLength(12);
            e.Property(x => x.Clave).HasMaxLength(100);
        });

        modelBuilder.Entity<Compra>(e =>
        {
            e.ToTable("compras");
            e.Property(x => x.NumeroCompra).HasMaxLength(40);
            e.HasIndex(x => x.NumeroCompra).IsUnique();
            e.Property(x => x.Descripcion).HasMaxLength(300);
            e.Property(x => x.CodigoSubasta).HasMaxLength(40);
            e.Property(x => x.ValorOferta).HasPrecision(18, 2);
            e.Property(x => x.Impuestos).HasPrecision(18, 2);
            e.Property(x => x.Comision).HasPrecision(18, 2);
            e.Property(x => x.ValorTotal).HasPrecision(18, 2);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.ClienteId, x.Estado });
            e.HasOne(x => x.Cliente).WithMany(c => c.Compras).HasForeignKey(x => x.ClienteId);
            // Concurrencia optimista con la columna de sistema xmin de PostgreSQL.
            e.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        });

        modelBuilder.Entity<Pago>(e =>
        {
            e.ToTable("pagos");
            e.Property(x => x.CodigoTransaccion).HasMaxLength(20);
            e.HasIndex(x => x.CodigoTransaccion).IsUnique();
            e.Property(x => x.MedioPago).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Banco).HasMaxLength(60);
            e.Property(x => x.TipoCuenta).HasMaxLength(30);
            e.Property(x => x.CorreoComprobante).HasMaxLength(200);
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.Instrumento).HasMaxLength(200);
            e.Property(x => x.ReferenciaPasarela).HasMaxLength(100);
            e.Property(x => x.CodigoFallo).HasMaxLength(20);
            e.Property(x => x.MensajeFallo).HasMaxLength(300);
            e.Property(x => x.CodigoAcuseSso).HasMaxLength(60);
            e.HasOne(x => x.Compra).WithMany(c => c.Pagos).HasForeignKey(x => x.CompraId);
            // Anti doble pago a nivel de BD: solo un cobro "Iniciado" por compra.
            e.HasIndex(x => x.CompraId).IsUnique().HasFilter("estado = 'Iniciado'")
                .HasDatabaseName("ux_pagos_un_iniciado_por_compra");
        });

        modelBuilder.Entity<MensajeSalida>(e =>
        {
            e.ToTable("mensajes_salida");
            e.Property(x => x.Tipo).HasMaxLength(80);
            e.Property(x => x.Contenido).HasColumnType("jsonb");
            e.Property(x => x.UltimoError).HasMaxLength(500);
            e.HasIndex(x => new { x.ProximoIntentoEn, x.CreadoEn }).HasFilter("procesado_en IS NULL")
                .HasDatabaseName("ix_mensajes_salida_pendientes");
            e.HasIndex(x => x.AgregadoId);
        });
    }
}
