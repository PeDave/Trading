import { SubscriptionTier } from '@/types';
import { clsx } from 'clsx';

interface Props { tier: SubscriptionTier; small?: boolean; }

const labels: Record<SubscriptionTier, string> = { [SubscriptionTier.Free]: 'Free', [SubscriptionTier.Pro]: 'Pro', [SubscriptionTier.ProPlus]: 'Pro+' };
const colors: Record<SubscriptionTier, string> = { [SubscriptionTier.Free]: 'bg-gray-700 text-gray-300', [SubscriptionTier.Pro]: 'bg-blue-700 text-blue-200', [SubscriptionTier.ProPlus]: 'bg-purple-700 text-purple-200' };

export default function TierBadge({ tier, small }: Props) {
  return (
    <span className={clsx('rounded font-semibold uppercase tracking-wide', colors[tier], small ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1')}>
      {labels[tier]}
    </span>
  );
}
