using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MonitorProductividad.Server.Data;
using MonitorProductividad.Server.DTOs.Dashboard;

namespace MonitorProductividad.Server.Services;

public class MetricsAnalyticsService : IMetricsAnalyticsService
{
    private readonly TelemetryDbContext _context;
    private const double SegundosPorMuestra = 5.0; 

    public MetricsAnalyticsService(TelemetryDbContext context)
    {
        _context = context;
    }

    public async Task<DailySummaryDto> ObtenerResumenDiarioAsync(string macAddress, DateTime fecha)
    {
        // Definimos el rango del día basándonos en la zona horaria UTC-5 (Perú/Colombia/EST)
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        // Ordenamos cronológicamente para medir cambios de contexto y bloques de foco reales
        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia)
            .OrderBy(t => t.DeviceTimestamp)
            .ToListAsync();

        if (!registros.Any())
        {
            return new DailySummaryDto { MacAddress = macAddress, Date = fecha };
        }

        var registrosActivosList = registros.Where(r => r.ProcessName != "idle.exe").ToList();
        int countInactivos = registros.Count - registrosActivosList.Count;

        double totalActiveMinutes = (registrosActivosList.Count * SegundosPorMuestra) / 60.0;
        double totalIdleMinutes = (countInactivos * SegundosPorMuestra) / 60.0;

        // 1. CPU y RAM Promedio
        double avgCpu = registrosActivosList.Any() ? registrosActivosList.Average(r => r.CpuPercentage) : 0.0;
        double avgRamGb = registrosActivosList.Any() ? registrosActivosList.Average(r => r.RamUsageMb) / 1024.0 : 0.0;

        // 2. Promedio de Apps/Ventanas en segundo plano abiertas (concurrencia)
        double avgBgWindows = registrosActivosList.Any() 
            ? registrosActivosList.Average(r => r.BackgroundWindows != null ? r.BackgroundWindows.Count : 0) 
            : 0.0;

        // 3. Cálculo del Bloque de Foco más largo y Cambios de Contexto
        int contextSwitches = 0;
        string? lastProcess = null;
        int currentStreak = 0;
        int maxStreak = 0;

        foreach (var r in registros)
        {
            if (r.ProcessName == "idle.exe")
            {
                if (currentStreak > maxStreak) maxStreak = currentStreak;
                currentStreak = 0;
            }
            else
            {
                if (lastProcess != null && r.ProcessName != lastProcess)
                {
                    contextSwitches++;
                    if (currentStreak > maxStreak) maxStreak = currentStreak;
                    currentStreak = 1;
                }
                else
                {
                    currentStreak++;
                }
                lastProcess = r.ProcessName;
            }
        }
        if (currentStreak > maxStreak) maxStreak = currentStreak;

        double longestFocusMinutes = (maxStreak * SegundosPorMuestra) / 60.0;

        // Enviamos formato ISO UTC para que React lo convierta
        string firstTime = registros.Any() ? registros.First().DeviceTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ") : "";
        string lastTime = registros.Any() ? registros.Last().DeviceTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ") : "";

        return new DailySummaryDto
        {
            MacAddress = macAddress,
            Date = fecha,
            TotalActiveMinutes = Math.Round(totalActiveMinutes, 2),
            TotalIdleMinutes = Math.Round(totalIdleMinutes, 2),
            AverageCpu = Math.Round(avgCpu, 1),
            AverageRamGb = Math.Round(avgRamGb, 1),
            ContextSwitches = contextSwitches,
            AverageBackgroundWindows = Math.Round(avgBgWindows, 1),
            LongestFocusBlockMinutes = Math.Round(longestFocusMinutes, 1),
            FirstRecordTime = firstTime,
            LastRecordTime = lastTime
        };
    }

    public async Task<List<AppUsageDistributionDto>> ObtenerDistribucionUsoAppsAsync(string macAddress, DateTime fecha)
    {
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        var registrosProductivos = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .ToListAsync();

        if (!registrosProductivos.Any()) return new List<AppUsageDistributionDto>();

        var totalRegistros = registrosProductivos.Count;

        return registrosProductivos
            .GroupBy(r => r.ProcessName)
            .Select(g => new AppUsageDistributionDto
            {
                ProcessName = g.Key,
                TimeSpentMinutes = Math.Round((g.Count() * SegundosPorMuestra) / 60.0, 2),
                Percentage = Math.Round(((double)g.Count() / totalRegistros) * 100, 1)
            })
            .OrderByDescending(x => x.Percentage)
            .ToList();
    }

    public async Task<List<AppUsageDistributionDto>> ObtenerDistribucionVentanasAsync(string macAddress, DateTime fecha)
    {
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        var registrosProductivos = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .ToListAsync();

        if (!registrosProductivos.Any()) return new List<AppUsageDistributionDto>();

        var totalRegistros = registrosProductivos.Count;

        return registrosProductivos
            .GroupBy(r => string.IsNullOrWhiteSpace(r.WindowTitle) ? r.ProcessName : r.WindowTitle)
            .Select(g => new AppUsageDistributionDto
            {
                ProcessName = g.Key, 
                TimeSpentMinutes = Math.Round((g.Count() * SegundosPorMuestra) / 60.0, 2),
                Percentage = Math.Round(((double)g.Count() / totalRegistros) * 100, 1)
            })
            .OrderByDescending(x => x.Percentage)
            .Take(10)
            .ToList();
    }

    public async Task<List<TimelineSegmentDto>> ObtenerLineaTiempoDiaAsync(string macAddress, DateTime fecha)
    {
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia)
            .OrderBy(t => t.DeviceTimestamp)
            .Select(t => t.ProcessName)
            .ToListAsync();

        if (!registros.Any()) return new List<TimelineSegmentDto>();

        int total = registros.Count;
        var segments = new List<TimelineSegmentDto>();

        string currentProc = registros[0];
        int currentCount = 0;

        for (int i = 0; i < total; i++)
        {
            if (registros[i] == currentProc)
            {
                currentCount++;
            }
            else
            {
                segments.Add(new TimelineSegmentDto
                {
                    ProcessName = currentProc,
                    Label = currentProc == "idle.exe" ? "Inactividad" : currentProc,
                    Percentage = Math.Round(((double)currentCount / total) * 100, 2)
                });
                currentProc = registros[i];
                currentCount = 1;
            }

            if (i == total - 1)
            {
                segments.Add(new TimelineSegmentDto
                {
                    ProcessName = currentProc,
                    Label = currentProc == "idle.exe" ? "Inactividad" : currentProc,
                    Percentage = Math.Round(((double)currentCount / total) * 100, 2)
                });
            }
        }

        return segments;
    }

    public async Task<List<HourlySwitchDto>> ObtenerTendenciaCambiosAsync(string macAddress, DateTime fecha)
    {
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .OrderBy(t => t.DeviceTimestamp)
            .Select(t => new { t.DeviceTimestamp, t.ProcessName })
            .ToListAsync();

        var hourlyDict = new Dictionary<string, int>();
        // Generamos un fallback inicial en el diccionario en UTC-5 para asegurar que la gráfica no esté vacía
        for (int h = 8; h <= 18; h++)
        {
            hourlyDict[$"{h:D2}:00"] = 0;
        }

        string? lastProc = null;
        foreach (var r in registros)
        {
            if (lastProc != null && r.ProcessName != lastProc)
            {
                // Enviamos el timestamp completo en UTC para que React lo agrupe localmente
                string hourKey = r.DeviceTimestamp.ToString("yyyy-MM-ddTHH:00:00Z");
                if (hourlyDict.ContainsKey(hourKey))
                    hourlyDict[hourKey]++;
                else
                    hourlyDict[hourKey] = 1;
            }
            lastProc = r.ProcessName;
        }

        return hourlyDict
            .OrderBy(k => k.Key)
            .Select(k => new HourlySwitchDto { Time = k.Key, Switches = k.Value })
            .ToList();
    }

    public async Task<List<TelemetryLogDto>> ObtenerRegistrosRecientesAsync(string macAddress, DateTime fecha, int limite = 50)
    {
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia)
            .OrderByDescending(t => t.DeviceTimestamp)
            .Take(limite)
            .ToListAsync();

        return registros.Select(r => new TelemetryLogDto
        {
            // Enviamos en formato ISO UTC para la tabla
            Time = r.DeviceTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Process = r.ProcessName,
            Window = string.IsNullOrWhiteSpace(r.WindowTitle) ? "—" : r.WindowTitle,
            Cpu = $"{Math.Round(r.CpuPercentage, 1)}%",
            Ram = $"{Math.Round(r.RamUsageMb / 1024.0, 1)} GB",
            Idle = $"{r.IdleSeconds}s",
            BackgroundWindows = r.BackgroundWindows != null 
                ? r.BackgroundWindows.Select(bw => bw.ProcessName).Distinct().ToList() 
                : new List<string>()
        }).ToList();
    }

    public async Task<object> ObtenerBackgroundStatsAsync(string macAddress, DateTime fecha)
    {
        var inicioDia = new DateTimeOffset(fecha.Date, TimeSpan.FromHours(-5)).UtcDateTime;
        var finDia = inicioDia.AddDays(1).AddTicks(-1);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .OrderBy(t => t.DeviceTimestamp)
            .ToListAsync();

        var distribucion = new Dictionary<string, int>();
        var appsAbiertasHistorico = new HashSet<string>();
        int totalVentanas = 0;

        foreach (var r in registros)
        {
            if (r.BackgroundWindows != null)
            {
                foreach (var bw in r.BackgroundWindows)
                {
                    string tituloLimpio = string.IsNullOrWhiteSpace(bw.Titulo) ? "Sin Título" : bw.Titulo;
                    string nombreCompleto = $"{tituloLimpio} ({bw.ProcessName})";
                    
                    if (distribucion.ContainsKey(nombreCompleto)) distribucion[nombreCompleto]++;
                    else distribucion[nombreCompleto] = 1;

                    appsAbiertasHistorico.Add(bw.ProcessName);
                    totalVentanas++;
                }
            }
        }

        var ultimoRegistro = registros.LastOrDefault();
        var appsActuales = ultimoRegistro?.BackgroundWindows?.Select(b => b.ProcessName).ToHashSet() ?? new HashSet<string>();

        var tags = appsAbiertasHistorico.Select(app => new {
            Name = app,
            IsOpen = appsActuales.Contains(app)
        }).OrderByDescending(t => t.IsOpen).ToList();

        var chartData = distribucion.Select(kv => new {
            Name = kv.Key,
            Percentage = totalVentanas > 0 ? Math.Round(((double)kv.Value / totalVentanas) * 100, 1) : 0
        }).OrderByDescending(x => x.Percentage).ToList();

        return new { Tags = tags, Chart = chartData };
    }
}