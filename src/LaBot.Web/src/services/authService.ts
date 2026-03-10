import api from './api';
import type { AuthTokens, User } from '@/types';

export const authService = {
  async login(email: string, password: string): Promise<AuthTokens> {
    const { data } = await api.post<AuthTokens>('/auth/login', { email, password });
    return data;
  },

  async register(email: string, userName: string, password: string): Promise<AuthTokens> {
    const { data } = await api.post<AuthTokens>('/auth/register', { email, userName, password });
    return data;
  },

  async refreshToken(token: string): Promise<AuthTokens> {
    const { data } = await api.post<AuthTokens>('/auth/refresh', { refreshToken: token });
    return data;
  },

  async logout(refreshToken: string): Promise<void> {
    await api.post('/auth/logout', { refreshToken });
  },

  async me(): Promise<User> {
    const { data } = await api.get<User>('/auth/me');
    return data;
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await api.post('/auth/change-password', { currentPassword, newPassword });
  },
};
