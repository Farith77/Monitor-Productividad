const TZ = 'America/Lima';

// YYYY-MM-DD en hora de Perú (toISOString usa UTC y cambia de día a las 19:00)
export const getTodayLima = () => new Date().toLocaleDateString('en-CA', { timeZone: TZ });

// Suma días a una fecha 'YYYY-MM-DD' sin depender de la zona horaria del navegador
export const addDays = (isoDate, days) => {
  const [y, m, d] = isoDate.split('-').map(Number);
  const dt = new Date(Date.UTC(y, m - 1, d + days));
  return dt.toISOString().split('T')[0];
};

export const getYesterdayLima = () => addDays(getTodayLima(), -1);

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const DIAS = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];

// "Hoy, 24 sep 2026" | "Ayer, 23 sep 2026" | "mié, 16 sep 2026"
export const formatFechaLarga = (isoDate) => {
  const [y, m, d] = isoDate.split('-').map(Number);
  const dow = new Date(Date.UTC(y, m - 1, d)).getUTCDay();
  const base = `${d} ${MESES[m - 1]} ${y}`;
  if (isoDate === getTodayLima()) return `Hoy, ${base}`;
  if (isoDate === getYesterdayLima()) return `Ayer, ${base}`;
  return `${DIAS[dow]}, ${base}`;
};

// Etiqueta corta para el botón del filtro
export const formatFechaCorta = (isoDate) => {
  if (isoDate === getTodayLima()) return 'Hoy';
  if (isoDate === getYesterdayLima()) return 'Ayer';
  const [y, m, d] = isoDate.split('-');
  return `${d}/${m}/${y}`;
};
