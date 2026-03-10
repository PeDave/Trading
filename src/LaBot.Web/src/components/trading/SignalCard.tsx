import type { TradingSignal } from '@/types';
import { SignalDirection, SignalStatus } from '@/types';
import TierBadge from '@/components/subscription/TierBadge';

interface Props { signal: TradingSignal; locked?: boolean; }

const statusLabel: Record<SignalStatus, string> = { [SignalStatus.Active]: 'Active', [SignalStatus.TP1Hit]: 'TP1 ✓', [SignalStatus.TP2Hit]: 'TP2 ✓', [SignalStatus.TP3Hit]: 'TP3 ✓', [SignalStatus.StopLoss]: 'SL Hit', [SignalStatus.Expired]: 'Expired', [SignalStatus.Cancelled]: 'Cancelled' };
const statusColor: Record<SignalStatus, string> = { [SignalStatus.Active]: 'text-green-400 bg-green-900/30', [SignalStatus.TP1Hit]: 'text-blue-400 bg-blue-900/30', [SignalStatus.TP2Hit]: 'text-blue-400 bg-blue-900/30', [SignalStatus.TP3Hit]: 'text-purple-400 bg-purple-900/30', [SignalStatus.StopLoss]: 'text-red-400 bg-red-900/30', [SignalStatus.Expired]: 'text-gray-400 bg-gray-800', [SignalStatus.Cancelled]: 'text-gray-500 bg-gray-800' };

export default function SignalCard({ signal, locked }: Props) {
  const isLong = signal.direction === SignalDirection.Long;
  return (
    <div className={`card relative overflow-hidden ${locked ? 'select-none' : ''}`}>
      {locked && (
        <div className="absolute inset-0 bg-gray-900/80 backdrop-blur-sm flex flex-col items-center justify-center z-10 rounded-xl">
          <span className="text-2xl mb-1">🔒</span>
          <p className="text-sm text-gray-400">Upgrade to unlock</p>
          <TierBadge tier={signal.minTier} />
        </div>
      )}
      <div className="flex items-start justify-between mb-3">
        <div>
          <span className="font-bold text-lg text-gray-100">{signal.symbol}</span>
          <span className={`ml-2 text-xs font-semibold px-2 py-0.5 rounded ${isLong ? 'bg-green-900/40 text-green-400' : 'bg-red-900/40 text-red-400'}`}>
            {isLong ? '▲ LONG' : '▼ SHORT'}
          </span>
        </div>
        <span className={`text-xs px-2 py-0.5 rounded font-medium ${statusColor[signal.status]}`}>{statusLabel[signal.status]}</span>
      </div>
      <div className="grid grid-cols-2 gap-2 text-sm mb-3">
        <div><span className="text-gray-500">Entry</span><p className="text-gray-100 font-mono">{signal.entryPrice.toFixed(4)}</p></div>
        <div><span className="text-gray-500">Stop Loss</span><p className="text-red-400 font-mono">{signal.stopLoss.toFixed(4)}</p></div>
        <div><span className="text-gray-500">TP1</span><p className="text-green-400 font-mono">{signal.takeProfit1.toFixed(4)}</p></div>
        <div><span className="text-gray-500">TP2</span><p className="text-green-300 font-mono">{signal.takeProfit2.toFixed(4)}</p></div>
        <div><span className="text-gray-500">TP3</span><p className="text-green-200 font-mono">{signal.takeProfit3.toFixed(4)}</p></div>
        <div><span className="text-gray-500">RR Ratio</span><p className="text-blue-400 font-mono">{signal.riskRewardRatio.toFixed(2)}R</p></div>
      </div>
      <div className="mb-2">
        <div className="flex justify-between text-xs text-gray-500 mb-1">
          <span>Confidence</span><span>{signal.confidence}%</span>
        </div>
        <div className="h-1.5 bg-gray-800 rounded-full overflow-hidden">
          <div className="h-full bg-blue-500 rounded-full" style={{ width: `${signal.confidence}%` }} />
        </div>
      </div>
      <div className="flex items-center justify-between text-xs text-gray-500 mt-3">
        <span>{signal.timeframe} · {signal.source}</span>
        <TierBadge tier={signal.minTier} small />
      </div>
    </div>
  );
}
