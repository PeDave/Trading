import { useEffect, useState } from 'react';
import { tradingService } from '@/services/tradingService';

interface Position { symbol: string; side: string; size: number; entryPrice: number; markPrice: number; unrealizedPnl: number; leverage: number; }

export default function PositionList() {
  const [positions, setPositions] = useState<Position[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    tradingService.getPositions().then(setPositions).catch(() => setPositions([])).finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="card text-gray-400 text-sm">Loading positions…</div>;
  if (positions.length === 0) return <div className="card text-gray-500 text-sm text-center py-8">No open positions</div>;

  return (
    <div className="card p-0 overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-gray-800 text-gray-400">
          <tr>{['Symbol','Side','Size','Entry','Mark','PnL','Leverage','Action'].map((h) => <th key={h} className="px-4 py-2 text-left font-medium">{h}</th>)}</tr>
        </thead>
        <tbody>
          {positions.map((p) => (
            <tr key={p.symbol} className="border-t border-gray-800 hover:bg-gray-800/50">
              <td className="px-4 py-2 font-medium">{p.symbol}</td>
              <td className={`px-4 py-2 font-semibold ${p.side === 'Long' ? 'text-green-400' : 'text-red-400'}`}>{p.side}</td>
              <td className="px-4 py-2 font-mono">{p.size}</td>
              <td className="px-4 py-2 font-mono">{p.entryPrice.toFixed(4)}</td>
              <td className="px-4 py-2 font-mono">{p.markPrice.toFixed(4)}</td>
              <td className={`px-4 py-2 font-mono font-semibold ${p.unrealizedPnl >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                {p.unrealizedPnl >= 0 ? '+' : ''}{p.unrealizedPnl.toFixed(2)}
              </td>
              <td className="px-4 py-2">{p.leverage}x</td>
              <td className="px-4 py-2">
                <button onClick={() => tradingService.closePosition(p.symbol).then(() => setPositions((ps) => ps.filter((x) => x.symbol !== p.symbol)))}
                  className="text-xs text-red-400 hover:text-red-300 border border-red-800 rounded px-2 py-0.5">
                  Close
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
