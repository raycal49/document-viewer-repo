import { useRef, useEffect, useCallback, useState } from 'react';
import type { Stroke, AnnotationVector } from './AnnotationLogic';
import { processMouseDown, processMouseMove, finaliseStroke } from './AnnotationLogic';

export type { Stroke, AnnotationVector };

const STROKE_TTL_MS = 2000;

interface AnnotationCanvasProps {
  enabled: boolean;
  videoRef: React.RefObject<HTMLVideoElement | null>;
  strokeColor: string;
  onStroke?: (stroke: Stroke) => void;
  onVector?: (vector: AnnotationVector) => void;
}

interface LiveStroke {
  id: number;
  points: Array<{ x: number; y: number }>;
  color: string;
}

function getVideoContentRect(video: HTMLVideoElement): DOMRect {
  const vw = video.videoWidth;
  const vh = video.videoHeight;
  const cr = video.getBoundingClientRect();
  if (!vw || !vh) return cr;

  const containerAspect = cr.width / cr.height;
  const videoAspect     = vw / vh;
  const isLetterboxed   = videoAspect > containerAspect;

  const renderedWidth  = isLetterboxed ? cr.width  : cr.height * videoAspect;
  const renderedHeight = isLetterboxed ? cr.width / videoAspect : cr.height;

  return new DOMRect(
    cr.left + (cr.width  - renderedWidth)  / 2,
    cr.top  + (cr.height - renderedHeight) / 2,
    renderedWidth,
    renderedHeight,
  );
}

function redrawStrokes(canvas: HTMLCanvasElement, strokes: LiveStroke[]) {
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  for (const stroke of strokes) {
    if (stroke.points.length < 2) continue;
    ctx.strokeStyle = stroke.color;
    ctx.lineWidth   = 8;
    ctx.lineCap     = 'round';
    ctx.lineJoin    = 'round';
    ctx.beginPath();
    ctx.moveTo(stroke.points[0].x, stroke.points[0].y);
    for (let i = 1; i < stroke.points.length; i++) {
      ctx.lineTo(stroke.points[i].x, stroke.points[i].y);
    }
    ctx.stroke();
  }
}

function useVideoContentCanvas(
  canvasRef: React.RefObject<HTMLCanvasElement | null>,
  videoRef:  React.RefObject<HTMLVideoElement | null>,
  enabled:   boolean,
) {
  const syncCanvas = useCallback(() => {
    const canvas = canvasRef.current;
    const video  = videoRef.current;
    if (!canvas || !video) return;
    const rect = getVideoContentRect(video);
    canvas.style.left   = `${rect.left}px`;
    canvas.style.top    = `${rect.top}px`;
    canvas.style.width  = `${rect.width}px`;
    canvas.style.height = `${rect.height}px`;
    canvas.width  = rect.width;
    canvas.height = rect.height;
  }, [canvasRef, videoRef]);

  useEffect(() => {
    if (!enabled) return;
    syncCanvas();
    const timers = [100, 500, 1000, 2000].map(d => setTimeout(syncCanvas, d));
    window.addEventListener('resize', syncCanvas);
    const video = videoRef.current;
    video?.addEventListener('loadedmetadata', syncCanvas);
    video?.addEventListener('resize', syncCanvas);
    return () => {
      window.removeEventListener('resize', syncCanvas);
      video?.removeEventListener('loadedmetadata', syncCanvas);
      video?.removeEventListener('resize', syncCanvas);
      timers.forEach(clearTimeout);
    };
  }, [enabled, syncCanvas, videoRef]);
}

function useStrokeTTL(canvasRef: React.RefObject<HTMLCanvasElement | null>) {
  const liveStrokesRef = useRef<LiveStroke[]>([]);
  const strokeIdRef    = useRef(0);

  const registerStroke = useCallback((points: Array<{ x: number; y: number }>, color: string) => {
    const id = ++strokeIdRef.current;
    liveStrokesRef.current = [...liveStrokesRef.current, { id, points, color }];

    setTimeout(() => {
      liveStrokesRef.current = liveStrokesRef.current.filter(s => s.id !== id);
      if (canvasRef.current) redrawStrokes(canvasRef.current, liveStrokesRef.current);
    }, STROKE_TTL_MS);
  }, [canvasRef]);

  return { registerStroke };
}

function useMouseHandlers(
  canvasRef: React.RefObject<HTMLCanvasElement | null>,
  options: {
    enabled: boolean,
    strokeColor: string,
    onStroke: ((stroke: Stroke) => void) | undefined,
    registerStroke: (points: Array<{ x: number; y: number }>, color: string) => void,
  }
) {
  const { enabled, strokeColor, onStroke, registerStroke } = options;
  const [isDrawing, setIsDrawing]         = useState(false);
  const [currentStroke, setCurrentStroke] = useState<Array<{ x: number; y: number }>>([]);

  const getCtxAndRect = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return null;
    return { ctx: canvas.getContext('2d'), rect: canvas.getBoundingClientRect(), width: canvas.width, height: canvas.height };
  }, [canvasRef]);

  const handleMouseDown = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!enabled) return;
    const cv = getCtxAndRect();
    if (!cv) return;
    const { initialStroke } = processMouseDown({
      clientX: e.clientX, clientY: e.clientY,
      canvasRect: cv.rect, canvasWidth: cv.width, canvasHeight: cv.height,
      ctx: cv.ctx,
    });
    if (cv.ctx) cv.ctx.strokeStyle = strokeColor;
    setIsDrawing(true);
    setCurrentStroke(initialStroke);
  }, [enabled, getCtxAndRect, strokeColor]);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!enabled || !isDrawing) return;
    const cv = getCtxAndRect();
    if (!cv) return;
    if (cv.ctx) cv.ctx.strokeStyle = strokeColor;
    const { position } = processMouseMove({
      clientX: e.clientX, clientY: e.clientY,
      canvasRect: cv.rect, canvasWidth: cv.width, canvasHeight: cv.height,
      ctx: cv.ctx,
    });
    setCurrentStroke(prev => [...prev, position]);
  }, [enabled, isDrawing, getCtxAndRect, strokeColor]);

  const handleMouseUp = () => {
    if (!enabled || !isDrawing) return;
    const cv = getCtxAndRect();
    finaliseStroke({ isDrawing, currentStroke, canvasWidth: cv?.width ?? 1, canvasHeight: cv?.height ?? 1, onStroke });
    registerStroke(currentStroke, strokeColor);
    setIsDrawing(false);
    setCurrentStroke([]);
  };

  const handleMouseLeave = () => {
    if (isDrawing) handleMouseUp();
  };

  return { handleMouseDown, handleMouseMove, handleMouseUp, handleMouseLeave };
}

export default function AnnotationCanvas({ enabled, videoRef, strokeColor, onStroke }: AnnotationCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useVideoContentCanvas(canvasRef, videoRef, enabled);
  const { registerStroke } = useStrokeTTL(canvasRef);
  const { handleMouseDown, handleMouseMove, handleMouseUp, handleMouseLeave } = useMouseHandlers(canvasRef, {enabled, strokeColor,onStroke, registerStroke});

  if (!enabled) return null;

  return (
    <canvas
      ref={canvasRef}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseLeave}
      style={{ position: 'fixed', zIndex: 10, cursor: 'crosshair', pointerEvents: 'auto', display: 'block' }}
    />
  );
}