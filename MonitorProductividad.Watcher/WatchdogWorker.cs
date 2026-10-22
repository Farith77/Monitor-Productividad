using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonitorProductividad.Watcher
{
    public class WatchdogWorker : BackgroundService
    {
        private readonly ILogger<WatchdogWorker> _logger;
        // Nombre exacto del proceso del agente
        private const string AgentProcessName = "MonitorProductividad.Agent"; 
        // Ruta de instalación final en el entorno de despliegue
        private const string AgentPath = @"C:\Program Files\MonitorTesis\MonitorProductividad.Agent.exe";

        public WatchdogWorker(ILogger<WatchdogWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio Watchdog iniciado en Sesión 0.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Watchdog verificando estado del Agente de Telemetría...");

                // 1. Buscar si el proceso del Agente de Usuario está corriendo en el sistema
                Process[] processes = Process.GetProcessesByName(AgentProcessName);

                if (processes.Length == 0)
                {
                    _logger.LogWarning("¡ALERTA! El Agente de Telemetría no está ejecutándose. Iniciando reanimación...");
                    LevantarAgenteEnSesionUsuario();
                }
                else
                {
                    _logger.LogInformation("Agente operando normalmente. Hilos activos detectados.");
                }

                // Evalúa el estado cada 10 segundos (puedes parametrizarlo en el appsettings)
                await Task.Delay(10000, stoppingToken);
            }
        }

        private void LevantarAgenteEnSesionUsuario()
        {
            try
            {
                if (!File.Exists(AgentPath))
                {
                    _logger.LogError($"Error de despliegue: No se encontró el ejecutable en {AgentPath}");
                    return;
                }

                // NOTA INGENIERIL DE SESIÓN 0:
                // Un Process.Start simple aquí abriría el agente dentro de la Sesión 0 (invisible).
                // Para una fase de pruebas local de la tesis, puedes simular el levantamiento básico:
                var startInfo = new ProcessStartInfo
                {
                    FileName = AgentPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
                _logger.LogInformation("Comando de reanimación enviado exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fallo al intentar resucitar el agente: {ex.Message}");
            }
        }
    }
}