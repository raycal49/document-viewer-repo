export interface Stroke {
  points: Array<{ x: number; y: number }>;
  color: string;
  width: number;
  timestamp: number;
}

export interface AnnotationVector {
  x: number;
  y: number;
  type: 'point';
}

export function annotationToVector(
  pixelX: number,
  pixelY: number,
  canvasWidth: number,
  canvasHeight: number
): AnnotationVector {
  return {
    x: (pixelX / canvasWidth) ,
    y: (pixelY / canvasHeight) ,
    type: 'point',
  };
}

export function initializeDrawing(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number
): void {
  ctx.strokeStyle = '#FF0000';
  ctx.lineWidth = 8;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  ctx.moveTo(x, y);
}

export interface SendVectorParams {
  onVector: ((vector: AnnotationVector) => void) | undefined;
  x: number;
  y: number;
  canvasWidth: number;
  canvasHeight: number;
}

export function sendVectorIfValid(params: SendVectorParams): void {
  const { onVector, x, y, canvasWidth, canvasHeight } = params;
  if (onVector && canvasWidth > 0 && canvasHeight > 0) {
    onVector(annotationToVector(x, y, canvasWidth, canvasHeight));
  }
}

export interface CanvasRect {
  left: number;
  top: number;
}

export interface MousePosition {
  x: number;
  y: number;
}

export function getCanvasPosition(
  clientX: number,
  clientY: number,
  rect: CanvasRect
): MousePosition {
  return {
    x: clientX - rect.left,
    y: clientY - rect.top,
  };
}

export interface MouseDownParams {
  clientX: number;
  clientY: number;
  canvasRect: CanvasRect;
  canvasWidth: number;
  canvasHeight: number;
  ctx: CanvasRenderingContext2D | null;
}

export interface MouseDownResult {
  position: MousePosition;
  initialStroke: Array<MousePosition>;
}

export function processMouseDown(params: MouseDownParams): MouseDownResult {
  const { clientX, clientY, canvasRect, ctx } = params;

  const position = getCanvasPosition(clientX, clientY, canvasRect);

  if (ctx) {
    initializeDrawing(ctx, position.x, position.y);
  }

  return { position, initialStroke: [position] };
}

export interface MouseMoveParams {
  clientX: number;
  clientY: number;
  canvasRect: CanvasRect;
  canvasWidth: number;
  canvasHeight: number;
  ctx: CanvasRenderingContext2D | null;
}

export interface MouseMoveResult {
  position: MousePosition;
}

export function processMouseMove(params: MouseMoveParams): MouseMoveResult {
  const { clientX, clientY, canvasRect, ctx } = params;

  const position = getCanvasPosition(clientX, clientY, canvasRect);

  if (ctx) {
    ctx.lineTo(position.x, position.y);
    ctx.stroke();
  }

  return { position };
}

export interface RGBColor {
  r: number;
  g: number;
  b: number;
}

export function parseColor(color: string): RGBColor {
  const named: Record<string, RGBColor> = {
    red:   { r: 255, g: 0,   b: 0   },
    green: { r: 0,   g: 255, b: 0   },
    blue:  { r: 0,   g: 0,   b: 255 },
    white: { r: 255, g: 255, b: 255 },
    black: { r: 0,   g: 0,   b: 0   },
  };
  const lower = color.trim().toLowerCase();
  if (named[lower]) return named[lower];

  const rgbMatch = lower.match(/^rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$/);
  if (rgbMatch) {
    return { r: Number(rgbMatch[1]), g: Number(rgbMatch[2]), b: Number(rgbMatch[3]) };
  }

  const hex = lower.replace('#', '');
  if (hex.length === 6) {
    return {
      r: parseInt(hex.slice(0, 2), 16),
      g: parseInt(hex.slice(2, 4), 16),
      b: parseInt(hex.slice(4, 6), 16),
    };
  }
  if (hex.length === 3) {
    return {
      r: parseInt(hex[0] + hex[0], 16),
      g: parseInt(hex[1] + hex[1], 16),
      b: parseInt(hex[2] + hex[2], 16),
    };
  }

  return { r: 255, g: 255, b: 255 }; // fallback
}

export type StrokeType = 'fading' | 'permanent';

export interface AnnotationPayload {
  Type: 'stroke';
  Vector: [number, number][];
  Color: [number, number, number];
  StrokeType: StrokeType;
  FadeDuration?: number;
}

export interface ClearAnnotationsPayload {
  Type: 'clear-annotations';
}
 
export type AnnotationMessage = AnnotationPayload | ClearAnnotationsPayload;
 
export const FADING_DURATION_MS = 2000;

export function strokeToAnnotation(
  stroke: Stroke,
  strokeType: StrokeType,
  fadeDuration: number = FADING_DURATION_MS,
): AnnotationPayload {
  const { r, g, b } = parseColor(stroke.color);
  return {
    Type: 'stroke',
    Vector: stroke.points.map(p => [p.x, p.y]),
    Color: [r, g, b],
    StrokeType: strokeType,
    ...(strokeType === 'fading' ? { FadeDuration: fadeDuration } : {}),
  };
}

export interface FinaliseStrokeParams {
  isDrawing: boolean;
  currentStroke: Array<MousePosition>;
  canvasWidth: number;
  canvasHeight: number;
  onStroke: ((stroke: Stroke) => void) | undefined;
}

export function makeClearAnnotations(): ClearAnnotationsPayload {
  return { Type: 'clear-annotations' };
}

export function finaliseStroke(params: FinaliseStrokeParams): Stroke | null {
  const { isDrawing, currentStroke, canvasWidth, canvasHeight, onStroke } = params;

  if (!isDrawing || currentStroke.length === 0 || !onStroke) return null;

  const normalisedPoints = currentStroke.map(p => ({
    x: p.x / canvasWidth,
    y: p.y / canvasHeight,
  }));

  const stroke: Stroke = {
    points: normalisedPoints,
    color: 'red',
    width: 8,
    timestamp: Date.now(),
  };

  onStroke(stroke);
  return stroke;
}