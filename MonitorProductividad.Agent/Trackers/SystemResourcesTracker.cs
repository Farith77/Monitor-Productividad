using System;
using System.Diagnostics;

namespace MonitorProductividad.Agent.Trackers
{
    public class SystemResourcesTracker
    {
        // Ahora el método devuelve un "Tuple" con la cantidad de apps y la RAM total
        public static (int cantidad, long ramTotal) ObtenerResumenRecursos()
        {
            Process[] procesos = Process.GetProcesses();
            int appsVisibles = 0;
            long memoriaAcumulada = 0;

            foreach (Process proceso in procesos)
            {
                if (proceso.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proceso.MainWindowTitle))
                {
                    appsVisibles++;
                    memoriaAcumulada += (proceso.WorkingSet64 / (1024 * 1024));
                }
            }
            return (appsVisibles, memoriaAcumulada);
        }
    }
}