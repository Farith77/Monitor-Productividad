export default function ContextTimeline({ segments }) {
  return (
    <div className="w-full flex h-6 rounded overflow-hidden shadow-inner bg-slate-100">
      {segments.map((segment, index) => (
        <div
          key={index}
          className="h-full transition-all duration-300 hover:brightness-110"
          style={{ 
            width: `${segment.percentage}%`, 
            backgroundColor: segment.color 
          }}
          title={`${segment.label}: ${segment.percentage}%`}
        ></div>
      ))}
    </div>
  );
}