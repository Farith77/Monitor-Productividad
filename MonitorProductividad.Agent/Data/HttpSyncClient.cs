using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonitorProductividad.Agent.Data
{
    public class HttpSyncClient
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Ejecuta el empaquetado asíncrono y la transmisión del lote de telemetría local por HTTPS.
        /// </summary>
        public static async Task SincronizarDatosAsync(string apiUrl)
        {
            try
            {
                var registrosPendientes = LocalDbContext.ObtenerTelemetriaNoSincronizada();
                if (registrosPendientes.Count == 0) return;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [RED] Capa de red activa: Transmitiendo lote de {registrosPendientes.Count} registros...");

                // Construcción de la estructura JSON idéntica a la sección 3.5 de la tesis
                var payload = new SyncPayload
                {
                    mac_address = ObtenerMacAddress(),
                    device_timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    telemetry_data = registrosPendientes
                };

                string jsonBody = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Envío asíncrono hacia el backend centralizado
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [RED] Servidor respondió HTTP {(int)response.StatusCode}. Confirmando limpieza de cola local...");
                    
                    var idsSincronizados = registrosPendientes.Select(r => r.Id).ToList();
                    LocalDbContext.MarcarComoSincronizados(idsSincronizados);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ALERTA RED] El servidor central rechazó el lote (HTTP {(int)response.StatusCode}). Resguardando datos en SQLite.");
                }
            }
            catch (HttpRequestException)
            {
                // CONTROL DE ERRORES TRANSIENTES (Tolerancia a fallos por diseño)
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ALERTA RED] Servidor central inaccesible. Modo Offline activo, datos resguardados localmente.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR CAPA RED] Fallo no controlado: {ex.Message}");
            }
            finally
            {
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Obtiene la dirección MAC física de la tarjeta de red activa para la gobernanza basada en hardware.
        /// </summary>
        private static string ObtenerMacAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                // 1. Ignorar interfaces apagadas o de loopback local
                if (ni.OperationalStatus != OperationalStatus.Up || 
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                string descripcion = ni.Description.ToLower();
                string nombre = ni.Name.ToLower();

                // 2. Filtro estricto de hipervisores y adaptadores virtuales
                if (descripcion.Contains("hyper-v") || 
                    descripcion.Contains("virtual") || 
                    descripcion.Contains("wsl") || 
                    descripcion.Contains("vmware") ||
                    descripcion.Contains("pseudo") ||
                    nombre.Contains("veth"))
                {
                    continue;
                }

                // 3. Obtener los bytes y formatear con dos puntos (XX:XX:XX:XX:XX:XX)
                var bytes = ni.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length == 6)
                {
                    return string.Join(":", bytes.Select(b => b.ToString("X2")));
                }
            }

            return "00:00:00:00:00:00"; // Fallback en caso extremo
        }
    }

    public class SyncPayload
    {
        public string mac_address { get; set; } = "";
        public string device_timestamp { get; set; } = "";
        public List<TelemetryItemDto> telemetry_data { get; set; } = new List<TelemetryItemDto>();
    }
}