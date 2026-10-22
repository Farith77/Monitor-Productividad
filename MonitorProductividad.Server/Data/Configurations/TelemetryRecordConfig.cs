using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorProductividad.Server.Entities;

namespace MonitorProductividad.Server.Data.Configurations;

public class TelemetryRecordConfig : IEntityTypeConfiguration<TelemetryRecord>
{
    public void Configure(EntityTypeBuilder<TelemetryRecord> builder)
    {
        // Definir tabla y llave primaria
        builder.ToTable("TelemetryRecords");
        builder.HasKey(t => t.Id);

        // Restricciones de tamaño para optimizar PostgreSQL
        builder.Property(t => t.ProcessName).IsRequired().HasMaxLength(150);
        builder.Property(t => t.WindowTitle).HasMaxLength(500);

        // Mapeo mágico: Le decimos a Npgsql que guarde esta lista como JSONB nativo
        builder.Property(t => t.BackgroundWindows)
               .HasColumnType("jsonb");

        // Configuración de la relación
        builder.HasOne(t => t.Workstation)
               .WithMany(w => w.TelemetryRecords)
               .HasForeignKey(t => t.WorkstationId)
               .OnDelete(DeleteBehavior.Cascade); // Si borramos una PC, se borra su historial
    }
}