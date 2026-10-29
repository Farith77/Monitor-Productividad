import { useState, useEffect, useCallback } from 'react';
import { PieChart, Shuffle, Clock, MonitorSmartphone } from 'lucide-react';
import Card from '../components/dashboard/Card';
import DeviceTable from '../components/dashboard/DeviceTable';
import AppUsageChart from '../components/dashboard/AppUsageChart';
import ContextSwitchChart from '../components/dashboard/ContextSwitchChart';
import { dashboardService } from '../services/dashboardService';

const formatMinutes = (totalMinutes) => {
  if (!totalMinutes || totalMinutes === 0) return "0m";
  const hours = Math.floor(totalMinutes / 60);
  const minutes = Math.round(totalMinutes % 60);
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
};

export default function DashboardPage() {
  const [deviceList, setDeviceList] = useState([]);
  const [globalMetrics, setGlobalMetrics] = useState({
    avgActivity: 0, totalActiveMins: 0, totalIdleMins: 0, activeCount: 0, totalCount: 0,
    execData: [], windowData: [], switchTrend: [], totalSwitches: 0,
    avgBackgroundWindows: 0, maxFocusBlock: 0
  });
  const [isLoading, setIsLoading] = useState(true);

  // Fecha calendario en hora de Perú (toISOString usa UTC y cambia de día a las 19:00)
  const today = new Date().toLocaleDateString('en-CA', { timeZone: 'America/Lima' });

  const fetchData = useCallback(async (showLoader = true) => {
    if (showLoader) setIsLoading(true);
    try {
      const devs = await dashboardService.getDispositivos();
      
      // Consultamos concurrentemente las métricas pulidas del backend para cada PC
      const statsPromises = devs.map(async (d) => {
        const resumen = await dashboardService.getResumen(d.macAddress, today).catch(() => null);
        const apps = await dashboardService.getDistribucion(d.macAddress, today).catch(() => []);
        const windows = await dashboardService.getDistribucionVentanas(d.macAddress, today).catch(() => []);
        const switches = await dashboardService.getTendenciaCambios(d.macAddress, today).catch(() => []);
        return { mac: d.macAddress, user: d.rolAsignado || 'Por asignar', resumen, apps, windows, switches };
      });
      const stats = await Promise.all(statsPromises);

      let sumActiveMins = 0;
      let sumIdleMins = 0;
      let activeDevicesCount = 0;
      let globalSwitches = 0;
      let maxFocus = 0;
      let sumBgWindows = 0;
      
      const allApps = {};
      const allWindows = {};
      const hourlySwitches = {};

      const tableData = stats.map(s => {
        if (s.resumen) {
          if (s.resumen.totalActiveMinutes > 0) {
            activeDevicesCount++;
            sumActiveMins += s.resumen.totalActiveMinutes;
            sumIdleMins += s.resumen.totalIdleMinutes;
          }
          globalSwitches += s.resumen.contextSwitches;
          sumBgWindows += s.resumen.averageBackgroundWindows;
          if (s.resumen.longestFocusBlockMinutes > maxFocus) maxFocus = s.resumen.longestFocusBlockMinutes;
        }

        // Agregar donas globalmente sumando minutos
        s.apps.forEach(a => allApps[a.processName] = (allApps[a.processName] || 0) + a.timeSpentMinutes);
        s.windows.forEach(w => allWindows[w.processName] = (allWindows[w.processName] || 0) + w.timeSpentMinutes);
        
        // Agregar gráfico de área (tendencias)
        s.switches.forEach(sw => hourlySwitches[sw.time] = (hourlySwitches[sw.time] || 0) + sw.switches);

        return {
          mac: s.mac, user: s.user,
          contextSwitches: s.resumen?.contextSwitches || 0,
          activity: formatMinutes(s.resumen?.totalActiveMinutes || 0),
          status: (s.resumen?.totalActiveMinutes > 0) ? 'Activo' : 'Inactivo'
        };
      });

      // Calcular porcentajes para las donas basándonos en minutos acumulados
      const formatPie = (dict) => {
        const total = Object.values(dict).reduce((a,b) => a+b, 0);
        return Object.entries(dict)
          .map(([name, mins]) => ({
            name: name.length > 22 ? name.substring(0, 22) + '...' : name,
            percentage: total > 0 ? Math.round((mins / total) * 100) : 0
          }))
          .sort((a, b) => b.percentage - a.percentage).slice(0, 5);
      };

      const switchTrendData = Object.keys(hourlySwitches).sort().map(hour => ({
        time: hour, switches: hourlySwitches[hour]
      }));

      setDeviceList(tableData);
      setGlobalMetrics({
        avgActivity: activeDevicesCount > 0 ? sumActiveMins / activeDevicesCount : 0,
        totalActiveMins: sumActiveMins,
        totalIdleMins: sumIdleMins,
        activeCount: activeDevicesCount,
        totalCount: devs.length,
        execData: formatPie(allApps),
        windowData: formatPie(allWindows),
        switchTrend: switchTrendData,
        totalSwitches: globalSwitches,
        avgBackgroundWindows: activeDevicesCount > 0 ? (sumBgWindows / activeDevicesCount).toFixed(1) : 0,
        maxFocusBlock: maxFocus
      });

    } catch (error) {
      console.error("Fallo al cargar datos globales", error);
    } finally {
      if (showLoader) setIsLoading(false);
    }
  }, [today]);

  useEffect(() => {
    fetchData(true);
    const intervalId = setInterval(() => fetchData(false), 10000);
    return () => clearInterval(intervalId);
  }, [fetchData]);

  // Cálculos para la barra de Actividad vs Inactividad
  const totalTime = globalMetrics.totalActiveMins + globalMetrics.totalIdleMins;
  const activePercentage = totalTime > 0 ? (globalMetrics.totalActiveMins / totalTime) * 100 : 0;
  const idlePercentage = totalTime > 0 ? (globalMetrics.totalIdleMins / totalTime) * 100 : 0;

  return (
    <div className="min-h-screen bg-slate-50 p-8 font-sans text-slate-800">
      <div className="max-w-6xl mx-auto space-y-6">
        
        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-2xl font-normal text-slate-900">Panel de productividad</h1>
            <p className="text-sm text-slate-500 mt-0.5">Fecha: {today}</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <Card title="Distribución por ejecutable" icon={PieChart}>
            {globalMetrics.execData.length > 0 ? (
               <AppUsageChart data={globalMetrics.execData} centerText={formatMinutes(globalMetrics.avgActivity)} />
            ) : <div className="h-40 flex items-center justify-center text-slate-400 text-sm">Sin datos suficientes</div>}
          </Card>
          
          <Card title="Distribución por ventanas (Top Tareas)" icon={PieChart}>
             {globalMetrics.windowData.length > 0 ? (
               <AppUsageChart data={globalMetrics.windowData} />
             ) : <div className="h-40 flex items-center justify-center text-slate-400 text-sm">Sin datos suficientes</div>}
          </Card>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="md:col-span-2">
            <Card title="Curva de Fatiga y Cambio de Contexto" icon={Shuffle}>
              <div className="grid grid-cols-3 gap-4 mb-2">
                <div>
                  <p className="text-sm text-slate-600">Cambios de<br/>contexto</p>
                  <p className="text-3xl font-medium mt-1">{isLoading ? "..." : globalMetrics.totalSwitches}</p>
                </div>
                {/* MÉTRICAS RESTAURADAS DE LOS MOCKUPS */}
                <div>
                  <p className="text-sm text-slate-600">Apps Concurrentes<br/>promedio</p>
                  <p className="text-3xl font-medium mt-1">{isLoading ? "..." : globalMetrics.avgBackgroundWindows}</p>
                </div>
                <div>
                  <p className="text-sm text-slate-600">Bloque de foco<br/>más largo (equipo)</p>
                  <p className="text-3xl font-medium mt-1">{isLoading ? "..." : formatMinutes(globalMetrics.maxFocusBlock)}</p>
                </div>
              </div>
              <div>
                <div className="flex justify-between items-end mb-1">
                  <p className="text-xs text-slate-500 font-medium uppercase tracking-wider">Volumen de interrupciones por hora</p>
                </div>
                {globalMetrics.switchTrend.length > 0 ? (
                   <ContextSwitchChart data={globalMetrics.switchTrend} />
                ) : <div className="h-40 w-full bg-slate-100 rounded animate-pulse mt-4"></div>}
              </div>
            </Card>
          </div>

          <div className="md:col-span-1">
            <Card title="Actividad promedio" icon={Clock}>
              <div className="flex flex-col items-center justify-center h-full pt-2">
                <span className="text-5xl font-semibold tracking-tight text-black">
                  {isLoading ? "..." : formatMinutes(globalMetrics.avgActivity)}
                </span>
                <span className="text-sm text-slate-700 mt-2 font-medium">
                  dispositivos activos: {globalMetrics.activeCount}/{globalMetrics.totalCount}
                </span>

                {/* NUEVA BARRA: Proporción de Actividad vs Inactividad */}
                <div className="w-full mt-6">
                  <div className="flex justify-between text-xs text-slate-500 mb-1 font-medium">
                    <span>Activo ({Math.round(activePercentage)}%)</span>
                    <span>Inactivo ({Math.round(idlePercentage)}%)</span>
                  </div>
                  <div className="w-full h-2 flex rounded-full overflow-hidden bg-slate-100">
                    <div style={{ width: `${activePercentage}%` }} className="bg-emerald-500 transition-all duration-500" title="Activo"></div>
                    <div style={{ width: `${idlePercentage}%` }} className="bg-slate-300 transition-all duration-500" title="Inactivo"></div>
                  </div>
                </div>

              </div>
            </Card>
          </div>
        </div>

        <Card title="Dispositivos" icon={MonitorSmartphone} rightAction="Identificación por MAC">
           <DeviceTable devices={deviceList} />
        </Card>

      </div>
    </div>
  );
}