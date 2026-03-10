import { useEffect, useState } from 'react';
import api from '@/services/api';
import type { User } from '@/types';
import { SubscriptionTier } from '@/types';
import TierBadge from '@/components/subscription/TierBadge';

export default function UserManagement() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<User[]>('/admin/users').then((r) => setUsers(r.data)).catch(() => setUsers([])).finally(() => setLoading(false));
  }, []);

  const changeTier = async (userId: string, tier: SubscriptionTier) => {
    await api.put(`/admin/users/${userId}/tier`, { tier });
    setUsers((us) => us.map((u) => u.id === userId ? { ...u, subscriptionTier: tier } : u));
  };

  const toggleActive = async (user: User) => {
    await api.put(`/admin/users/${user.id}/status`, { isActive: !user.isActive });
    setUsers((us) => us.map((u) => u.id === user.id ? { ...u, isActive: !u.isActive } : u));
  };

  if (loading) return <div className="text-gray-400 text-sm">Loading users…</div>;

  return (
    <div className="card p-0 overflow-x-auto">
      <table className="w-full text-sm min-w-[800px]">
        <thead className="bg-gray-800 text-gray-400">
          <tr>{['Email','Username','Tier','Role','Status','Created','Actions'].map((h) => <th key={h} className="px-4 py-3 text-left font-medium">{h}</th>)}</tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id} className="border-t border-gray-800 hover:bg-gray-800/50">
              <td className="px-4 py-2">{u.email}</td>
              <td className="px-4 py-2 font-medium">{u.userName}</td>
              <td className="px-4 py-2">
                <select value={u.subscriptionTier} onChange={(e) => changeTier(u.id, Number(e.target.value))}
                  className="bg-gray-800 border border-gray-700 text-gray-100 rounded px-2 py-1 text-xs">
                  <option value={SubscriptionTier.Free}>Free</option>
                  <option value={SubscriptionTier.Pro}>Pro</option>
                  <option value={SubscriptionTier.ProPlus}>Pro+</option>
                </select>
              </td>
              <td className="px-4 py-2"><span className="text-yellow-400 text-xs">{u.role}</span></td>
              <td className="px-4 py-2"><span className={`text-xs px-2 py-0.5 rounded ${u.isActive ? 'bg-green-900/40 text-green-400' : 'bg-red-900/40 text-red-400'}`}>{u.isActive ? 'Active' : 'Disabled'}</span></td>
              <td className="px-4 py-2 text-gray-500 text-xs">{new Date(u.createdAt).toLocaleDateString()}</td>
              <td className="px-4 py-2 flex gap-2">
                <button onClick={() => toggleActive(u)} className={`text-xs px-2 py-0.5 rounded border ${u.isActive ? 'border-red-800 text-red-400 hover:bg-red-900/30' : 'border-green-800 text-green-400 hover:bg-green-900/30'}`}>
                  {u.isActive ? 'Disable' : 'Enable'}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {users.length === 0 && <p className="text-center text-gray-500 py-8">No users found</p>}
    </div>
  );
}

export { TierBadge };
