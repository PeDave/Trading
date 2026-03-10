import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { SubscriptionTier } from '@/types';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
  requireAuth?: boolean;
  requireAdmin?: boolean;
  requiredTier?: SubscriptionTier;
}

export default function ProtectedRoute({ children, requireAuth = true, requireAdmin = false, requiredTier }: Props) {
  const { isAuthenticated, isAdmin, user } = useAuthStore();
  const location = useLocation();

  if (requireAuth && !isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  if (requireAdmin && !isAdmin) {
    return <Navigate to="/" replace />;
  }
  if (requiredTier !== undefined && (user?.subscriptionTier ?? SubscriptionTier.Free) < requiredTier) {
    return <Navigate to="/pricing" replace />;
  }
  return <>{children}</>;
}
