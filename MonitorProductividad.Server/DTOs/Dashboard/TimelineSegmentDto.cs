namespace MonitorProductividad.Server.DTOs.Dashboard;

public class TimelineSegmentDto
{
    public string Label { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public string ProcessName { get; set; } = string.Empty;
}

public class HourlySwitchDto
{
    public string Time { get; set; } = string.Empty; // Ej: "09:00"
    public int Switches { get; set; }
}