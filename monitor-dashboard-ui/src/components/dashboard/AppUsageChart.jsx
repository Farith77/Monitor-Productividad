import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';

const getProcessColor = (name) => {
  // 1. BLINDAJE: Si el nombre viene nulo o indefinido, retornar gris sin procesar el hash
  if (!name) return '#a6a6a6';
  
  const APP_COLORS = { 
    'Code.exe': '#517de3', 'brave.exe': '#73a580', 'chrome.exe': '#73a580', 
    'explorer.exe': '#a57a4a', 'SystemSettings.exe': '#64748b', 'idle.exe': '#e2e8f0' 
  };
  if (APP_COLORS[name]) return APP_COLORS[name];
  
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
  const c = (hash & 0x00FFFFFF).toString(16).toUpperCase();
  return '#' + '00000'.substring(0, 6 - c.length) + c;
};

export default function AppUsageChart({ data, centerText }) {
  if (!data || data.length === 0) {
    return <div className="h-48 flex items-center justify-center text-slate-400 text-sm">Sin datos suficientes</div>;
  }

  return (
    <div className="h-48 w-full flex items-center justify-between">
      <div className="w-1/2 h-full relative">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius="65%"
              outerRadius="90%"
              paddingAngle={2}
              dataKey="percentage"
              stroke="none"
            >
              {data.map((entry, index) => {
                // 2. SOPORTE MULTI-FORMATO: Acepta 'name' (React), 'processName' o 'ProcessName' (C#)
                const itemName = entry.name || entry.processName || entry.ProcessName || 'Desconocido';
                return <Cell key={`cell-${index}`} fill={getProcessColor(itemName)} />;
              })}
            </Pie>
            <Tooltip 
              formatter={(value) => [`${value}%`, 'Proporción']}
              contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)', fontSize: '12px' }}
            />
          </PieChart>
        </ResponsiveContainer>
        {centerText && (
          <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
            <span className="text-sm font-medium text-slate-700">{centerText}</span>
          </div>
        )}
      </div>
      
      <div className="w-1/2 flex flex-col justify-center gap-2 pl-4 border-l border-slate-100">
        {data.map((item, i) => {
          const itemName = item.name || item.processName || item.ProcessName || 'Desconocido';
          return (
            <div key={i} className="flex items-center justify-between text-xs">
              <div className="flex items-center gap-2 overflow-hidden">
                <span 
                  className="w-2.5 h-2.5 rounded-full flex-shrink-0" 
                  style={{ backgroundColor: getProcessColor(itemName) }}
                ></span>
                <span className="text-slate-600 truncate font-medium" title={itemName}>
                  {itemName}
                </span>
              </div>
              <span className="text-slate-500 ml-2 flex-shrink-0">{item.percentage}%</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}