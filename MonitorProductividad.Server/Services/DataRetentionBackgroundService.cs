using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitorProductividad.Server.Data;

namespace MonitorProductividad.Server.Services;

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataRetentionBackgroundService> _logger;
    
    // Retención de 3 meses
    private const int RetencionDias = 90; 

    public DataRetentionBackgroundService(IServiceProvider serviceProvider, ILogger<DataRetentionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de retención de datos en PostgreSQL iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Inyectamos un "Scope" para poder instanciar el DbContext dentro de un proceso en segundo plano
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
                    
                    var fechaLimite = DateTime.UtcNow.AddDays(-RetencionDias);
                    
                    // ExecuteDeleteAsync es una funcionalidad nativa de EF Core 7+ que ejecuta un DELETE directo 
                    // en PostgreSQL sin necesidad de cargar los datos masivos en la memoria RAM del servidor.
                    var registrosEliminados = await context.TelemetryRecords
                        .Where(t => t.DeviceTimestamp < fechaLimite)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (registrosEliminados > 0)
                    {
                        _logger.LogInformation($"[MANTENIMIENTO] Se purgaron {registrosEliminados} registros de telemetría anteriores a {fechaLimite:yyyy-MM-dd}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al intentar purgar datos antiguos de PostgreSQL.");
            }

            // Dormimos el hilo durante 24 horas hasta el próximo ciclo de mantenimiento
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}