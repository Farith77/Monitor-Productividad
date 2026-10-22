using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MonitorProductividad.Agent.Trackers;
using MonitorProductividad.Agent.Data;

namespace MonitorProductividad.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== AGENTE DE MONITOREO V4.5 (FASE DE PULIDO COMPLETADA) ===");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            int samplingRateSeconds = configuration.GetValue<int>("AgentSettings:SamplingRateSeconds", 5);
            double umbralInactividadSegundos = configuration.GetValue<double>("AgentSettings:InactivityThresholdSeconds", 60.0);
            int syncIntervalSeconds = configuration.GetValue<int>("AgentSettings:SyncIntervalSeconds", 300);
            string centralApiUrl = configuration.GetValue<string>("AgentSettings:CentralApiUrl", "");

            Console.WriteLine($"[CONFIG] Muestreo: {samplingRateSeconds}s | Inactividad: {umbralInactividadSegundos}s | Sincronización: {syncIntervalSeconds}s");
            
            Console.WriteLine("[INFO] Verificando infraestructura de datos local...");
            LocalDbContext.InicializarBaseDatos();
            Console.WriteLine("[INFO] Base de datos SQLite operativa.");
            Console.WriteLine("--------------------------------------------------");

            DateTime proximaSincronizacion = DateTime.Now.AddSeconds(syncIntervalSeconds);

            while (true)
            {
                // 1. Telemetría de bajo nivel
                var infoVentana = ActiveWindowTracker.GetActiveWindowProcess();
                var todasLasVentanas = ActiveWindowTracker.ObtenerVentanasAbiertas();
                double cpuActual = CpuTracker.ObtenerPorcentajeCpu(infoVentana.Proceso);
                var recursos = SystemResourcesTracker.ObtenerResumenRecursos();
                double segundosInactivo = IdleTracker.ObtenerTiempoInactivoSegundos();

                // Filtrar ventanas en segundo plano excluyendo la que está en foco
                int pidFoco = infoVentana.Proceso?.Id ?? 0;
                var ventanasSegundoPlano = todasLasVentanas
                    .Where(v => v.Pid != pidFoco || v.Titulo != infoVentana.Titulo)
                    .ToList();

                string ventanasJson = JsonSerializer.Serialize(ventanasSegundoPlano);

                string ventanaAGuardar;
                string ejecutableAGuardar;
                double cpuAGuardar;

                // 2. Máquina de Estados Finita (FSM)
                if (segundosInactivo >= umbralInactividadSegundos)
                {
                    ventanaAGuardar = "Sistema - Usuario Inactivo";
                    ejecutableAGuardar = "idle.exe";
                    cpuAGuardar = 0.0; 
                    
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INACTIVO] Ausente ({segundosInactivo:F0}s).");
                }
                else
                {
                    ventanaAGuardar = infoVentana.Titulo;
                    ejecutableAGuardar = infoVentana.ProcessName;
                    cpuAGuardar = cpuActual;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ACTIVO] {ejecutableAGuardar} ({cpuAGuardar}%) | Fondo: {ventanasSegundoPlano.Count} ventanas");
                }
                Console.ResetColor();

                // 3. Persistencia en SQLite incluyendo ventanas de segundo plano
                LocalDbContext.GuardarTelemetria(
                    ventanaAGuardar, 
                    ejecutableAGuardar, 
                    cpuAGuardar, 
                    (long)recursos.ramTotal, 
                    segundosInactivo, 
                    ventanasJson
                );

                // 4. Disparo Asíncrono no bloqueante de la Sincronización Web
                if (DateTime.Now >= proximaSincronizacion)
                {
                    Task.Run(() => HttpSyncClient.SincronizarDatosAsync(centralApiUrl));
                    proximaSincronizacion = DateTime.Now.AddSeconds(syncIntervalSeconds);
                }

                Thread.Sleep(samplingRateSeconds * 1000); 
            }
        }
    }
}