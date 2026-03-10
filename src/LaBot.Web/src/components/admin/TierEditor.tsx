import { useEffect, useState } from 'react';
import type { FeatureGate } from '@/types';
import { SubscriptionTier } from '@/types';
import { signalService } from '@/services/signalService';

export default function TierEditor() {
  const [gates, setGates] = useState<FeatureGate[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    signalService.getFeatureGates().then(setGates).catch(() => setGates([])).finally(() => setLoading(false));
  }, []);

  const update = async (id: string, patch: Partial<FeatureGate>) => {
    const updated = await signalService.updateFeatureGate(id, patch);
    setGates((gs) => gs.map((g) => g.id === id ? updated : g));
  };

  if (loading) return <div className="text-gray-400 text-sm">Loading feature gates…</div>;

  return (
    <div className="card p-0 overflow-x-auto">
      <table className="w-full text-sm min-w-[700px]">
        <thead className="bg-gray-800 text-gray-400">
          <tr>{['Feature','Min Tier','Enabled','Delay (min)','Schedule Start','Schedule End'].map((h) => <th key={h} className="px-4 py-3 text-left font-medium">{h}</th>)}</tr>
        </thead>
        <tbody>
          {gates.map((g) => (
            <tr key={g.id} className="border-t border-gray-800 hover:bg-gray-800/50">
              <td className="px-4 py-2 font-medium">{g.featureName}</td>
              <td className="px-4 py-2">
                <select value={g.minTier} onChange={(e) => update(g.id, { minTier: Number(e.target.value) })}
                  className="bg-gray-800 border border-gray-700 text-gray-100 rounded px-2 py-1 text-xs">
                  <option value={SubscriptionTier.Free}>Free</option>
                  <option value={SubscriptionTier.Pro}>Pro</option>
                  <option value={SubscriptionTier.ProPlus}>Pro+</option>
                </select>
              </td>
              <td className="px-4 py-2">
                <input type="checkbox" checked={g.isEnabled} onChange={(e) => update(g.id, { isEnabled: e.target.checked })} className="w-4 h-4 accent-blue-500" />
              </td>
              <td className="px-4 py-2">
                <input type="number" value={g.delayMinutes} onChange={(e) => update(g.id, { delayMinutes: Number(e.target.value) })}
                  className="bg-gray-800 border border-gray-700 text-gray-100 rounded px-2 py-1 text-xs w-20" />
              </td>
              <td className="px-4 py-2">
                <input type="datetime-local" value={g.scheduleStart ?? ''} onChange={(e) => update(g.id, { scheduleStart: e.target.value })}
                  className="bg-gray-800 border border-gray-700 text-gray-100 rounded px-2 py-1 text-xs" />
              </td>
              <td className="px-4 py-2">
                <input type="datetime-local" value={g.scheduleEnd ?? ''} onChange={(e) => update(g.id, { scheduleEnd: e.target.value })}
                  className="bg-gray-800 border border-gray-700 text-gray-100 rounded px-2 py-1 text-xs" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {gates.length === 0 && <p className="text-center text-gray-500 py-8">No feature gates configured</p>}
    </div>
  );
}
