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
    public DbSet<PlanVerticalP26> PlanesVerticalP26 => Set<PlanVerticalP26>();
    public DbSet<CuentaPnl> CuentasPnl => Set<CuentaPnl>();
    public DbSet<MovimientoGR55> MovimientosGR55 => Set<MovimientoGR55>();

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

        modelBuilder.Entity<PlanVerticalP26>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Vertical).HasMaxLength(100);
            entity.Property(e => e.IngresoPlan).HasPrecision(18, 2);
            entity.Property(e => e.CostoPlan).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.ConsolidacionId, e.Vertical, e.Año, e.Mes });
            entity.HasOne(e => e.Consolidacion)
                  .WithMany()
                  .HasForeignKey(e => e.ConsolidacionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CuentaPnl>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LineItemId).HasMaxLength(60);
            entity.Property(e => e.AccountName).HasMaxLength(300);
            entity.Property(e => e.ParentId).HasMaxLength(60);
            entity.Property(e => e.TipoFinanciero).HasMaxLength(30);
            entity.Property(e => e.Referencia).HasColumnType("text");
            entity.HasIndex(e => new { e.ConsolidacionId, e.ParentId });
            entity.HasOne(e => e.Consolidacion)
                  .WithMany()
                  .HasForeignKey(e => e.ConsolidacionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovimientoGR55>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NumeroCuenta).HasMaxLength(60);
            entity.Property(e => e.CodProyecto).HasMaxLength(100);
            entity.Property(e => e.Cliente).HasMaxLength(200);
            entity.Property(e => e.Vertical).HasMaxLength(100);
            entity.Property(e => e.Valor).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.ConsolidacionId, e.NumeroCuenta, e.Año, e.Mes });
            entity.HasOne(e => e.Consolidacion)
                  .WithMany()
                  .HasForeignKey(e => e.ConsolidacionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
