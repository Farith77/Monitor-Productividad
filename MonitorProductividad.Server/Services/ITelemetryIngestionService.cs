using System.Threading.Tasks;
using MonitorProductividad.Server.DTOs.Ingestion;

namespace MonitorProductividad.Server.Services;

public interface ITelemetryIngestionService
{
    Task ProcesarLoteAsync(TelemetryBatchRequest request);
}