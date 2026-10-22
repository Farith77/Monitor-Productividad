using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MonitorProductividad.Server.Data;
using MonitorProductividad.Server.DTOs.Ingestion;
using MonitorProductividad.Server.Entities;

namespace MonitorProductividad.Server.Services;

public class TelemetryIngestionService : ITelemetryIngestionService
{
    private readonly TelemetryDbContext _context;

    public TelemetryIngestionService(TelemetryDbContext context)
    {
        _context = context;
    }

    public async Task ProcesarLoteAsync(TelemetryBatchRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MacAddress) || !request.TelemetryData.Any())
        {
            return; // Validamos que el lote no venga vacío
        }

        // 1. Verificar si la máquina ya existe, si no, la registramos automáticamente
        var workstation = await _context.Workstations
            .FirstOrDefaultAsync(w => w.MacAddress == request.MacAddress);

        if (workstation == null)
        {
            workstation = new Workstation
            {
                MacAddress = request.MacAddress,
                Hostname = "Desconocido", // Podríamos agregarlo al payload en el futuro
                RolAsignado = "Sin Asignar"
            };
            _context.Workstations.Add(workstation);
        }

        // 2. Transformar los DTOs a Entidades de Base de Datos
        var registros = new List<TelemetryRecord>();
        foreach (var item in request.TelemetryData)
        {
            // Convertimos el string JSON nativo enviado por el agente a nuestra lista de objetos
            List<BackgroundWindow> backgroundWindows = new();
            if (!string.IsNullOrWhiteSpace(item.BackgroundWindows) && item.BackgroundWindows != "[]")
            {
                try
                {
                    backgroundWindows = JsonSerializer.Deserialize<List<BackgroundWindow>>(item.BackgroundWindows) ?? new();
                }
                catch
                {
                    // Ignoramos silenciosamente si hay un error de parseo en una ventana
                }
            }

            registros.Add(new TelemetryRecord
            {
                DeviceTimestamp = DateTime.SpecifyKind(DateTime.Parse(item.Timestamp), DateTimeKind.Utc),
                ProcessName = item.ProcessName,
                WindowTitle = item.WindowTitle,
                CpuPercentage = item.CpuPercentage,
                RamUsageMb = item.RamUsageMb,
                IdleSeconds = item.IdleSeconds,
                BackgroundWindows = backgroundWindows,
                WorkstationId = workstation.MacAddress
            });
        }

        // 3. Guardar todo el bloque masivo en la base de datos
        _context.TelemetryRecords.AddRange(registros);
        await _context.SaveChangesAsync();
    }
}