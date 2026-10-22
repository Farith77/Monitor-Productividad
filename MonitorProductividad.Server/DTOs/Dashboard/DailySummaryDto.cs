using System;

namespace MonitorProductividad.Server.DTOs.Dashboard;

public class DailySummaryDto
{
    public string MacAddress { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double TotalActiveMinutes { get; set; }
    public double TotalIdleMinutes { get; set; }
    
    // 1. Métricas restauradas para el Panel Individual (reemplazan a Productividad y Total Muestras)
    public double AverageCpu { get; set; }
    public double AverageRamGb { get; set; }
    
    // 2. Métricas de Concurrencia y Foco (calculadas con TODO el día, no solo 50 registros)
    public int ContextSwitches { get; set; }
    public double AverageBackgroundWindows { get; set; }
    public double LongestFocusBlockMinutes { get; set; }
}