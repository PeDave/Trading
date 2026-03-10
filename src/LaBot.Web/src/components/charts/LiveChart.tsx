import { useEffect, useRef } from 'react';
import { createChart, ColorType, CandlestickSeries } from 'lightweight-charts';
import type { IChartApi, ISeriesApi, CandlestickData, Time } from 'lightweight-charts';
import { useMarketStore } from '@/stores/marketStore';

const GRANULARITIES = ['1m', '5m', '15m', '1H', '4H', '1D'];

export default function LiveChart() {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null);
  const { candles, selectedSymbol, selectedGranularity, setSelectedGranularity, fetchCandles, isLoadingCandles } = useMarketStore();

  useEffect(() => {
    if (!containerRef.current) return;
    const chart = createChart(containerRef.current, {
      layout: { background: { type: ColorType.Solid, color: '#111827' }, textColor: '#9ca3af' },
      grid: { vertLines: { color: '#1f2937' }, horzLines: { color: '#1f2937' } },
      width: containerRef.current.clientWidth,
      height: containerRef.current.clientHeight || 400,
    });
    chartRef.current = chart;
    seriesRef.current = chart.addSeries(CandlestickSeries, { upColor: '#22c55e', downColor: '#ef4444', borderVisible: false, wickUpColor: '#22c55e', wickDownColor: '#ef4444' });

    const ro = new ResizeObserver(() => {
      if (containerRef.current) chart.applyOptions({ width: containerRef.current.clientWidth, height: containerRef.current.clientHeight });
    });
    ro.observe(containerRef.current);
    return () => { ro.disconnect(); chart.remove(); };
  }, []);

  useEffect(() => {
    if (!seriesRef.current || candles.length === 0) return;
    const data: CandlestickData[] = candles
      .map((c) => ({ time: (Math.floor(new Date(c.timestamp).getTime() / 1000)) as Time, open: parseFloat(c.open), high: parseFloat(c.high), low: parseFloat(c.low), close: parseFloat(c.close) }))
      .sort((a, b) => (a.time as number) - (b.time as number));
    seriesRef.current.setData(data);
    chartRef.current?.timeScale().fitContent();
  }, [candles]);

  const handleGranularity = (g: string) => {
    setSelectedGranularity(g);
    fetchCandles(selectedSymbol, g);
  };

  return (
    <div className="card p-0 overflow-hidden flex flex-col h-[480px]">
      <div className="flex items-center gap-2 px-4 py-2 border-b border-gray-800 shrink-0">
        <span className="font-semibold text-gray-100">{selectedSymbol}</span>
        <div className="ml-auto flex gap-1">
          {GRANULARITIES.map((g) => (
            <button key={g} onClick={() => handleGranularity(g)}
              className={`px-2 py-0.5 text-xs rounded transition-colors ${selectedGranularity === g ? 'bg-blue-600 text-white' : 'text-gray-400 hover:bg-gray-800'}`}>
              {g}
            </button>
          ))}
        </div>
      </div>
      <div className="relative flex-1">
        {isLoadingCandles && (
          <div className="absolute inset-0 flex items-center justify-center bg-gray-900/70 z-10">
            <div className="text-gray-400 text-sm">Loading chart…</div>
          </div>
        )}
        <div ref={containerRef} className="w-full h-full" />
      </div>
    </div>
  );
}
