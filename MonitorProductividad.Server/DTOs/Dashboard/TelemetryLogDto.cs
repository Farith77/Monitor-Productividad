using System.Collections.Generic;

namespace MonitorProductividad.Server.DTOs.Dashboard;

public class TelemetryLogDto
{
    public string Time { get; set; } = string.Empty;     // Ej: "14:32:05"
    public string Process { get; set; } = string.Empty;  // Ej: "Code.exe"
    public string Window { get; set; } = string.Empty;   // Ej: "mockup.jsx — proyecto"
    public string Cpu { get; set; } = string.Empty;      // Ej: "42%"
    public string Ram { get; set; } = string.Empty;      // Ej: "5.8 GB"
    public string Idle { get; set; } = string.Empty;     // Ej: "0s"
    
    // Lista plana solo con los nombres de las aplicaciones de fondo (para las insignias)
    public List<string> BackgroundWindows { get; set; } = new();
}