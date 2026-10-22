using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MonitorProductividad.Server.Data;

namespace MonitorProductividad.Server.Endpoints;

public static class DispositivosEndpoints
{
    public static RouteGroupBuilder MapDispositivosEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/dispositivos
        group.MapGet("/", async (TelemetryDbContext context) =>
        {
            // Consultamos directamente la base de datos sin pasar por un servicio para simplificar
            var workstations = await context.Workstations.ToListAsync();
            return Results.Ok(workstations);
        });

        return group;
    }
}