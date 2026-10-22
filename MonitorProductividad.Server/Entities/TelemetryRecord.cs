using System;
using System.Collections.Generic;

namespace MonitorProductividad.Server.Entities;

public class TelemetryRecord
{
    public long Id { get; set; } // Llave Primaria Autoincremental
    public DateTime DeviceTimestamp { get; set; }
    
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public double CpuPercentage { get; set; }
    public int RamUsageMb { get; set; }
    public double IdleSeconds { get; set; }
    
    // Lista que se guardará como JSONB
    public List<BackgroundWindow> BackgroundWindows { get; set; } = new();

    // Llave Foránea hacia Workstation
    public string WorkstationId { get; set; } = string.Empty;
    public Workstation Workstation { get; set; } = null!;
}