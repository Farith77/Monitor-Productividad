using System.Collections.Generic;

namespace MonitorProductividad.Server.Entities;

public class Workstation
{
    public string MacAddress { get; set; } = string.Empty; // Llave Primaria
    public string Hostname { get; set; } = string.Empty;
    public string RolAsignado { get; set; } = string.Empty;

    // Relación uno a muchos: Una PC tiene muchos registros de telemetría
    public ICollection<TelemetryRecord> TelemetryRecords { get; set; } = new List<TelemetryRecord>();
}