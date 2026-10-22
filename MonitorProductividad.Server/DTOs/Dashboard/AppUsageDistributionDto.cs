namespace MonitorProductividad.Server.DTOs.Dashboard;

public class AppUsageDistributionDto
{
    public string ProcessName { get; set; } = string.Empty;
    public double TimeSpentMinutes { get; set; }
    public double Percentage { get; set; }
}