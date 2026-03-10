export enum SubscriptionTier {
  Free = 0,
  Pro = 1,
  ProPlus = 2,
}

export enum SignalDirection {
  Long = 0,
  Short = 1,
}

export enum SignalStatus {
  Active = 0,
  TP1Hit = 1,
  TP2Hit = 2,
  TP3Hit = 3,
  StopLoss = 4,
  Expired = 5,
  Cancelled = 6,
}

export enum OrderSide {
  Buy = 0,
  Sell = 1,
}

export enum OrderType {
  Market = 0,
  Limit = 1,
  StopLimit = 2,
}

export interface User {
  id: string;
  email: string;
  userName: string;
  subscriptionTier: SubscriptionTier;
  role: string;
  createdAt: string;
  lastLogin: string;
  isActive: boolean;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface TradingSignal {
  id: string;
  symbol: string;
  direction: SignalDirection;
  entryPrice: number;
  stopLoss: number;
  takeProfit1: number;
  takeProfit2: number;
  takeProfit3: number;
  riskRewardRatio: number;
  confidence: number;
  timeframe: string;
  status: SignalStatus;
  minTier: SubscriptionTier;
  createdAt: string;
  expiresAt: string;
  source: string;
}

export interface SpotTicker {
  symbol: string;
  lastPr: string;
  open24h: string;
  high24h: string;
  low24h: string;
  quoteVolume: string;
  baseVolume: string;
  priceChangePercent: string;
  ts: string;
}

export interface CandleData {
  timestamp: string;
  open: string;
  high: string;
  low: string;
  close: string;
  volume: string;
  quoteVolume: string;
}

export interface FeatureGate {
  id: string;
  featureName: string;
  minTier: SubscriptionTier;
  isEnabled: boolean;
  delayMinutes: number;
  scheduleStart?: string;
  scheduleEnd?: string;
  updatedAt: string;
}

export interface Order {
  id: string;
  symbol: string;
  side: OrderSide;
  orderType: OrderType;
  price?: number;
  quantity: number;
  status: string;
  filledQuantity: number;
  createdAt: string;
}

export interface Playbook {
  id: string;
  name: string;
  description: string;
  symbol: string;
  timeframe: string;
  probability: number;
  status: string;
  createdAt: string;
}

export interface ChartAnalysis {
  id: string;
  title: string;
  imagePath: string;
  annotationData: string;
  playbookId?: string;
  createdAt: string;
}

export interface Subscription {
  id: string;
  tier: SubscriptionTier;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface ApiResponse<T> {
  data?: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
