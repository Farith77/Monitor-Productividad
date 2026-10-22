using System;
using System.Runtime.InteropServices;

namespace MonitorProductividad.Agent.Trackers
{
    public class IdleTracker
    {
        // Estructura requerida por la API de Windows para almacenar el tiempo del último input
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Devuelve el tiempo en segundos que el usuario ha estado sin tocar el mouse o teclado.
        /// </summary>
        public static double ObtenerTiempoInactivoSegundos()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);
            lii.dwTime = 0;

            // Obtenemos el milisegundo del sistema actual
            uint sistemaTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lii))
            {
                // Calculamos la diferencia
                uint diferenciaTicks = sistemaTicks - lii.dwTime;

                // Convertimos de milisegundos a segundos
                return diferenciaTicks / 1000.0;
            }

            return 0.0;
        }
    }
}