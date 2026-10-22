export default function Card({ title, icon: Icon, rightAction, children }) {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm flex flex-col h-full">
      {/* Cabecera de la tarjeta */}
      {(title || Icon) && (
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            {Icon && <Icon className="w-5 h-5 text-slate-700" />}
            <h2 className="text-base font-semibold text-slate-900">{title}</h2>
          </div>
          {rightAction && <div className="text-xs text-slate-500">{rightAction}</div>}
        </div>
      )}
      
      {/* Contenido dinámico */}
      <div className="flex-1">
        {children}
      </div>
    </div>
  );
}