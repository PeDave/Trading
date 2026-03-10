import { create } from 'zustand';
import type { SpotTicker, CandleData } from '@/types';
import { marketService } from '@/services/marketService';

interface MarketState {
  tickers: Record<string, SpotTicker>;
  selectedSymbol: string;
  candles: CandleData[];
  selectedGranularity: string;
  isLoadingTickers: boolean;
  isLoadingCandles: boolean;
  setSelectedSymbol: (symbol: string) => void;
  setSelectedGranularity: (granularity: string) => void;
  fetchTickers: () => Promise<void>;
  fetchCandles: (symbol: string, granularity: string) => Promise<void>;
}

export const useMarketStore = create<MarketState>((set, get) => ({
  tickers: {},
  selectedSymbol: 'BTCUSDT',
  candles: [],
  selectedGranularity: '1H',
  isLoadingTickers: false,
  isLoadingCandles: false,

  setSelectedSymbol: (symbol) => set({ selectedSymbol: symbol }),
  setSelectedGranularity: (granularity) => set({ selectedGranularity: granularity }),

  fetchTickers: async () => {
    set({ isLoadingTickers: true });
    try {
      const list = await marketService.getSpotTickers();
      const map: Record<string, SpotTicker> = {};
      list.forEach((t) => { map[t.symbol] = t; });
      set({ tickers: map });
    } finally {
      set({ isLoadingTickers: false });
    }
  },

  fetchCandles: async (symbol, granularity) => {
    set({ isLoadingCandles: true, selectedSymbol: symbol, selectedGranularity: granularity });
    try {
      const candles = await marketService.getSpotCandles(symbol, granularity);
      set({ candles });
    } finally {
      set({ isLoadingCandles: false });
    }
    // ensure state is accessible via get() for future use
    void get;
  },
}));
