import api from './api';
import type { Order, OrderSide, OrderType } from '@/types';

interface PlaceOrderParams {
  symbol: string;
  side: OrderSide;
  orderType: OrderType;
  quantity: number;
  price?: number;
  stopPrice?: number;
}

interface Position {
  symbol: string;
  side: string;
  size: number;
  entryPrice: number;
  markPrice: number;
  unrealizedPnl: number;
  leverage: number;
}

interface AccountInfo {
  totalBalance: number;
  availableBalance: number;
  totalUnrealizedPnl: number;
  currency: string;
}

export const tradingService = {
  async placeOrder(params: PlaceOrderParams): Promise<Order> {
    const { data } = await api.post<Order>('/trading/orders', params);
    return data;
  },

  async cancelOrder(orderId: string): Promise<void> {
    await api.delete(`/trading/orders/${orderId}`);
  },

  async getOrders(symbol?: string): Promise<Order[]> {
    const { data } = await api.get<Order[]>('/trading/orders', { params: { symbol } });
    return data;
  },

  async getPositions(): Promise<Position[]> {
    const { data } = await api.get<Position[]>('/trading/positions');
    return data;
  },

  async getAccount(): Promise<AccountInfo> {
    const { data } = await api.get<AccountInfo>('/trading/account');
    return data;
  },

  async closePosition(symbol: string): Promise<void> {
    await api.post(`/trading/positions/${symbol}/close`);
  },
};
