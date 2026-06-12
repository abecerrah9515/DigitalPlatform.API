using DigitalPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<ConsolidacionLog> ConsolidacionLogs => Set<ConsolidacionLog>();
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();
    public DbSet<Sociedad> Sociedades => Set<Sociedad>();
    public DbSet<CeBe> CeBes => Set<CeBe>();
    public DbSet<Industria> Industrias => Set<Industria>();
    public DbSet<CargaArchivo> CargasArchivo => Set<CargaArchivo>();
    public DbSet<BaseCliente> BaseClientes => Set<BaseCliente>();
    public DbSet<ControlFactura> ControlFacturas => Set<ControlFactura>();
    public DbSet<ReporteCarteraFactura> ReporteCarteraFacturas => Set<ReporteCarteraFactura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Industria).HasMaxLength(100);
            entity.Property(e => e.Cliente).HasMaxLength(200);
            entity.Property(e => e.CodProyecto).HasMaxLength(100);
            entity.Property(e => e.CeBe).HasMaxLength(200);
            entity.Property(e => e.Responsable).HasMaxLength(200);
            entity.Property(e => e.Area).HasMaxLength(100);
            entity.Property(e => e.Sociedad).HasMaxLength(100);
            entity.Property(e => e.Vertical).HasMaxLength(100);
            entity.Property(e => e.Pais).HasMaxLength(100);
            entity.Property(e => e.IngresoReal).HasPrecision(18, 2);
            entity.Property(e => e.IngresoPlaneado).HasPrecision(18, 2);
            entity.Property(e => e.CostoReal).HasPrecision(18, 2);
            entity.Property(e => e.CostoPlaneado).HasPrecision(18, 2);
            entity.Property(e => e.Horas).HasPrecision(18, 2);
            entity.Ignore(e => e.Ingreso);
            entity.Ignore(e => e.Costo);
            entity.Ignore(e => e.GM);
            entity.Ignore(e => e.GMPorcentaje);
            entity.Ignore(e => e.TarifaEntrega);
            entity.HasOne(e => e.Consolidacion)
                  .WithMany(c => c.Proyectos)
                  .HasForeignKey(e => e.ConsolidacionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsolidacionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Estado).HasConversion<string>();
            entity.Property(e => e.IniciadoPor).HasMaxLength(200);
            entity.Property(e => e.Errores).HasColumnType("text");
        });

        modelBuilder.Entity<TipoCambio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Moneda).HasMaxLength(10);
            entity.Property(e => e.Tasa).HasPrecision(18, 6);
            entity.HasIndex(e => new { e.Año, e.Mes, e.Moneda }).IsUnique();
        });

        modelBuilder.Entity<Sociedad>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.RazonSocial).HasMaxLength(200);
            entity.Property(e => e.Pais).HasMaxLength(100);
        });

        modelBuilder.Entity<CeBe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.CeBeGroup).HasMaxLength(100);
            entity.Property(e => e.Nombre).HasMaxLength(200);
        });

        modelBuilder.Entity<Industria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodIndustria).HasMaxLength(20);
            entity.Property(e => e.Vertical).HasMaxLength(100);
        });

        modelBuilder.Entity<CargaArchivo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NombreArchivo).HasMaxLength(255);
            entity.Property(e => e.FechaCarga).IsRequired();
            entity.Property(e => e.RutaArchivo).HasMaxLength(500);
        });

        modelBuilder.Entity<BaseCliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CargaArchivo)
                  .WithMany()
                  .HasForeignKey(e => e.CargaArchivoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ControlFactura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Valor).HasPrecision(18, 2);
            entity.Property(e => e.Iva).HasPrecision(18, 2);
            entity.Property(e => e.ReteIva).HasPrecision(18, 2);
            entity.Property(e => e.Autorenta).HasPrecision(18, 2);
            entity.Property(e => e.Retencion).HasPrecision(18, 2);
            entity.Property(e => e.Ica).HasPrecision(18, 2);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.Trm).HasPrecision(18, 2);
            entity.Property(e => e.ValorUsd).HasPrecision(18, 2);
            entity.Property(e => e.ValorAnulacion).HasPrecision(18, 2);
            entity.Property(e => e.ValorCancelar).HasPrecision(18, 2);
            entity.HasOne(e => e.CargaArchivo)
                  .WithMany()
                  .HasForeignKey(e => e.CargaArchivoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReporteCarteraFactura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValorRecibir).HasPrecision(18, 2);
            entity.Property(e => e.ImporteMonedaLocal).HasPrecision(18, 2);
            entity.Property(e => e.ImporteMonedaDoc).HasPrecision(18, 2);
            entity.Property(e => e.VencidoEnTiempo).HasPrecision(18, 2);
            entity.Property(e => e.Vencido0_15).HasPrecision(18, 2);
            entity.Property(e => e.Vencido16_30).HasPrecision(18, 2);
            entity.Property(e => e.Vencido31_60).HasPrecision(18, 2);
            entity.Property(e => e.Vencido61_90).HasPrecision(18, 2);
            entity.Property(e => e.Vencido91_120).HasPrecision(18, 2);
            entity.Property(e => e.Vencido121_365).HasPrecision(18, 2);
            entity.HasOne(e => e.CargaArchivo)
                  .WithMany()
                  .HasForeignKey(e => e.CargaArchivoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
