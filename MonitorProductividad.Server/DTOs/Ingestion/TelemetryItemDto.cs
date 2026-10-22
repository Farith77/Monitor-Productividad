using System.Text.Json.Serialization;

namespace MonitorProductividad.Server.DTOs.Ingestion;

public class TelemetryItemDto
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("process_name")]
    public string ProcessName { get; set; } = string.Empty;

    [JsonPropertyName("window_title")]
    public string WindowTitle { get; set; } = string.Empty;

    [JsonPropertyName("cpu_percentage")]
    public double CpuPercentage { get; set; }

    [JsonPropertyName("ram_usage_mb")]
    public int RamUsageMb { get; set; }

    [JsonPropertyName("idle_seconds")]
    public double IdleSeconds { get; set; }

    [JsonPropertyName("background_windows")]
    public string BackgroundWindows { get; set; } = "[]";
}