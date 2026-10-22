using Microsoft.EntityFrameworkCore;
using MonitorProductividad.Server.Entities;
using MonitorProductividad.Server.Data.Configurations;

namespace MonitorProductividad.Server.Data;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options)
    {
    }

    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplicar la configuración JSONB que creamos
        modelBuilder.ApplyConfiguration(new TelemetryRecordConfig());
        
        // Configurar rápidamente la tabla Workstation
        modelBuilder.Entity<Workstation>()
            .HasKey(w => w.MacAddress);
    }
}