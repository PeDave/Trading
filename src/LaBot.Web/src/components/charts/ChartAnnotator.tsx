import { useState, useRef, useCallback } from 'react';
import { Stage, Layer, Image as KonvaImage, Line, Rect, Text as KonvaText, Arrow } from 'react-konva';
import type Konva from 'konva';
import { Minus, Square, Type, Trash2, RotateCcw, RotateCw, Download } from 'lucide-react';

type Tool = 'select' | 'line' | 'hline' | 'rect' | 'text' | 'arrow' | 'freehand';

interface DrawShape {
  id: string;
  tool: Tool;
  points?: number[];
  x?: number; y?: number;
  width?: number; height?: number;
  text?: string;
  color: string;
  strokeWidth: number;
}

interface Props { imageUrl?: string; width?: number; height?: number; onSave?: (dataUrl: string) => void; }

export default function ChartAnnotator({ imageUrl, width = 800, height = 500, onSave }: Props) {
  const [tool, setTool] = useState<Tool>('line');
  const [color, setColor] = useState('#3b82f6');
  const [strokeWidth, setStrokeWidth] = useState(2);
  const [shapes, setShapes] = useState<DrawShape[]>([]);
  const [history, setHistory] = useState<DrawShape[][]>([[]]);
  const [histIdx, setHistIdx] = useState(0);
  const [drawing, setDrawing] = useState(false);
  const [currentShape, setCurrentShape] = useState<DrawShape | null>(null);
  const stageRef = useRef<Konva.Stage>(null);
  const [bgImage, setBgImage] = useState<HTMLImageElement | null>(null);

  useState(() => {
    if (!imageUrl) return;
    const img = new window.Image();
    img.crossOrigin = 'anonymous';
    img.src = imageUrl;
    img.onload = () => setBgImage(img);
  });

  const commit = useCallback((newShapes: DrawShape[]) => {
    const next = history.slice(0, histIdx + 1);
    next.push(newShapes);
    setHistory(next);
    setHistIdx(next.length - 1);
    setShapes(newShapes);
  }, [history, histIdx]);

  const undo = () => { if (histIdx > 0) { setHistIdx(histIdx - 1); setShapes(history[histIdx - 1]); } };
  const redo = () => { if (histIdx < history.length - 1) { setHistIdx(histIdx + 1); setShapes(history[histIdx + 1]); } };

  const handleMouseDown = (e: Konva.KonvaEventObject<MouseEvent>) => {
    const pos = e.target.getStage()?.getPointerPosition();
    if (!pos) return;
    setDrawing(true);
    const id = crypto.randomUUID();
    if (tool === 'text') {
      const t = prompt('Enter text:');
      if (t) commit([...shapes, { id, tool, x: pos.x, y: pos.y, text: t, color, strokeWidth }]);
      return;
    }
    setCurrentShape({ id, tool, points: [pos.x, pos.y, pos.x, pos.y], x: pos.x, y: pos.y, width: 0, height: 0, color, strokeWidth });
  };

  const handleMouseMove = (e: Konva.KonvaEventObject<MouseEvent>) => {
    if (!drawing || !currentShape) return;
    const pos = e.target.getStage()?.getPointerPosition();
    if (!pos) return;
    const { x = 0, y = 0 } = currentShape;
    const updated: DrawShape = { ...currentShape };
    if (tool === 'hline') updated.points = [0, pos.y, width, pos.y];
    else if (tool === 'line' || tool === 'arrow' || tool === 'freehand') {
      updated.points = tool === 'freehand' ? [...(currentShape.points ?? []), pos.x, pos.y] : [x, y, pos.x, pos.y];
    } else if (tool === 'rect') { updated.width = pos.x - x; updated.height = pos.y - y; }
    setCurrentShape(updated);
  };

  const handleMouseUp = () => {
    if (!drawing || !currentShape) return;
    setDrawing(false);
    commit([...shapes, currentShape]);
    setCurrentShape(null);
  };

  const exportPng = () => {
    if (!stageRef.current) return;
    const dataUrl = stageRef.current.toDataURL({ pixelRatio: 2 });
    onSave?.(dataUrl);
    const a = document.createElement('a'); a.href = dataUrl; a.download = 'chart-analysis.png'; a.click();
  };

  const tools: { id: Tool; icon: React.ReactNode; label: string }[] = [
    { id: 'line', icon: <Minus size={16} />, label: 'Line' },
    { id: 'hline', icon: <Minus size={16} className="rotate-0" />, label: 'H-Line' },
    { id: 'rect', icon: <Square size={16} />, label: 'Rect' },
    { id: 'arrow', icon: <Minus size={16} />, label: 'Arrow' },
    { id: 'text', icon: <Type size={16} />, label: 'Text' },
    { id: 'freehand', icon: <Minus size={16} />, label: 'Free' },
  ];

  return (
    <div className="flex gap-3">
      <div className="flex flex-col gap-1 w-24 shrink-0">
        {tools.map((t) => (
          <button key={t.id} onClick={() => setTool(t.id)}
            className={`flex items-center gap-1 px-2 py-1.5 rounded text-xs transition-colors ${tool === t.id ? 'bg-blue-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'}`}>
            {t.icon} {t.label}
          </button>
        ))}
        <hr className="border-gray-700 my-1" />
        <label className="text-xs text-gray-400">Color</label>
        <input type="color" value={color} onChange={(e) => setColor(e.target.value)} className="w-full h-8 rounded cursor-pointer" />
        <label className="text-xs text-gray-400">Width</label>
        <input type="range" min={1} max={8} value={strokeWidth} onChange={(e) => setStrokeWidth(Number(e.target.value))} className="w-full" />
        <hr className="border-gray-700 my-1" />
        <button onClick={undo} disabled={histIdx === 0} className="flex items-center gap-1 px-2 py-1.5 rounded text-xs bg-gray-800 text-gray-400 hover:bg-gray-700 disabled:opacity-40">
          <RotateCcw size={14} /> Undo
        </button>
        <button onClick={redo} disabled={histIdx >= history.length - 1} className="flex items-center gap-1 px-2 py-1.5 rounded text-xs bg-gray-800 text-gray-400 hover:bg-gray-700 disabled:opacity-40">
          <RotateCw size={14} /> Redo
        </button>
        <button onClick={() => commit([])} className="flex items-center gap-1 px-2 py-1.5 rounded text-xs bg-red-900/50 text-red-400 hover:bg-red-900">
          <Trash2 size={14} /> Clear
        </button>
        <button onClick={exportPng} className="flex items-center gap-1 px-2 py-1.5 rounded text-xs bg-green-900/50 text-green-400 hover:bg-green-900 mt-1">
          <Download size={14} /> Export
        </button>
      </div>
      <div className="border border-gray-700 rounded-lg overflow-hidden">
        <Stage ref={stageRef} width={width} height={height}
          onMouseDown={handleMouseDown} onMouseMove={handleMouseMove} onMouseUp={handleMouseUp}
          style={{ background: '#111827', cursor: 'crosshair' }}>
          <Layer>
            {bgImage && <KonvaImage image={bgImage} width={width} height={height} />}
            {[...shapes, ...(currentShape ? [currentShape] : [])].map((s) => {
              if (s.tool === 'rect') return <Rect key={s.id} x={s.x} y={s.y} width={s.width} height={s.height} stroke={s.color} strokeWidth={s.strokeWidth} fill="transparent" />;
              if (s.tool === 'text') return <KonvaText key={s.id} x={s.x} y={s.y} text={s.text} fill={s.color} fontSize={16} />;
              if (s.tool === 'arrow') return <Arrow key={s.id} points={s.points ?? []} stroke={s.color} strokeWidth={s.strokeWidth} fill={s.color} />;
              return <Line key={s.id} points={s.points ?? []} stroke={s.color} strokeWidth={s.strokeWidth} lineCap="round" lineJoin="round" />;
            })}
          </Layer>
        </Stage>
      </div>
    </div>
  );
}
