using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonitorProductividad.Server.DTOs.Ingestion;

public class TelemetryBatchRequest
{
    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("device_timestamp")]
    public string DeviceTimestamp { get; set; } = string.Empty;

    [JsonPropertyName("telemetry_data")]
    public List<TelemetryItemDto> TelemetryData { get; set; } = new();
}