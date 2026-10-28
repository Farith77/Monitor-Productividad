import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Activity, List, PieChart, AppWindow } from 'lucide-react';
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

// HELPER: Filtra los ejecutables principales al Top 7 y agrupa el resto en "Otros"
const procesarDistribucion = (dist) => {
  if (!dist || dist.length === 0) return [];
  if (dist.length <= 8) return dist;
  
  const top7 = dist.slice(0, 7);
  const otros = dist.slice(7).reduce((acc, curr) => {
    acc.percentage += curr.percentage;
    return acc;
  }, { processName: 'Otros', name: 'Otros', percentage: 0 });
  
  otros.percentage = Math.round(otros.percentage * 10) / 10;
  return [...top7, otros];
};

export default function DeviceDetailPage() {
  const { mac } = useParams();
  const [resumen, setResumen] = useState(null);
  const [registros, setRegistros] = useState([]);
  const [lineaTiempo, setLineaTiempo] = useState([]);
  const [distribucion, setDistribucion] = useState([]);
  const [bgStats, setBgStats] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  const today = new Date().toISOString().split('T')[0];

  const fetchData = useCallback(async (showLoader = true) => {
    if (showLoader) setIsLoading(true);
    try {
      const [resumenData, registrosData, lineaData, distribucionData, bgStatsData] = await Promise.all([
        dashboardService.getResumen(mac, today),
        dashboardService.getRegistros(mac, today),
        dashboardService.getLineaTiempo(mac, today),
        dashboardService.getDistribucion(mac, today),
        dashboardService.getBackgroundStats(mac, today) // Trae todo el historial de fondo procesado
      ]);
      setResumen(resumenData);
      setRegistros(registrosData);
      setDistribucion(procesarDistribucion(distribucionData)); 
      setBgStats(bgStatsData);
      
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
                  {/* Las horas ahora reflejan todo el día */}
                  <span>{resumen?.firstRecordTime || "--:--"}</span>
                  <span>{resumen?.lastRecordTime || "--:--"}</span>
                </div>
              </>
            ) : (
              <div className="h-6 w-full bg-slate-100 rounded animate-pulse"></div>
            )}
          </div>
        </Card>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <Card title="Distribución por ejecutable" icon={PieChart}>
            {distribucion.length > 0 ? (
               <AppUsageChart data={distribucion} centerText={formatMinutes(resumen?.totalActiveMinutes)} />
            ) : (
               <div className="h-48 flex items-center justify-center text-slate-400 text-sm">Sin datos suficientes</div>
            )}
          </Card>

          <Card title="Proporción de ventanas en segundo plano" icon={PieChart}>
            {bgStats?.chart?.length > 0 ? (
               <AppUsageChart data={bgStats.chart} centerText="Top 7 Ventanas" />
            ) : (
               <div className="h-48 flex items-center justify-center text-slate-400 text-sm">No hay procesos recientes</div>
            )}
          </Card>
        </div>

        {/* NUEVA TARJETA: Histórico de aplicaciones de fondo con estado en vivo */}
        <Card title="Aplicaciones en segundo plano" icon={AppWindow}>
          <div className="mt-2">
            {!bgStats?.tags || bgStats.tags.length === 0 ? (
              <p className="text-sm text-slate-500">No hay procesos en segundo plano registrados hoy.</p>
            ) : (
              <div className="flex flex-wrap gap-3 mt-3">
                {bgStats.tags.map((app, index) => (
                  <div key={index} className="flex items-center gap-2 px-3 py-1.5 bg-red-50/50 text-slate-800 text-sm font-medium rounded-md border border-red-100">
                    <span>{app.name}</span>
                    <span className={`w-2 h-2 rounded-full ${app.isOpen ? 'bg-emerald-500 shadow-sm shadow-emerald-400/50' : 'bg-slate-300'}`}></span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>

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