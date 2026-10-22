import { Link } from 'react-router-dom';

export default function DeviceTable({ devices }) {
  if (!devices || devices.length === 0) {
    return <div className="p-8 text-center text-sm text-slate-500">No hay dispositivos registrados aún.</div>;
  }

  return (
    <div className="overflow-x-auto w-full mt-4">
      <table className="w-full text-sm text-left">
        <thead className="text-slate-600 border-b border-slate-200">
          <tr>
            <th className="pb-3 font-medium font-sans">Dispositivo</th>
            <th className="pb-3 font-medium font-sans">Asignado a</th>
            <th className="pb-3 font-medium font-sans">Cambios de contexto</th>
            <th className="pb-3 font-medium font-sans">Actividad</th>
            <th className="pb-3 font-medium font-sans">Estado</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {devices.map((dev, i) => (
            <tr key={i} className="text-slate-800">
              <td className="py-4 font-mono text-xs text-slate-600">
                <Link to={`/device/${dev.mac}`} className="text-blue-600 hover:underline">
                  {dev.mac}
                </Link>
              </td>
              <td className={`py-4 ${dev.user === 'Por asignar' ? 'text-slate-400' : 'text-slate-900'}`}>
                {dev.user}
              </td>
              <td className="py-4 font-medium">{dev.contextSwitches}</td>
              <td className="py-4">{dev.activity}</td>
              <td className={`py-4 ${dev.status === 'Activo' ? 'text-emerald-600 font-medium' : 'text-slate-500'}`}>
                {dev.status}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}