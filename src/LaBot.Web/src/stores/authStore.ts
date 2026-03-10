import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User, SubscriptionTier } from '@/types';
import { authService } from '@/services/authService';

interface AuthState {
  accessToken: string | null;
  refreshTokenValue: string | null;
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  userTier: SubscriptionTier;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<void>;
  loadUser: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshTokenValue: null,
      user: null,
      isAuthenticated: false,
      isAdmin: false,
      userTier: 0 as SubscriptionTier,

      login: async (email, password) => {
        const tokens = await authService.login(email, password);
        set({
          accessToken: tokens.accessToken,
          refreshTokenValue: tokens.refreshToken,
          user: tokens.user,
          isAuthenticated: true,
          isAdmin: tokens.user.role === 'Admin',
          userTier: tokens.user.subscriptionTier,
        });
      },

      logout: () => {
        const token = get().refreshTokenValue;
        if (token) authService.logout(token).catch(() => undefined);
        set({
          accessToken: null,
          refreshTokenValue: null,
          user: null,
          isAuthenticated: false,
          isAdmin: false,
          userTier: 0 as SubscriptionTier,
        });
      },

      refreshToken: async () => {
        const token = get().refreshTokenValue;
        if (!token) throw new Error('No refresh token');
        const tokens = await authService.refreshToken(token);
        set({
          accessToken: tokens.accessToken,
          refreshTokenValue: tokens.refreshToken,
          user: tokens.user,
          isAuthenticated: true,
          isAdmin: tokens.user.role === 'Admin',
          userTier: tokens.user.subscriptionTier,
        });
      },

      loadUser: async () => {
        if (!get().accessToken) return;
        try {
          const user = await authService.me();
          set({ user, isAdmin: user.role === 'Admin', userTier: user.subscriptionTier });
        } catch {
          get().logout();
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshTokenValue: state.refreshTokenValue,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        isAdmin: state.isAdmin,
        userTier: state.userTier,
      }),
    }
  )
);
