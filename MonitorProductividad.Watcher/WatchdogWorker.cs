using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonitorProductividad.Watcher
{
    public class WatchdogWorker : BackgroundService
    {
        private readonly ILogger<WatchdogWorker> _logger;
        private const string AgentProcessName = "MonitorProductividad.Agent"; 
        private const string AgentPath = @"C:\Program Files\MonitorProductividad\MonitorProductividad.Agent.exe"; 

        public WatchdogWorker(ILogger<WatchdogWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio Watchdog iniciado en Sesión 0.");

            while (!stoppingToken.IsCancellationRequested)
            {
                Process[] processes = Process.GetProcessesByName(AgentProcessName);

                if (processes.Length == 0)
                {
                    _logger.LogWarning("¡ALERTA! Agente caído. Ejecutando puente Inter-Sesión...");
                    LevantarAgenteEnSesionUsuario();
                }

                await Task.Delay(10000, stoppingToken);
            }
        }

        private void LevantarAgenteEnSesionUsuario()
        {
            if (!File.Exists(AgentPath))
            {
                _logger.LogError($"No se encontró el ejecutable en {AgentPath}");
                return;
            }

            try
            {
                // 1. Obtener el ID de la sesión del usuario activo (Sesión 1+)
                uint activeSessionId = WTSGetActiveConsoleSessionId();
                if (activeSessionId == 0xFFFFFFFF)
                {
                    _logger.LogWarning("No hay ningún usuario logueado en la consola.");
                    return; 
                }

                // 2. Obtener el token de seguridad de ese usuario
                if (!WTSQueryUserToken(activeSessionId, out IntPtr userToken))
                {
                    int err = Marshal.GetLastWin32Error();
                    _logger.LogError($"Fallo al obtener el token del usuario activo. Error: {err}");
                    return;
                }

                // 3. NUEVO: Crear el bloque de variables de entorno correctas para el usuario
                IntPtr envBlock = IntPtr.Zero;
                CreateEnvironmentBlock(out envBlock, userToken, false);
                uint dwCreationFlags = envBlock != IntPtr.Zero ? 0x00000400U : 0; // 0x00000400 = CREATE_UNICODE_ENVIRONMENT

                // 4. Configurar el entorno de inicio forzando el escritorio interactivo
                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default";

                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
                string workingDirectory = Path.GetDirectoryName(AgentPath)!;
                string commandLine = $"\"{AgentPath}\"";

                // 5. Lanzar el proceso inyectando el bloque de entorno
                bool result = CreateProcessAsUser(
                    userToken,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    dwCreationFlags,
                    envBlock,
                    workingDirectory,
                    ref si,
                    out pi);

                if (result)
                {
                    _logger.LogInformation("Agente resucitado con variables de entorno correctas.");
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    _logger.LogError($"Fallo al crear el proceso. Error: {err}");
                }

                // Limpieza estricta de memoria no administrada
                if (envBlock != IntPtr.Zero) DestroyEnvironmentBlock(envBlock);
                CloseHandle(userToken);
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción crítica en el puente Inter-Sesión: {ex.Message}");
            }
        }

        #region P/Invoke Win32 API (Cruce de Sesiones)

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string? lpReserved;
            public string? lpDesktop;
            public string? lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        // NUEVO: API para generar variables de entorno (AppData, Temp, etc)
        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string? lpApplicationName,
            string? lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        #endregion
    }
}