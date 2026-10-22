namespace MonitorProductividad.Server.Entities;

public class BackgroundWindow
{
    public int Pid { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
}