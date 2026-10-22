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
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet || 
                     nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && 
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    string hex = nic.GetPhysicalAddress().ToString();
                    if (string.IsNullOrEmpty(hex)) continue;
                    
                    return string.Join(":", Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)));
                }
            }
            return "00:00:00:00:00:00";
        }
    }

    public class SyncPayload
    {
        public string mac_address { get; set; } = "";
        public string device_timestamp { get; set; } = "";
        public List<TelemetryItemDto> telemetry_data { get; set; } = new List<TelemetryItemDto>();
    }
}