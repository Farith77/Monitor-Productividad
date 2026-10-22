import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Activity, Layers, List, PieChart } from 'lucide-react';
import Card from '../components/dashboard/Card';
import ContextTimeline from '../components/dashboard/ContextTimeline';
import AppUsageChart from '../components/dashboard/AppUsageChart';
import { dashboardService } from '../services/dashboardService';

const formatMinutes = (totalMinutes) => {
  if (!totalMinutes || totalMinutes === 0) return "0m";
  if (totalMinutes > 0 && totalMinutes < 1) return "< 1m";
  
  const hours = Math.floor(totalMinutes / 60);
  const minutes = Math.round(totalMinutes % 60);
  
  if (minutes === 60) return `${hours + 1}h 0m`;
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
};

const APP_COLORS = {
  'Code.exe': '#517de3', 'brave.exe': '#73a580', 'chrome.exe': '#73a580',
  'idle.exe': '#e2e8f0', 'explorer.exe': '#a57a4a', 'SystemSettings.exe': '#64748b'
};

const getProcessColor = (processName) => {
  if (!processName) return '#a6a6a6';
  if (APP_COLORS[processName]) return APP_COLORS[processName];
  let hash = 0;
  for (let i = 0; i < processName.length; i++) hash = processName.charCodeAt(i) + ((hash << 5) - hash);
  const c = (hash & 0x00FFFFFF).toString(16).toUpperCase();
  return '#' + '00000'.substring(0, 6 - c.length) + c;
};

export default function DeviceDetailPage() {
  const { mac } = useParams();
  const [resumen, setResumen] = useState(null);
  const [registros, setRegistros] = useState([]);
  const [lineaTiempo, setLineaTiempo] = useState([]);
  const [distribucion, setDistribucion] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  const today = new Date().toISOString().split('T')[0];

  const fetchData = useCallback(async (showLoader = true) => {
    if (showLoader) setIsLoading(true);
    try {
      const [resumenData, registrosData, lineaData, distribucionData] = await Promise.all([
        dashboardService.getResumen(mac, today),
        dashboardService.getRegistros(mac, today),
        dashboardService.getLineaTiempo(mac, today),
        dashboardService.getDistribucion(mac, today)
      ]);
      setResumen(resumenData);
      setRegistros(registrosData);
      setDistribucion(distribucionData);
      
      const formattedTimeline = lineaData.map(seg => ({
        label: seg.label,
        percentage: seg.percentage,
        color: getProcessColor(seg.processName)
      }));
      setLineaTiempo(formattedTimeline);
    } catch (error) {
      console.error("Error al cargar datos del dispositivo", error);
    } finally {
      if (showLoader) setIsLoading(false);
    }
  }, [mac, today]);

  useEffect(() => {
    if (mac) {
      fetchData(true);
      const intervalId = setInterval(() => fetchData(false), 10000);
      return () => clearInterval(intervalId);
    }
  }, [mac, fetchData]);

  // =========================================================================
  // NUEVO: Transformar el historial en datos porcentuales para la 2da Dona
  // =========================================================================
  const bgWindowCounts = {};
  let totalBgWindowsCount = 0;

  registros.forEach(r => {
    // Solo medimos la concurrencia cuando el usuario realmente está trabajando
    if (r.process !== 'idle.exe' && r.backgroundWindows) {
      r.backgroundWindows.forEach(app => {
        bgWindowCounts[app] = (bgWindowCounts[app] || 0) + 1;
        totalBgWindowsCount++;
      });
    }
  });

  const backgroundChartData = Object.entries(bgWindowCounts)
    .map(([name, count]) => ({
      name: name,
      percentage: totalBgWindowsCount > 0 ? Math.round((count / totalBgWindowsCount) * 100) : 0
    }))
    .sort((a, b) => b.percentage - a.percentage)
    .slice(0, 5); // Tomamos el Top 5 para que la dona no se sature

  const startTime = registros.length > 0 ? registros[registros.length - 1].time.substring(0, 5) : "--:--";
  const endTime = registros.length > 0 ? registros[0].time.substring(0, 5) : "--:--";

  return (
    <div className="min-h-screen bg-slate-50 p-8 font-sans text-slate-800">
      <div className="max-w-6xl mx-auto space-y-6">
        
        <div className="flex flex-col gap-4 mb-2">
          <Link to="/" className="inline-flex items-center text-sm text-slate-500 hover:text-blue-600 transition-colors w-fit">
            <ArrowLeft className="w-4 h-4 mr-1" />
            Volver al panel general
          </Link>
          <div className="flex justify-between items-start">
            <div className="flex flex-col gap-1">
              <div className="flex items-baseline gap-3">
                <h1 className="text-2xl font-medium text-slate-900">Dispositivo</h1>
                <span className="text-lg text-slate-700 font-mono tracking-tight">{mac}</span>
              </div>
              <p className="text-sm text-slate-500">Fecha consultada: {today}</p>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 pb-2">
          <div className="flex flex-col">
            <span className="text-sm text-slate-700 mb-1">Actividad</span>
            <span className="text-3xl font-medium text-slate-900">{isLoading ? "..." : formatMinutes(resumen?.totalActiveMinutes)}</span>
          </div>
          <div className="flex flex-col">
            <span className="text-sm text-slate-700 mb-1">Tiempo inactivo</span>
            <span className="text-3xl font-medium text-slate-900">{isLoading ? "..." : formatMinutes(resumen?.totalIdleMinutes)}</span>
          </div>
          <div className="flex flex-col">
            <span className="text-sm text-slate-700 mb-1">CPU Promedio</span>
            <span className="text-3xl font-medium text-slate-900">{isLoading ? "..." : `${resumen?.averageCpu ?? 0}%`}</span>
          </div>
          <div className="flex flex-col">
            <span className="text-sm text-slate-700 mb-1">RAM Promedio</span>
            <span className="text-3xl font-medium text-slate-900">{isLoading ? "..." : `${resumen?.averageRamGb ?? 0} GB`}</span>
          </div>
        </div>

        <Card title="Linea de tiempo" icon={Activity}>
          <div className="mt-4">
            {lineaTiempo.length > 0 ? (
              <>
                <ContextTimeline segments={lineaTiempo} />
                <div className="flex justify-between text-xs text-slate-400 mt-2 font-mono font-medium">
                  <span>{startTime}</span>
                  <span>{endTime}</span>
                </div>
              </>
            ) : (
              <div className="h-6 w-full bg-slate-100 rounded animate-pulse"></div>
            )}
          </div>
        </Card>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Dona 1: Ejecutables en Primer Plano */}
          <Card title="Distribución por ejecutable" icon={PieChart}>
            {distribucion.length > 0 ? (
               <AppUsageChart data={distribucion} centerText={formatMinutes(resumen?.totalActiveMinutes)} />
            ) : (
               <div className="h-48 flex items-center justify-center text-slate-400 text-sm">Sin datos suficientes</div>
            )}
          </Card>

          {/* Dona 2: Ventanas en Segundo Plano (AHORA COMO GRÁFICA) */}
          <Card title="Proporción de ventanas en segundo plano" icon={Layers}>
            {backgroundChartData.length > 0 ? (
               <AppUsageChart data={backgroundChartData} centerText="Top 5 Apps" />
            ) : (
               <div className="h-48 flex items-center justify-center text-slate-400 text-sm">No hay procesos recientes</div>
            )}
          </Card>
        </div>

        <Card title="Registro reciente" icon={List}>
          <div className="overflow-x-auto w-full mt-2">
            <table className="w-full text-sm text-left">
              <thead className="text-slate-500 border-b border-slate-200">
                <tr>
                  <th className="pb-3 font-medium">Hora</th>
                  <th className="pb-3 font-medium">Proceso</th>
                  <th className="pb-3 font-medium">Título de ventana</th>
                  <th className="pb-3 font-medium">CPU</th>
                  <th className="pb-3 font-medium">RAM</th>
                  <th className="pb-3 font-medium">Idle</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {registros.map((log, i) => (
                  <tr key={i} className="text-slate-800">
                    <td className="py-3 font-mono text-xs text-slate-400 font-medium">{log.time}</td>
                    <td className="py-3 font-semibold text-slate-800">{log.process}</td>
                    <td className="py-3 text-slate-600 font-medium">{log.window}</td>
                    <td className="py-3 font-medium">{log.cpu}</td>
                    <td className="py-3 font-medium">{log.ram}</td>
                    <td className="py-3 font-medium">{log.idle}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      </div>
    </div>
  );
}