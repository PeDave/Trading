import { useAuthStore } from '@/stores/authStore';
import { SubscriptionTier } from '@/types';

export function useSubscriptionGate(requiredTier: SubscriptionTier) {
  const tier = useAuthStore((s) => s.user?.subscriptionTier ?? SubscriptionTier.Free);
  return { hasAccess: tier >= requiredTier, currentTier: tier };
}
