using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MonitorProductividad.Server.Services;

namespace MonitorProductividad.Server.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        // 1. Resumen completo del día (Tiempo activo/inactivo, CPU/RAM promedio, Foco máximo, Concurrencia, Cambios de contexto)
        group.MapGet("/resumen", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerResumenDiarioAsync(macAddress, fecha));
        });

        // 2. Dona 1: Distribución por Ejecutable (Todo el día)
        group.MapGet("/distribucion", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerDistribucionUsoAppsAsync(macAddress, fecha));
        });

        // 3. Dona 2: Distribución por Ventanas (Todo el día)
        group.MapGet("/distribucion-ventanas", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerDistribucionVentanasAsync(macAddress, fecha));
        });

        // 4. Línea de tiempo del día completo para el Panel Individual
        group.MapGet("/linea-tiempo", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerLineaTiempoDiaAsync(macAddress, fecha));
        });

        // 5. Tendencia de cambios de contexto por hora (Todo el día)
        group.MapGet("/tendencia-cambios", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerTendenciaCambiosAsync(macAddress, fecha));
        });

        // 6. Historial reciente (DEVUELTO A 50 REGISTROS para proteger el navegador)
        group.MapGet("/registros", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerRegistrosRecientesAsync(macAddress, fecha, 50));
        });

        //  7. endpoints para backgroud del dia
        group.MapGet("/background-stats", async (
            [FromQuery] string macAddress, 
            [FromQuery] DateTime fecha, 
            IMetricsAnalyticsService analyticsService) =>
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return Results.BadRequest("La MAC Address es obligatoria.");
            return Results.Ok(await analyticsService.ObtenerBackgroundStatsAsync(macAddress, fecha));
        });

        return group;
    }
}