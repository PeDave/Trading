import api from './api';
import type { SpotTicker, CandleData } from '@/types';

export const marketService = {
  async getSpotTickers(): Promise<SpotTicker[]> {
    const { data } = await api.get<SpotTicker[]>('/market/spot/tickers');
    return data;
  },

  async getFuturesTickers(): Promise<SpotTicker[]> {
    const { data } = await api.get<SpotTicker[]>('/market/futures/tickers');
    return data;
  },

  async getSpotCandles(symbol: string, granularity: string, limit = 200): Promise<CandleData[]> {
    const { data } = await api.get<CandleData[]>('/market/spot/candles', {
      params: { symbol, granularity, limit },
    });
    return data;
  },

  async getFuturesCandles(symbol: string, granularity: string, limit = 200): Promise<CandleData[]> {
    const { data } = await api.get<CandleData[]>('/market/futures/candles', {
      params: { symbol, granularity, limit },
    });
    return data;
  },

  async getOrderBook(symbol: string, limit = 20): Promise<{ asks: [string, string][]; bids: [string, string][] }> {
    const { data } = await api.get<{ asks: [string, string][]; bids: [string, string][] }>('/market/spot/orderbook', {
      params: { symbol, limit },
    });
    return data;
  },

  async getRecentTrades(symbol: string, limit = 50): Promise<{ price: string; qty: string; side: string; ts: string }[]> {
    const { data } = await api.get<{ price: string; qty: string; side: string; ts: string }[]>('/market/spot/trades', {
      params: { symbol, limit },
    });
    return data;
  },
};
