import { create } from 'zustand';
import type { TradingSignal } from '@/types';
import { signalService } from '@/services/signalService';

interface SignalState {
  signals: TradingSignal[];
  isLoading: boolean;
  totalCount: number;
  page: number;
  fetchSignals: (page?: number) => Promise<void>;
  addSignal: (signal: TradingSignal) => void;
  updateSignal: (signal: TradingSignal) => void;
}

export const useSignalStore = create<SignalState>((set) => ({
  signals: [],
  isLoading: false,
  totalCount: 0,
  page: 1,

  fetchSignals: async (page = 1) => {
    set({ isLoading: true, page });
    try {
      const result = await signalService.getSignals(page);
      set({ signals: result.items, totalCount: result.totalCount });
    } finally {
      set({ isLoading: false });
    }
  },

  addSignal: (signal) =>
    set((state) => ({ signals: [signal, ...state.signals] })),

  updateSignal: (signal) =>
    set((state) => ({
      signals: state.signals.map((s) => (s.id === signal.id ? signal : s)),
    })),
}));
