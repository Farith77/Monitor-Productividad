using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace MonitorProductividad.Agent.Trackers
{
    public class CpuTracker
    {
        private static Dictionary<int, (TimeSpan, DateTime)> memoriaProcesos = new Dictionary<int, (TimeSpan, DateTime)>();

        public static double ObtenerPorcentajeCpu(Process? proceso)
        {
            // BLINDAJE 2: Si el proceso es nulo debido a un error previo, devolvemos 0% inmediatamente
            if (proceso == null) return 0.0;

            try
            {
                // Comprobamos si el proceso ya terminó antes de pedirle datos
                if (proceso.HasExited) return 0.0;

                int pid = proceso.Id;
                TimeSpan tiempoCpuActual = proceso.TotalProcessorTime; // <- Punto crítico de crasheo
                DateTime horaActual = DateTime.UtcNow;

                if (!memoriaProcesos.ContainsKey(pid))
                {
                    memoriaProcesos[pid] = (tiempoCpuActual, horaActual);
                    return 0.0;
                }

                var estadoAnterior = memoriaProcesos[pid];
                TimeSpan tiempoCpuAnterior = estadoAnterior.Item1;
                DateTime horaAnterior = estadoAnterior.Item2;

                double milisegundosPasados = (horaActual - horaAnterior).TotalMilliseconds;
                double milisegundosDeCpuUsados = (tiempoCpuActual - tiempoCpuAnterior).TotalMilliseconds;

                memoriaProcesos[pid] = (tiempoCpuActual, horaActual);

                if (milisegundosPasados > 0)
                {
                    double usoCpu = (milisegundosDeCpuUsados / (Environment.ProcessorCount * milisegundosPasados)) * 100;
                    return Math.Max(0, Math.Min(100, Math.Round(usoCpu, 2)));
                }
            }
            catch (InvalidOperationException)
            {
                // El proceso terminó justo cuando calculábamos su tiempo
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Acceso denegado: El proceso requiere permisos de Administrador/SYSTEM para ser leído
            }
            catch
            {
                // Cualquier otro error imprevisto es absorbido para que el agente siga vivo
            }
            
            return 0.0;
        }
    }
}