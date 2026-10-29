import { useSearchParams } from 'react-router-dom';
import { getTodayLima } from '../utils/dateUtils';

const ISO_REGEX = /^\d{4}-\d{2}-\d{2}$/;

// Fecha seleccionada persistida en la URL (?fecha=YYYY-MM-DD) para compartirla entre páginas
export function useSelectedDate() {
  const [searchParams, setSearchParams] = useSearchParams();
  const today = getTodayLima();
  const param = searchParams.get('fecha');
  const selectedDate = param && ISO_REGEX.test(param) && param <= today ? param : today;

  const setSelectedDate = (fecha) => {
    const next = new URLSearchParams(searchParams);
    if (!fecha || fecha === today) next.delete('fecha');
    else next.set('fecha', fecha);
    setSearchParams(next, { replace: true });
  };

  return { selectedDate, setSelectedDate, isToday: selectedDate === today, search: searchParams.toString() };
}
