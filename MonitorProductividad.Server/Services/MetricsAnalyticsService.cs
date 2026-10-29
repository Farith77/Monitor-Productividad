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

    // Perú es UTC-5 todo el año (sin horario de verano)
    private static readonly TimeSpan OffsetPeru = TimeSpan.FromHours(-5);

    public MetricsAnalyticsService(TelemetryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Devuelve el rango [inicio, fin] en UTC correspondiente al día calendario peruano de 'fecha'.
    /// Ej: 2026-10-29 (Perú) => 2026-10-29 05:00 UTC .. 2026-10-30 04:59:59.9999999 UTC
    /// </summary>
    private static (DateTime inicioUtc, DateTime finUtc) RangoDiaPeru(DateTime fecha)
    {
        var inicioUtc = DateTime.SpecifyKind(fecha.Date - OffsetPeru, DateTimeKind.Utc);
        var finUtc = inicioUtc.AddDays(1).AddTicks(-1);
        return (inicioUtc, finUtc);
    }

    private static DateTime AHoraPeru(DateTime utc) => utc.Add(OffsetPeru);

    public async Task<DailySummaryDto> ObtenerResumenDiarioAsync(string macAddress, DateTime fecha)
    {
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

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

        // 1. CPU y RAM Promedio (calculados sobre el tiempo activo para no sesgar con ceros de idle)
        double avgCpu = registrosActivosList.Any() ? registrosActivosList.Average(r => r.CpuPercentage) : 0.0;
        double avgRamGb = registrosActivosList.Any() ? registrosActivosList.Average(r => r.RamUsageMb) / 1024.0 : 0.0;

        // 2. Promedio de Apps/Ventanas en segundo plano abiertas (concurrencia)
        double avgBgWindows = registrosActivosList.Any() 
            ? registrosActivosList.Average(r => r.BackgroundWindows != null ? r.BackgroundWindows.Count : 0) 
            : 0.0;

        // 3. Cálculo del Bloque de Foco más largo y Cambios de Contexto de TODO el día
        int contextSwitches = 0;
        string? lastProcess = null;
        int currentStreak = 0;
        int maxStreak = 0;

        foreach (var r in registros)
        {
            if (r.ProcessName == "idle.exe")
            {
                // Si entra en inactividad, se rompe la racha de foco continuo
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

        string firstTime = registros.Any() ? AHoraPeru(registros.First().DeviceTimestamp).ToString("HH:mm") : "--:--";
        string lastTime = registros.Any() ? AHoraPeru(registros.Last().DeviceTimestamp).ToString("HH:mm") : "--:--";

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

    // DONA 1: Distribución por Ejecutable (Todo el día)
    public async Task<List<AppUsageDistributionDto>> ObtenerDistribucionUsoAppsAsync(string macAddress, DateTime fecha)
    {
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

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

    // DONA 2: Distribución por Título de Ventana (Todo el día)
    public async Task<List<AppUsageDistributionDto>> ObtenerDistribucionVentanasAsync(string macAddress, DateTime fecha)
    {
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

        var registrosProductivos = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .ToListAsync();

        if (!registrosProductivos.Any()) return new List<AppUsageDistributionDto>();

        var totalRegistros = registrosProductivos.Count;

        return registrosProductivos
            .GroupBy(r => string.IsNullOrWhiteSpace(r.WindowTitle) ? r.ProcessName : r.WindowTitle)
            .Select(g => new AppUsageDistributionDto
            {
                ProcessName = g.Key, // Usamos la misma propiedad para el título de ventana
                TimeSpentMinutes = Math.Round((g.Count() * SegundosPorMuestra) / 60.0, 2),
                Percentage = Math.Round(((double)g.Count() / totalRegistros) * 100, 1)
            })
            .OrderByDescending(x => x.Percentage)
            .Take(10) // Top 10 ventanas del día
            .ToList();
    }

    // LÍNEA DE TIEMPO DEL PANEL INDIVIDUAL (Todo el día comprimido en bloques)
    public async Task<List<TimelineSegmentDto>> ObtenerLineaTiempoDiaAsync(string macAddress, DateTime fecha)
    {
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

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

    // CURVA DE CAMBIOS DE CONTEXTO POR HORA (Todo el día)
    public async Task<List<HourlySwitchDto>> ObtenerTendenciaCambiosAsync(string macAddress, DateTime fecha)
    {
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .OrderBy(t => t.DeviceTimestamp)
            .Select(t => new { t.DeviceTimestamp, t.ProcessName })
            .ToListAsync();

        var hourlyDict = new Dictionary<string, int>();
        for (int h = 8; h <= 18; h++)
        {
            hourlyDict[$"{h:D2}:00"] = 0;
        }

        string? lastProc = null;
        foreach (var r in registros)
        {
            if (lastProc != null && r.ProcessName != lastProc)
            {
                // Forzamos la zona horaria a UTC-5 (Perú/Colombia) sin importar dónde esté el servidor
                string hourKey = AHoraPeru(r.DeviceTimestamp).ToString("HH:00");
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

    // TABLA DE REGISTRO RECIENTE (Estricto en 50 para no saturar el frontend)
    public async Task<List<TelemetryLogDto>> ObtenerRegistrosRecientesAsync(string macAddress, DateTime fecha, int limite = 50)
    {
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia)
            .OrderByDescending(t => t.DeviceTimestamp)
            .Take(limite)
            .ToListAsync();

        return registros.Select(r => new TelemetryLogDto
        {
            // Forzamos a UTC-5
            Time = AHoraPeru(r.DeviceTimestamp).ToString("HH:mm:ss"),
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
        var (inicioDia, finDia) = RangoDiaPeru(fecha);

        var registros = await _context.TelemetryRecords
            .Where(t => t.WorkstationId == macAddress && t.DeviceTimestamp >= inicioDia && t.DeviceTimestamp <= finDia && t.ProcessName != "idle.exe")
            .OrderBy(t => t.DeviceTimestamp)
            .ToListAsync();

        var distribucion = new Dictionary<string, int>();
        var appsAbiertasHistorico = new HashSet<string>();
        int totalVentanas = 0; // NUEVO: Para calcular el porcentaje sobre el total real

        foreach (var r in registros)
        {
            if (r.BackgroundWindows != null)
            {
                foreach (var bw in r.BackgroundWindows)
                {
                    // Formato: "Título de la ventana (proceso.exe)"
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

        // Ordenamos para que las que están abiertas (verde) salgan primero
        var tags = appsAbiertasHistorico.Select(app => new {
            Name = app,
            IsOpen = appsActuales.Contains(app)
        }).OrderByDescending(t => t.IsOpen).ToList();

        // Envíamos TODO (sin Take) para que React agrupe el excedente en "Otros"
        var chartData = distribucion.Select(kv => new {
            Name = kv.Key,
            Percentage = totalVentanas > 0 ? Math.Round(((double)kv.Value / totalVentanas) * 100, 1) : 0
        }).OrderByDescending(x => x.Percentage).ToList();

        return new { Tags = tags, Chart = chartData };
    }
}