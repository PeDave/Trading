import { SubscriptionTier } from '@/types';
import { Check } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useNavigate } from 'react-router-dom';

const plans = [
  { tier: SubscriptionTier.Free, name: 'Free', price: '$0', color: 'border-gray-700', btn: 'btn-secondary', features: ['5 signals/day', 'Basic market data', 'Community access'] },
  { tier: SubscriptionTier.Pro, name: 'Pro', price: '$29/mo', color: 'border-blue-600', btn: 'btn-primary', features: ['Unlimited signals', 'Real-time alerts', 'Chart analysis', 'Priority support'] },
  { tier: SubscriptionTier.ProPlus, name: 'Pro+', price: '$79/mo', color: 'border-purple-600', btn: 'bg-purple-600 hover:bg-purple-700 text-white font-medium py-2 px-4 rounded-lg transition-colors', features: ['Everything in Pro', 'AI-powered signals', 'Playbooks', 'Order execution', 'API access'] },
];

export default function PricingPage() {
  const { isAuthenticated, user } = useAuthStore();
  const navigate = useNavigate();
  const currentTier = user?.subscriptionTier ?? SubscriptionTier.Free;

  return (
    <div className="max-w-5xl mx-auto px-4 py-12">
      <h1 className="text-3xl font-bold text-center mb-2">Choose Your Plan</h1>
      <p className="text-gray-400 text-center mb-10">Unlock more signals and features as you grow</p>
      <div className="grid md:grid-cols-3 gap-6">
        {plans.map((p) => (
          <div key={p.tier} className={`card border-2 ${p.color} flex flex-col`}>
            <div className="mb-4">
              <h2 className="text-xl font-bold">{p.name}</h2>
              <p className="text-3xl font-bold mt-2">{p.price}</p>
            </div>
            <ul className="space-y-2 flex-1 mb-6">
              {p.features.map((f) => (
                <li key={f} className="flex items-center gap-2 text-sm text-gray-300">
                  <Check size={15} className="text-green-400 shrink-0" /> {f}
                </li>
              ))}
            </ul>
            {isAuthenticated && currentTier === p.tier ? (
              <span className="text-center text-sm text-gray-400 border border-gray-700 rounded-lg py-2">Current Plan</span>
            ) : (
              <button className={p.btn} onClick={() => navigate(isAuthenticated ? '/profile' : '/register')}>
                {p.tier === SubscriptionTier.Free ? 'Get Started' : 'Upgrade'}
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
