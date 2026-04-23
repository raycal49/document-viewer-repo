import { describe, it, expect, beforeEach } from 'vitest';
import { annotationLogger } from '../scenes/videoCallPage/AnnotationLogger';

beforeEach(() => {
  annotationLogger.endCall();
  annotationLogger.clear();
});

describe('startNewCall', () => {
  it('clears previous logs and starts fresh with one system entry', () => {
    annotationLogger.startNewCall('call-1');
    annotationLogger.logAnnotationCleared();
    annotationLogger.startNewCall('call-2');
    expect(annotationLogger.getLogs()).toHaveLength(1);
  });
});

describe('endCall', () => {
  it('adds a system log when a call is active', () => {
    annotationLogger.startNewCall('call-1');
    annotationLogger.endCall();
    expect(annotationLogger.getLogs('system')).toHaveLength(2);
  });

  it('does nothing when no call is active', () => {
    annotationLogger.endCall();
    expect(annotationLogger.getLogs()).toHaveLength(0);
  });
});

describe('logAnnotationSent', () => {
  it('formats the message with type, color, and point count and attaches data', () => {
    annotationLogger.logAnnotationSent({ type: 'stroke', color: 'red', pointCount: 5 });
    const [entry] = annotationLogger.getLogs('annotation');
    expect(entry.message).toContain('stroke');
    expect(entry.message).toContain('red');
    expect(entry.message).toContain('5');
    expect(entry.data).toBeDefined();
  });
});

describe('logWebRTCEvent', () => {
  it('uses the lookup message for a known event', () => {
    annotationLogger.logWebRTCEvent({ event: 'ice-candidate' });
    expect(annotationLogger.getLogs('webrtc')[0].message).toBe('ICE candidate generated');
  });

  it('includes iceState in the message when provided', () => {
    annotationLogger.logWebRTCEvent({ event: 'ice-state-change', iceState: 'connected' });
    expect(annotationLogger.getLogs('webrtc')[0].message).toContain('connected');
  });

  it('defaults iceState to unknown when absent', () => {
    annotationLogger.logWebRTCEvent({ event: 'ice-state-change' });
    expect(annotationLogger.getLogs('webrtc')[0].message).toContain('unknown');
  });

  it('falls back to a generic message for an unrecognised event', () => {
    annotationLogger.logWebRTCEvent({ event: 'custom-event' });
    expect(annotationLogger.getLogs('webrtc')[0].message).toContain('custom-event');
  });
});

describe('getLogs', () => {
  it('returns all logs across categories when no filter is given', () => {
    annotationLogger.startNewCall('c1');
    annotationLogger.logAnnotationCleared();
    annotationLogger.logConnectionEvent('connected');
    annotationLogger.logSystemEvent('boot');
    expect(annotationLogger.getLogs()).toHaveLength(4);
  });

  it('returns only matching logs when a category is given', () => {
    annotationLogger.startNewCall('c1');
    annotationLogger.logAnnotationCleared();
    expect(annotationLogger.getLogs('annotation')).toHaveLength(1);
  });
});

describe('getFormattedLogs', () => {
  it('formats each entry as HH:MM:SS [CATEGORY] message', () => {
    annotationLogger.logAnnotationCleared();
    const [formatted] = annotationLogger.getFormattedLogs();
    expect(formatted).toMatch(/^\d{2}:\d{2}:\d{2} \[ANNOTATION\]/);
  });

  it('filters by category when provided', () => {
    annotationLogger.startNewCall('c1');
    annotationLogger.logAnnotationCleared();
    expect(annotationLogger.getFormattedLogs('annotation')).toHaveLength(1);
  });
});

describe('clear', () => {
  it('empties all logs', () => {
    annotationLogger.logAnnotationCleared();
    annotationLogger.clear();
    expect(annotationLogger.getLogs()).toHaveLength(0);
  });
});

describe('max log cap', () => {
  it('trims the log list to 500 when exceeded', () => {
    for (let i = 0; i < 501; i++) annotationLogger.logAnnotationCleared();
    expect(annotationLogger.getLogs()).toHaveLength(500);
  });
});

describe('exportToJSON', () => {
  it('includes callId, logCount, and logs array', () => {
    annotationLogger.startNewCall('call-export');
    const parsed = JSON.parse(annotationLogger.exportToJSON());
    expect(parsed.callId).toBe('call-export');
    expect(parsed.logCount).toBe(1);
    expect(parsed.logs).toHaveLength(1);
  });
});

describe('exportByCategoryToJSON', () => {
  it('contains only logs matching the requested category', () => {
    annotationLogger.startNewCall('c1');
    annotationLogger.logAnnotationCleared();
    const parsed = JSON.parse(annotationLogger.exportByCategoryToJSON('annotation'));
    expect(parsed.category).toBe('annotation');
    expect(parsed.logs).toHaveLength(1);
  });
});

describe('exportAnnotationsToJSON', () => {
  beforeEach(() => {
    annotationLogger.startNewCall('call-export');
    annotationLogger.logAnnotationSent({ type: 'stroke', color: 'red', pointCount: 5 });
  });

  it('includes both user IDs when provided', () => {
    const result = JSON.parse(annotationLogger.exportAnnotationsToJSON('user-1', 'quest-1'));
    expect(result.WebAppUserID).toBe('user-1');
    expect(result.QuestUserID).toBe('quest-1');
  });

  it('omits both user IDs when not provided', () => {
    const result = JSON.parse(annotationLogger.exportAnnotationsToJSON());
    expect(result).not.toHaveProperty('WebAppUserID');
    expect(result).not.toHaveProperty('QuestUserID');
  });

  it('includes only WebAppUserID when questUserId is omitted', () => {
    const result = JSON.parse(annotationLogger.exportAnnotationsToJSON('user-1'));
    expect(result.WebAppUserID).toBe('user-1');
    expect(result).not.toHaveProperty('QuestUserID');
  });

  it('includes only QuestUserID when webAppUserId is omitted', () => {
    const result = JSON.parse(annotationLogger.exportAnnotationsToJSON(undefined, 'quest-1'));
    expect(result).not.toHaveProperty('WebAppUserID');
    expect(result.QuestUserID).toBe('quest-1');
  });

  it('only includes annotation logs and strips the message field', () => {
    annotationLogger.logConnectionEvent('connected'); // should not appear
    const result = JSON.parse(annotationLogger.exportAnnotationsToJSON());
    expect(result.logs.every((l: { category: string }) => l.category === 'annotation')).toBe(true);
    expect(result.logs[0]).not.toHaveProperty('message');
  });
});
