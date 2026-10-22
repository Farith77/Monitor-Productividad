using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MonitorProductividad.Agent.Trackers
{
    public class VentanaItem
    {
        public int Pid { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
    }

    public class ActiveWindowTracker
    {
        // Delegado y llamadas Win32 para enumerar ventanas
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Lista de procesos nativos de Windows que ensucian la telemetría
        private static readonly HashSet<string> ProcesosDelSistema = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer",
            "lockapp",
            "systemsettings",
            "applicationframehost",
            "textinputhost",
            "searchapp",
            "startmenuexperiencehost",
            "shellexperiencehost",
            "windowsterminal",
            "conhost",
            "searchhost" // Agregado común en Windows 11
        };

        /// <summary>
        /// Obtiene la ventana y proceso actualmente en primer plano (Foreground).
        /// </summary>
        public static (string Titulo, string ProcessName, Process? Proceso) GetActiveWindowProcess()
        {
            IntPtr handle = GetForegroundWindow();
            StringBuilder buff = new StringBuilder(256);
            string titulo = "Desconocido";

            if (GetWindowText(handle, buff, 256) > 0)
            {
                titulo = buff.ToString();
            }

            try
            {
                GetWindowThreadProcessId(handle, out uint pid);
                
                if (pid == 0) return (titulo, "idle.exe", null);

                // BLINDAJE: Obtener el proceso puede fallar si se cierra en este preciso milisegundo
                Process proceso = Process.GetProcessById((int)pid);
                string nombreExe = proceso.ProcessName + ".exe";

                return (titulo, nombreExe, proceso);
            }
            catch (ArgumentException)
            {
                // CRASH-PROOFING: El proceso se cerró en medio de la lectura
                return ("Ventana Cerrándose", "desconocido.exe", null);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // CRASH-PROOFING: Procesos del sistema protegidos
                return (titulo, "system_protected.exe", null);
            }
            catch
            {
                return (titulo, "desconocido.exe", null);
            }
        }

        /// <summary>
        /// Enumera todas las ventanas activas y visibles en el sistema operativo.
        /// </summary>
        public static List<VentanaItem> ObtenerVentanasAbiertas()
        {
            var ventanas = new List<VentanaItem>();

            EnumWindows((hWnd, lParam) =>
            {
                // 1. Filtrar ventanas invisibles
                if (!IsWindowVisible(hWnd)) return true;

                // 2. Filtrar ventanas sin título (procesos fantasma/ocultos)
                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                var builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                string titulo = builder.ToString().Trim();

                if (string.IsNullOrWhiteSpace(titulo)) return true;

                // 3. Obtener PID
                GetWindowThreadProcessId(hWnd, out uint pid);
                string rawProcessName = "desconocido";

                // 4. Buscar el nombre real del proceso PRIMERO
                try
                {
                    using (var proc = Process.GetProcessById((int)pid))
                    {
                        rawProcessName = proc.ProcessName;
                    }
                }
                catch
                {
                    // El proceso pudo cerrarse antes de resolver el nombre
                }

                //  filtro con el nombre real (sin el .exe)
                if (ProcesosDelSistema.Contains(rawProcessName))
                {
                    return true; // En lugar de 'continue', se usa 'return true' para saltar al siguiente en EnumWindows
                }

                // Si superó el filtro, le agregamos el .exe para guardar en BD
                string finalProcessName = rawProcessName + ".exe";

                ventanas.Add(new VentanaItem
                {
                    Pid = (int)pid,
                    ProcessName = finalProcessName,
                    Titulo = titulo
                });

                return true;
            }, IntPtr.Zero);

            return ventanas;
        }
    }
}