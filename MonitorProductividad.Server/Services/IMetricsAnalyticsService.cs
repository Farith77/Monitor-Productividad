using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonitorProductividad.Server.DTOs.Dashboard;

namespace MonitorProductividad.Server.Services;

public interface IMetricsAnalyticsService
{
    Task<DailySummaryDto> ObtenerResumenDiarioAsync(string macAddress, DateTime fecha);
    
    // Gráficos de dona (Todo el día)
    Task<List<AppUsageDistributionDto>> ObtenerDistribucionUsoAppsAsync(string macAddress, DateTime fecha);
    Task<List<AppUsageDistributionDto>> ObtenerDistribucionVentanasAsync(string macAddress, DateTime fecha);
    
    // Línea de tiempo y Curva de fatiga (Todo el día)
    Task<List<TimelineSegmentDto>> ObtenerLineaTiempoDiaAsync(string macAddress, DateTime fecha);
    Task<List<HourlySwitchDto>> ObtenerTendenciaCambiosAsync(string macAddress, DateTime fecha);
    
    // Tabla de registro reciente (Limitado a 50 para velocidad)
    Task<List<TelemetryLogDto>> ObtenerRegistrosRecientesAsync(string macAddress, DateTime fecha, int limite = 50);

    Task<object> ObtenerBackgroundStatsAsync(string macAddress, DateTime fecha);
}