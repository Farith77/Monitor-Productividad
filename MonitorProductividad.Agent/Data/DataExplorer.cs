using System;
using System.IO;
using System.Text;

namespace MonitorProductividad.Agent.Data
{
    public class DataExporter
    {
        private const string FileName = "log_productividad.csv";

        // Añadimos el parámetro ejecutableName
        public static void ExportToCsv(string ventanaActiva, string ejecutableName, double cpuVentanaActiva, int cantidadApps, long memoriaTotal, double segundosInactivo)
        {
            bool existeArchivo = File.Exists(FileName);
            try
            {
                using (StreamWriter sw = new StreamWriter(FileName, true, Encoding.UTF8))
                {
                    if (!existeArchivo)
                    {
                        // Nueva columna: Ejecutable
                        sw.WriteLine("Timestamp,Ejecutable,Ventana_Activa,CPU_App_Activa_Porcentaje,Cantidad_Apps_Fondo,RAM_Total_MB,Segundos_Inactivo");
                    }

                    string ventanaLimpia = ventanaActiva.Replace(",", " ");
                    string cpuFormateado = cpuVentanaActiva.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string idleFormateado = segundosInactivo.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

                    // Insertamos el ejecutable en la línea
                    string linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{ejecutableName},{ventanaLimpia},{cpuFormateado},{cantidadApps},{memoriaTotal},{idleFormateado}";
                    sw.WriteLine(linea);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CSV: {ex.Message}");
            }
        }
    }
}