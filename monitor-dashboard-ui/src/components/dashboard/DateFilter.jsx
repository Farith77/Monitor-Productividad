import { useState, useRef, useEffect } from 'react';
import { ChevronDown, Check } from 'lucide-react';
import { getTodayLima, getYesterdayLima, formatFechaCorta } from '../../utils/dateUtils';

export default function DateFilter({ value, onChange }) {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);
  const today = getTodayLima();
  const yesterday = getYesterdayLima();

  useEffect(() => {
    if (!open) return;
    const handleClick = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    const handleKey = (e) => { if (e.key === 'Escape') setOpen(false); };
    document.addEventListener('mousedown', handleClick);
    document.addEventListener('keydown', handleKey);
    return () => {
      document.removeEventListener('mousedown', handleClick);
      document.removeEventListener('keydown', handleKey);
    };
  }, [open]);

  const select = (fecha) => { onChange(fecha); setOpen(false); };

  const Option = ({ fecha, label }) => (
    <button
      type="button"
      onClick={() => select(fecha)}
      className="w-full flex items-center justify-between px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-100 rounded"
    >
      {label}
      {value === fecha && <Check className="w-4 h-4 text-blue-600" />}
    </button>
  );

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        className="flex items-center justify-between gap-6 min-w-[100px] px-3 py-1 text-sm text-slate-800 bg-white border border-slate-300 rounded hover:border-slate-400 transition-colors"
      >
        <span>{formatFechaCorta(value)}</span>
        <ChevronDown className={`w-4 h-4 text-slate-600 transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>

      {open && (
        <div className="absolute right-0 mt-1 w-52 bg-white border border-slate-200 rounded-md shadow-lg p-1 z-20">
          <Option fecha={today} label="Hoy" />
          <Option fecha={yesterday} label="Ayer" />
          <div className="border-t border-slate-100 my-1" />
          <label className="block px-3 pt-1 pb-0.5 text-xs text-slate-500">Elegir fecha…</label>
          <input
            type="date"
            max={today}
            value={value}
            onChange={(e) => e.target.value && select(e.target.value)}
            className="w-full px-2 py-1 mb-1 text-sm border border-slate-300 rounded text-slate-700"
          />
        </div>
      )}
    </div>
  );
}
