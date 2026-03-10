import { useState, type FormEvent } from 'react';
import { OrderSide, OrderType, SubscriptionTier } from '@/types';
import { useSubscriptionGate } from '@/hooks/useSubscriptionGate';
import { tradingService } from '@/services/tradingService';

interface Props { symbol?: string; }

export default function OrderPanel({ symbol: defaultSymbol = 'BTCUSDT' }: Props) {
  const { hasAccess } = useSubscriptionGate(SubscriptionTier.ProPlus);
  const [symbol, setSymbol] = useState(defaultSymbol);
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy);
  const [orderType, setOrderType] = useState<OrderType>(OrderType.Market);
  const [quantity, setQuantity] = useState('');
  const [price, setPrice] = useState('');
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState('');

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!hasAccess) return;
    setLoading(true); setMsg('');
    try {
      await tradingService.placeOrder({ symbol, side, orderType, quantity: parseFloat(quantity), price: price ? parseFloat(price) : undefined });
      setMsg('Order placed successfully');
      setQuantity(''); setPrice('');
    } catch { setMsg('Failed to place order'); }
    finally { setLoading(false); }
  };

  return (
    <div className="card">
      <h3 className="font-semibold mb-4">Place Order</h3>
      {!hasAccess && (
        <div className="bg-purple-900/30 border border-purple-700 rounded-lg p-3 mb-4 text-sm text-purple-300">
          Order execution requires Pro+ subscription
        </div>
      )}
      <form onSubmit={handleSubmit} className="space-y-3">
        <div>
          <label className="text-xs text-gray-400 mb-1 block">Symbol</label>
          <input className="input" value={symbol} onChange={(e) => setSymbol(e.target.value)} required />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="text-xs text-gray-400 mb-1 block">Side</label>
            <select className="input" value={side} onChange={(e) => setSide(Number(e.target.value))}>
              <option value={OrderSide.Buy}>Buy</option>
              <option value={OrderSide.Sell}>Sell</option>
            </select>
          </div>
          <div>
            <label className="text-xs text-gray-400 mb-1 block">Type</label>
            <select className="input" value={orderType} onChange={(e) => setOrderType(Number(e.target.value))}>
              <option value={OrderType.Market}>Market</option>
              <option value={OrderType.Limit}>Limit</option>
              <option value={OrderType.StopLimit}>Stop Limit</option>
            </select>
          </div>
        </div>
        <div>
          <label className="text-xs text-gray-400 mb-1 block">Quantity</label>
          <input className="input" type="number" step="any" value={quantity} onChange={(e) => setQuantity(e.target.value)} required />
        </div>
        {orderType !== OrderType.Market && (
          <div>
            <label className="text-xs text-gray-400 mb-1 block">Price</label>
            <input className="input" type="number" step="any" value={price} onChange={(e) => setPrice(e.target.value)} required />
          </div>
        )}
        {msg && <p className="text-sm text-green-400">{msg}</p>}
        <button type="submit" disabled={loading || !hasAccess}
          className={`w-full font-medium py-2 px-4 rounded-lg transition-colors ${side === OrderSide.Buy ? 'bg-green-600 hover:bg-green-700 text-white' : 'bg-red-600 hover:bg-red-700 text-white'} disabled:opacity-50`}>
          {loading ? 'Placing…' : side === OrderSide.Buy ? 'Buy' : 'Sell'}
        </button>
      </form>
    </div>
  );
}
