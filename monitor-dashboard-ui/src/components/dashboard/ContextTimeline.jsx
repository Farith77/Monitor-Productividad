// Elige ~N marcas de hora a lo largo de la barra usando el startTime real de cada segmento
const buildTicks = (segments, firstTime, lastTime, count = 6) => {
  const starts = [];
  let acc = 0;
  segments.forEach(s => { starts.push({ pos: acc, time: s.startTime }); acc += s.percentage; });

  const ticks = [{ pos: 0, time: firstTime || segments[0]?.startTime }];
  for (let i = 1; i < count - 1; i++) {
    const target = (i / (count - 1)) * 100;
    // Inicio de segmento más cercano a la posición objetivo
    const best = starts.reduce((a, b) => (Math.abs(b.pos - target) < Math.abs(a.pos - target) ? b : a), starts[0]);
    // Evitar marcas superpuestas o sin hora
    if (best?.time && ticks.every(t => Math.abs(t.pos - best.pos) > 8)) ticks.push(best);
  }
  if (lastTime) ticks.push({ pos: 100, time: lastTime });
  return ticks.filter(t => t.time);
};

export default function ContextTimeline({ segments, firstTime, lastTime }) {
  const hasTimes = segments.some(s => s.startTime);
  const ticks = hasTimes || firstTime ? buildTicks(segments, firstTime, lastTime) : [];

  return (
    <div className="w-full">
      <div className="w-full flex h-6 rounded overflow-hidden shadow-inner bg-slate-100">
        {segments.map((segment, index) => (
          <div
            key={index}
            className="h-full transition-all duration-300 hover:brightness-110"
            style={{ 
              width: `${segment.percentage}%`, 
              backgroundColor: segment.color 
            }}
            title={`${segment.startTime ? segment.startTime + ' · ' : ''}${segment.label}: ${segment.percentage}%`}
          ></div>
        ))}
      </div>

      {ticks.length > 0 && (
        <div className="relative w-full h-6 mt-1 text-xs text-slate-400 font-mono font-medium">
          {ticks.map((t, i) => {
            const isFirst = t.pos === 0;
            const isLast = t.pos === 100;
            const transform = isFirst ? 'none' : isLast ? 'translateX(-100%)' : 'translateX(-50%)';
            return (
              <div key={i} className="absolute top-0 flex flex-col items-center" style={{ left: `${t.pos}%`, transform }}>
                <span className={`w-px h-1.5 bg-slate-300 ${isFirst ? 'self-start' : isLast ? 'self-end' : ''}`}></span>
                <span>{t.time}</span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}