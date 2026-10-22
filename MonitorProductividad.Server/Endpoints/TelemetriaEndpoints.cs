using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonitorProductividad.Server.DTOs.Ingestion;
using MonitorProductividad.Server.Services;

namespace MonitorProductividad.Server.Endpoints;

public static class TelemetriaEndpoints
{
    public static RouteGroupBuilder MapTelemetriaEndpoints(this RouteGroupBuilder group)
    {
        // Ruta base: POST /api/telemetria
        group.MapPost("/", async (TelemetryBatchRequest request, ITelemetryIngestionService ingestionService) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MacAddress) || request.TelemetryData.Count == 0)
            {
                return Results.BadRequest("El lote de telemetría está vacío o no tiene una MAC válida.");
            }

            await ingestionService.ProcesarLoteAsync(request);
            
            return Results.Ok(new { 
                Mensaje = "Lote procesado y guardado en PostgreSQL exitosamente", 
                RegistrosProcesados = request.TelemetryData.Count 
            });
        });

        return group;
    }
}