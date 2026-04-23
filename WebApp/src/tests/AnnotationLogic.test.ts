import { describe, it, expect, vi } from 'vitest';
import {
  annotationToVector,
  initializeDrawing,
  sendVectorIfValid,
  getCanvasPosition,
  processMouseDown,
  processMouseMove,
  parseColor,
  strokeToAnnotation,
  finaliseStroke,
  makeClearAnnotations,
} from '../scenes/videoCallPage/Annotation/AnnotationLogic';
import type { Stroke } from '../scenes/videoCallPage/Annotation/AnnotationLogic';

function makeCtx(): CanvasRenderingContext2D {
  return {
    strokeStyle: '',
    lineWidth: 0,
    lineCap: '',
    lineJoin: '',
    beginPath: vi.fn(),
    moveTo: vi.fn(),
    lineTo: vi.fn(),
    stroke: vi.fn(),
  } as unknown as CanvasRenderingContext2D;
}

function makeStroke(overrides: Partial<Stroke> = {}): Stroke {
  return {
    points: [{ x: 0.5, y: 0.5 }],
    color: 'red',
    width: 8,
    timestamp: 0,
    ...overrides,
  };
}

describe('annotationToVector', () => {
  it('normalises pixel coords to the [0, 1] range', () => {
    const result = annotationToVector(100, 50, 200, 100);
    expect(result).toEqual({ x: 0.5, y: 0.5, type: 'point' });
  });

  it('produces 0 for a pixel at the origin', () => {
    const result = annotationToVector(0, 0, 400, 300);
    expect(result.x).toBe(0);
    expect(result.y).toBe(0);
  });

  it('produces 1 for a pixel at the far edge', () => {
    const result = annotationToVector(400, 300, 400, 300);
    expect(result.x).toBe(1);
    expect(result.y).toBe(1);
  });
});

describe('initializeDrawing', () => {
  it('sets strokeStyle, lineWidth, lineCap, and lineJoin', () => {
    const ctx = makeCtx();
    initializeDrawing(ctx, 10, 20);
    expect(ctx.strokeStyle).toBe('#FF0000');
    expect(ctx.lineWidth).toBe(8);
    expect(ctx.lineCap).toBe('round');
    expect(ctx.lineJoin).toBe('round');
  });

  it('calls beginPath then moveTo with the given coordinates', () => {
    const ctx = makeCtx();
    initializeDrawing(ctx, 10, 20);
    expect(ctx.beginPath).toHaveBeenCalled();
    expect(ctx.moveTo).toHaveBeenCalledWith(10, 20);
  });
});

describe('getCanvasPosition', () => {
  it('subtracts the rect offset from client coords', () => {
    expect(getCanvasPosition(150, 250, { left: 50, top: 100 })).toEqual({ x: 100, y: 150 });
  });

  it('returns zero when client coords equal the rect origin', () => {
    expect(getCanvasPosition(30, 40, { left: 30, top: 40 })).toEqual({ x: 0, y: 0 });
  });
});

describe('sendVectorIfValid', () => {
  it('calls onVector with the normalised vector when dimensions are valid', () => {
    const onVector = vi.fn();
    sendVectorIfValid({ onVector, x: 100, y: 50, canvasWidth: 200, canvasHeight: 100 });
    expect(onVector).toHaveBeenCalledWith({ x: 0.5, y: 0.5, type: 'point' });
  });

  it('does nothing when onVector is undefined', () => {
    expect(() =>
      sendVectorIfValid({ onVector: undefined, x: 100, y: 50, canvasWidth: 200, canvasHeight: 100 }),
    ).not.toThrow();
  });

  it('does not call onVector when canvasWidth is 0', () => {
    const onVector = vi.fn();
    sendVectorIfValid({ onVector, x: 100, y: 50, canvasWidth: 0, canvasHeight: 100 });
    expect(onVector).not.toHaveBeenCalled();
  });

  it('does not call onVector when canvasHeight is 0', () => {
    const onVector = vi.fn();
    sendVectorIfValid({ onVector, x: 100, y: 50, canvasWidth: 200, canvasHeight: 0 });
    expect(onVector).not.toHaveBeenCalled();
  });
});

describe('processMouseDown', () => {
  it('returns the canvas position relative to the rect', () => {
    const { position } = processMouseDown({
      clientX: 150, clientY: 250,
      canvasRect: { left: 50, top: 100 },
      canvasWidth: 400, canvasHeight: 300,
      ctx: null,
    });
    expect(position).toEqual({ x: 100, y: 150 });
  });

  it('returns initialStroke containing only the starting position', () => {
    const { initialStroke } = processMouseDown({
      clientX: 150, clientY: 250,
      canvasRect: { left: 50, top: 100 },
      canvasWidth: 400, canvasHeight: 300,
      ctx: null,
    });
    expect(initialStroke).toEqual([{ x: 100, y: 150 }]);
  });

  it('initialises drawing on the ctx when one is provided', () => {
    const ctx = makeCtx();
    processMouseDown({
      clientX: 150, clientY: 250,
      canvasRect: { left: 50, top: 100 },
      canvasWidth: 400, canvasHeight: 300,
      ctx,
    });
    expect(ctx.beginPath).toHaveBeenCalled();
    expect(ctx.moveTo).toHaveBeenCalledWith(100, 150);
  });

});

describe('processMouseMove', () => {
  it('returns the canvas position relative to the rect', () => {
    const { position } = processMouseMove({
      clientX: 200, clientY: 100,
      canvasRect: { left: 50, top: 50 },
      canvasWidth: 400, canvasHeight: 300,
      ctx: null,
    });
    expect(position).toEqual({ x: 150, y: 50 });
  });

  it('calls lineTo and stroke on the ctx when one is provided', () => {
    const ctx = makeCtx();
    processMouseMove({
      clientX: 200, clientY: 100,
      canvasRect: { left: 50, top: 50 },
      canvasWidth: 400, canvasHeight: 300,
      ctx,
    });
    expect(ctx.lineTo).toHaveBeenCalledWith(150, 50);
    expect(ctx.stroke).toHaveBeenCalled();
  });

});


describe('parseColor', () => {
  it.each([
    ['red',   { r: 255, g: 0,   b: 0   }],
    ['green', { r: 0,   g: 255, b: 0   }],
    ['blue',  { r: 0,   g: 0,   b: 255 }],
    ['white', { r: 255, g: 255, b: 255 }],
    ['black', { r: 0,   g: 0,   b: 0   }],
  ] as const)('parses named color "%s"', (color, expected) => {
    expect(parseColor(color)).toEqual(expected);
  });

  it('parses rgb() format', () => {
    expect(parseColor('rgb(10, 20, 30)')).toEqual({ r: 10, g: 20, b: 30 });
  });

  it('parses a 6-character hex color', () => {
    expect(parseColor('#ff8800')).toEqual({ r: 255, g: 136, b: 0 });
  });

  it('parses a 3-character hex color by doubling each digit', () => {
    expect(parseColor('#f80')).toEqual({ r: 255, g: 136, b: 0 });
  });

  it('trims whitespace before parsing', () => {
    expect(parseColor('  red  ')).toEqual({ r: 255, g: 0, b: 0 });
  });

  it('falls back to white for an unrecognised color string', () => {
    expect(parseColor('not-a-color')).toEqual({ r: 255, g: 255, b: 255 });
  });
});

describe('strokeToAnnotation', () => {
  it('maps stroke points to [x, y] tuples in Vector', () => {
    const stroke = makeStroke({ points: [{ x: 0.1, y: 0.2 }, { x: 0.3, y: 0.4 }] });
    expect(strokeToAnnotation(stroke,'permanent').Vector).toEqual([[0.1, 0.2], [0.3, 0.4]]);
  });

  it('encodes the stroke color as an [r, g, b] tuple in Color', () => {
    expect(strokeToAnnotation(makeStroke({ color: 'blue' }), 'permanent').Color).toEqual([0, 0, 255]);
  });

  it('encodes a hex color correctly', () => {
    expect(strokeToAnnotation(makeStroke({ color: '#ff8800' }), 'permanent').Color).toEqual([255, 136, 0]);
  });
  
  it('includes FadeDuration when strokeType is fading', () => {
    const result = strokeToAnnotation(makeStroke(), 'fading');
    expect(result.StrokeType).toBe('fading');
    expect(result.FadeDuration).toBe(2000);
  });

  it('accepts a custom fadeDuration when strokeType is fading', () => {
    const result = strokeToAnnotation(makeStroke(), 'fading', 5000);
    expect(result.FadeDuration).toBe(5000);
  });
});

describe('finaliseStroke', () => {
  it.each([
    ['isDrawing is false',        { isDrawing: false, currentStroke: [{ x: 10, y: 20 }], onStroke: vi.fn() }],
    ['currentStroke is empty',    { isDrawing: true,  currentStroke: [],                 onStroke: vi.fn() }],
    ['onStroke is undefined',     { isDrawing: true,  currentStroke: [{ x: 10, y: 20 }], onStroke: undefined }],
  ])('returns null when %s', (_label, extra) => {
    expect(finaliseStroke({ canvasWidth: 400, canvasHeight: 300, ...extra })).toBeNull();
  });

  it('normalises stroke points relative to canvas dimensions', () => {
    const onStroke = vi.fn();
    const result = finaliseStroke({
      isDrawing: true,
      currentStroke: [{ x: 200, y: 150 }],
      canvasWidth: 400, canvasHeight: 300,
      onStroke,
    });
    expect(result!.points).toEqual([{ x: 0.5, y: 0.5 }]);
  });

  it('calls onStroke with the correct color and width', () => {
    const onStroke = vi.fn();
    finaliseStroke({
      isDrawing: true,
      currentStroke: [{ x: 100, y: 100 }],
      canvasWidth: 400, canvasHeight: 300,
      onStroke,
    });
    expect(onStroke).toHaveBeenCalledOnce();
    expect(onStroke).toHaveBeenCalledWith(expect.objectContaining({ color: 'red', width: 8 }));
  });

  it('returns the same stroke object passed to onStroke', () => {
    const onStroke = vi.fn();
    const result = finaliseStroke({
      isDrawing: true,
      currentStroke: [{ x: 100, y: 100 }],
      canvasWidth: 400, canvasHeight: 300,
      onStroke,
    });
    expect(result).toBe(onStroke.mock.calls[0][0]);
  });

  it('does not call onStroke when returning null', () => {
    const onStroke = vi.fn();
    finaliseStroke({
      isDrawing: false,
      currentStroke: [{ x: 100, y: 100 }],
      canvasWidth: 400, canvasHeight: 300,
      onStroke,
    });
    expect(onStroke).not.toHaveBeenCalled();
  });
});

describe('makeClearAnnotations', () => {
  it('returns an object with Type set to clear-annotations', () => {
    expect(makeClearAnnotations()).toEqual({ Type: 'clear-annotations' });
  });
});
