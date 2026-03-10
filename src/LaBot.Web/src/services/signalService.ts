import api from './api';
import type { TradingSignal, FeatureGate, PaginatedResult } from '@/types';

interface SignalPerformance {
  totalSignals: number;
  winRate: number;
  avgRR: number;
  profitFactor: number;
}

export const signalService = {
  async getSignals(page = 1, pageSize = 20): Promise<PaginatedResult<TradingSignal>> {
    const { data } = await api.get<PaginatedResult<TradingSignal>>('/signals', {
      params: { page, pageSize },
    });
    return data;
  },

  async getSignal(id: string): Promise<TradingSignal> {
    const { data } = await api.get<TradingSignal>(`/signals/${id}`);
    return data;
  },

  async createSignal(signal: Omit<TradingSignal, 'id' | 'createdAt'>): Promise<TradingSignal> {
    const { data } = await api.post<TradingSignal>('/signals', signal);
    return data;
  },

  async updateSignal(id: string, signal: Partial<TradingSignal>): Promise<TradingSignal> {
    const { data } = await api.put<TradingSignal>(`/signals/${id}`, signal);
    return data;
  },

  async deleteSignal(id: string): Promise<void> {
    await api.delete(`/signals/${id}`);
  },

  async getPerformance(): Promise<SignalPerformance> {
    const { data } = await api.get<SignalPerformance>('/signals/performance');
    return data;
  },

  async getFeatureGates(): Promise<FeatureGate[]> {
    const { data } = await api.get<FeatureGate[]>('/admin/feature-gates');
    return data;
  },

  async updateFeatureGate(id: string, gate: Partial<FeatureGate>): Promise<FeatureGate> {
    const { data } = await api.put<FeatureGate>(`/admin/feature-gates/${id}`, gate);
    return data;
  },
};
