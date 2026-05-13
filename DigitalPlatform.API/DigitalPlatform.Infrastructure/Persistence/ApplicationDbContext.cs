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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Industria).HasMaxLength(100);
            entity.Property(e => e.Cliente).HasMaxLength(200);
            entity.Property(e => e.CodProyecto).HasMaxLength(50);
            entity.Property(e => e.CeBe).HasMaxLength(50);
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
    }
}
