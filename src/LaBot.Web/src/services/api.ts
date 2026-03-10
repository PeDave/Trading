import axios from 'axios';
import { useAuthStore } from '@/stores/authStore';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    const axiosError = error as { config?: { _retry?: boolean; headers?: Record<string, string> }; response?: { status?: number } };
    const original = axiosError.config;
    if (axiosError.response?.status === 401 && original && !original._retry) {
      original._retry = true;
      try {
        await useAuthStore.getState().refreshToken();
        const token = useAuthStore.getState().accessToken;
        if (original.headers) original.headers.Authorization = `Bearer ${token}`;
        return api(original as Parameters<typeof api>[0]);
      } catch {
        useAuthStore.getState().logout();
      }
    }
    return Promise.reject(error);
  }
);

export default api;
