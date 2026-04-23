import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { makePeerCallbacks } from '../../webrtc/signaling/peerCallbacks.ts';
import { makePeerCallbackOptions } from './webrtcUtils/WebrtcFactories.ts';

beforeEach(() => vi.useFakeTimers());
afterEach(() => vi.useRealTimers());

describe('onTrack', () => {
  it('logs the track kind', () => {
    const opts = makePeerCallbackOptions();
    const { onTrack } = makePeerCallbacks(opts);
    onTrack({ track: { kind: 'video' }, streams: [] } as unknown as RTCTrackEvent);
    expect(opts.addLog).toHaveBeenCalledWith(expect.stringContaining('video'));
  });

  it('calls attachStream with the track event', () => {
    const opts = makePeerCallbackOptions();
    const { onTrack } = makePeerCallbacks(opts);
    const event = { track: { kind: 'audio' }, streams: [] } as unknown as RTCTrackEvent;
    onTrack(event);
    expect(opts.attachStream).toHaveBeenCalledWith(event);
  });

  it('sets status to streaming', () => {
    const opts = makePeerCallbackOptions();
    const { onTrack } = makePeerCallbacks(opts);
    onTrack({ track: { kind: 'video' }, streams: [] } as unknown as RTCTrackEvent);
    expect(opts.setStatus).toHaveBeenCalledWith('streaming');
  });
});

describe('onIceCandidate', () => {
  it('sends ice-candidate to the group when signaling is available', () => {
    const opts = makePeerCallbackOptions();
    const { onIceCandidate } = makePeerCallbacks(opts);
    const candidate = {
      candidate: 'cand1',
      sdpMid: 'audio',
      sdpMLineIndex: 0,
    } as RTCIceCandidate;

    onIceCandidate(candidate);

    expect(opts.signaling.sendToGroup).toHaveBeenCalledWith('room1', {
      type: 'ice-candidate',
      room: 'room1',
      candidate: {
        candidate: 'cand1',
        sdpMid: 'audio',
        sdpMLineIndex: 0,
      },
    });
  });

  it('does nothing when signaling is null', () => {
    const opts = makePeerCallbackOptions({ signaling: null as unknown as typeof opts.signaling });
    const { onIceCandidate } = makePeerCallbacks(opts);
    expect(() =>
      onIceCandidate({ candidate: 'cand1', sdpMid: '0', sdpMLineIndex: 0 } as RTCIceCandidate),
    ).not.toThrow();
  });
});

describe('onStateChange', () => {
  it('logs the ICE connection state', () => {
    const opts = makePeerCallbackOptions();
    const { onStateChange } = makePeerCallbacks(opts);
    onStateChange('connected');
    expect(opts.addLog).toHaveBeenCalledWith(expect.stringContaining('connected'));
  });

  it('nullifies activeRoomRef and sets error on disconnected', () => {
    const opts = makePeerCallbackOptions();
    const { onStateChange } = makePeerCallbacks(opts);
    onStateChange('disconnected');
    vi.advanceTimersByTime(5000);
    expect(opts.onFailed).toHaveBeenCalled();
    expect(opts.setStatus).toHaveBeenCalledWith('error');
  });

  it('nullifies activeRoomRef and sets error on failed', () => {
    const opts = makePeerCallbackOptions();
    const { onStateChange } = makePeerCallbacks(opts);
    onStateChange('failed');
    expect(opts.onFailed).toHaveBeenCalled();
    expect(opts.setStatus).toHaveBeenCalledWith('error');
  });

  it('does not set error or nullify ref for non-terminal states', () => {
    const opts = makePeerCallbackOptions();
    const { onStateChange } = makePeerCallbacks(opts);
    onStateChange('connected');
    expect(opts.setStatus).not.toHaveBeenCalled();
    expect(opts.onFailed).not.toHaveBeenCalled();
  });

  it('does not set error or nullify ref for checking state', () => {
    const opts = makePeerCallbackOptions();
    const { onStateChange } = makePeerCallbacks(opts);
    onStateChange('checking');
    expect(opts.setStatus).not.toHaveBeenCalled();
    expect(opts.onFailed).not.toHaveBeenCalled();
  });
});

describe('onMicError', () => {
  it('logs the mic error message', () => {
    const opts = makePeerCallbackOptions();
    const callbacks = makePeerCallbacks(opts);
    (callbacks as unknown as { onMicError: (err: Error) => void }).onMicError(new Error('mic failed'));
    expect(opts.addLog).toHaveBeenCalledWith(expect.stringContaining('mic failed'));
  });
});

describe('onDataChannelOpen', () => {
  it('is passed through when provided in options', () => {
    const onDataChannelOpen = vi.fn();
    const opts = makePeerCallbackOptions({ onDataChannelOpen });
    const callbacks = makePeerCallbacks(opts);
    expect(callbacks.onDataChannelOpen).toBe(onDataChannelOpen);
  });

  it('is undefined when not provided in options', () => {
    const opts = makePeerCallbackOptions();
    const callbacks = makePeerCallbacks(opts);
    expect(callbacks.onDataChannelOpen).toBeUndefined();
  });
});