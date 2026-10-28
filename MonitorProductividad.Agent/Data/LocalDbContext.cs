using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MonitorProductividad.Agent.Data
{
    public class LocalDbContext
    {
        private static readonly string ConnectionString;

        // NUEVO: Constructor estático que redirige la base de datos a una ruta permitida por Windows
        static LocalDbContext()
        {
            // Apunta a C:\Users\<TuUsuario>\AppData\Local\MonitorTesis
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folderPath = Path.Combine(appData, "MonitorProductividad");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string dbPath = Path.Combine(folderPath, "telemetria_local.db");
            ConnectionString = $"Data Source={dbPath}";
        }

        /// <summary>
        /// Inicializa la base de datos creando la tabla y aplicando migraciones de columnas faltantes.
        /// </summary>
        public static void InicializarBaseDatos()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // 1. Crear tabla base si no existe
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Local_Telemetry_Log (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        ProcessName TEXT NOT NULL,
                        WindowTitle TEXT,
                        CpuUsage REAL NOT NULL,
                        RamUsageMB INTEGER NOT NULL,
                        IdleSeconds REAL NOT NULL,
                        BackgroundWindows TEXT,
                        IsSynced INTEGER DEFAULT 0
                    );";
                command.ExecuteNonQuery();

                // 2. Migración: Añadir la columna si la BD ya existía de versiones previas
                try
                {
                    var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = "ALTER TABLE Local_Telemetry_Log ADD COLUMN BackgroundWindows TEXT;";
                    alterCommand.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // La columna ya existe en la tabla; ignorar excepción
                }
            }
        }

        /// <summary>
        /// Inserta un nuevo registro de telemetría de forma segura usando parámetros SQL.
        /// </summary>
        public static void GuardarTelemetria(
            string ventanaActiva, 
            string ejecutableName, 
            double cpuUsage, 
            long memoriaTotal, 
            double segundosInactivo, 
            string backgroundWindowsJson)
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Local_Telemetry_Log 
                        (Timestamp, ProcessName, WindowTitle, CpuUsage, RamUsageMB, IdleSeconds, BackgroundWindows, IsSynced)
                        VALUES 
                        ($timestamp, $processName, $windowTitle, $cpuUsage, $ramUsageMB, $idleSeconds, $backgroundWindows, 0);";

                    command.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("$processName", ejecutableName ?? string.Empty);
                    command.Parameters.AddWithValue("$windowTitle", string.IsNullOrEmpty(ventanaActiva) ? DBNull.Value : (object)ventanaActiva);
                    command.Parameters.AddWithValue("$cpuUsage", cpuUsage);
                    command.Parameters.AddWithValue("$ramUsageMB", memoriaTotal);
                    command.Parameters.AddWithValue("$idleSeconds", segundosInactivo);
                    command.Parameters.AddWithValue("$backgroundWindows", string.IsNullOrEmpty(backgroundWindowsJson) ? "[]" : (object)backgroundWindowsJson);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR SQLITE] No se pudo escribir en la BD: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Recupera todos los registros de telemetría pendientes de sincronización.
        /// </summary>
        public static List<TelemetryItemDto> ObtenerTelemetriaNoSincronizada()
        {
            var lista = new List<TelemetryItemDto>();
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Timestamp, ProcessName, WindowTitle, CpuUsage, RamUsageMB, IdleSeconds, BackgroundWindows FROM Local_Telemetry_Log WHERE IsSynced = 0;";
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new TelemetryItemDto
                        {
                            Id = reader.GetInt32(0),
                            timestamp = reader.GetString(1),
                            process_name = reader.GetString(2),
                            window_title = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            cpu_percentage = reader.GetDouble(4),
                            ram_usage_mb = reader.GetInt32(5),
                            idle_seconds = reader.GetDouble(6),
                            background_windows = reader.IsDBNull(7) ? "[]" : reader.GetString(7)
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Actualiza masivamente el estado de sincronización mediante una transacción.
        /// </summary>
        public static void MarcarComoSincronizados(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // 1. Marcar el lote actual como sincronizado
                using (var transaction = connection.BeginTransaction())
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Local_Telemetry_Log SET IsSynced = 1 WHERE Id = $id;";
                    var idParam = command.Parameters.Add("$id", SqliteType.Integer);

                    foreach (var id in ids)
                    {
                        idParam.Value = id;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                
                // 2. Política de Retención (Purga de datos antiguos)
                try
                {
                    var purgeCommand = connection.CreateCommand();
                    purgeCommand.CommandText = "DELETE FROM Local_Telemetry_Log WHERE IsSynced = 1 AND datetime(Timestamp) <= datetime('now', '-3 days');";
                    int eliminados = purgeCommand.ExecuteNonQuery();

                    if (eliminados > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"[MANTENIMIENTO] Se purgaron {eliminados} registros antiguos de SQLite.");
                        Console.ResetColor();
                    }
                }
                catch
                {
                    // Ignoramos silenciosamente si la purga falla, no afecta el flujo de red principal
                }
            }
        }
    }

    public class TelemetryItemDto
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }
        public string timestamp { get; set; } = "";
        public string process_name { get; set; } = "";
        public string window_title { get; set; } = "";
        public double cpu_percentage { get; set; }
        public int ram_usage_mb { get; set; }
        public double idle_seconds { get; set; }
        public string background_windows { get; set; } = "[]";
    }
}