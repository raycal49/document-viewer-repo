import { describe, it, expect, vi, afterEach } from 'vitest';
import { routeSignalingMessage } from '../../webrtc/signaling/signalingMessageHandlers.ts';
import type { SignalingMessage } from '../../webrtc/BaseInterfaces.ts';
import { 
  makeMessageCtx, 
  runIncomingCallsUpdater,
  makeWebrtcRef,
  makeSignalingRef
 } from './webrtcUtils/WebrtcFactories.ts';

function msg(type: string, extra: Partial<SignalingMessage> = {}): SignalingMessage {
  return { type, ...extra } as SignalingMessage;
}

afterEach(() => vi.useRealTimers());

describe('routeSignalingMessage', () => {
  it('does nothing for an unknown message type', () => {
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('totally-unknown'), ctx);
    expect(ctx.setIncomingCalls).not.toHaveBeenCalled();
    expect(ctx.setStatus).not.toHaveBeenCalled();
    expect(ctx.addLog).not.toHaveBeenCalled();
  });
});

describe('handleCallRequest', () => {
  it('does nothing when callerName is missing', () => {
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('call-request', { room: 'r1' }), ctx);
    expect(ctx.setIncomingCalls).not.toHaveBeenCalled();
  });

  it('returns prev unchanged when the room already exists', () => {
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('call-request', { room: 'r1', callerName: 'Alice' }), ctx);
    const existing = [{ room: 'r1', callerName: 'Alice', claimed: false }];
    expect(runIncomingCallsUpdater(ctx, existing)).toBe(existing);
  });

  it('appends the new call when the room is not yet in the list', () => {
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('call-request', { room: 'r1', callerName: 'Alice' }), ctx);
    const result = runIncomingCallsUpdater(ctx, []);
    expect(result).toEqual([{ room: 'r1', callerName: 'Alice', claimed: false }]);
  });
});

describe('handleCallClaimed', () => {
  it('does nothing when the claimed room is the currently active room', () => {
    const ctx = makeMessageCtx({ activeRoomRef: { current: 'r1' } });
    routeSignalingMessage(msg('call-claimed', { room: 'r1' }), ctx);
    expect(ctx.setIncomingCalls).not.toHaveBeenCalled();
  });

  it('leaves other calls unchanged', () => {
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('call-claimed', { room: 'r1' }), ctx);
    const prev = [
      { room: 'r1', callerName: 'Alice', claimed: false },
      { room: 'r2', callerName: 'Bob', claimed: false },
    ];
    const result = runIncomingCallsUpdater(ctx, prev);
    expect(result[1].claimed).toBe(false);
  });

  it('removes the call after the delay', () => {
    vi.useFakeTimers();
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('call-claimed', { room: 'r1' }), ctx);
    vi.runAllTimers();
    const prev = [{ room: 'r1', callerName: 'Alice', claimed: true }];
    const result = runIncomingCallsUpdater(ctx, prev, 1);
    expect(result).toEqual([]);
  });
});

describe('handleCallEnded', () => {
  it('removes the ended call from the list', () => {
    const ctx = makeMessageCtx();
    routeSignalingMessage(msg('call-ended', { room: 'r1' }), ctx);
    const prev = [
      { room: 'r1', callerName: 'Alice', claimed: false },
      { room: 'r2', callerName: 'Bob', claimed: false },
    ];
    expect(runIncomingCallsUpdater(ctx, prev)).toEqual([
      { room: 'r2', callerName: 'Bob', claimed: false },
    ]);
  });

  it('cleans up and sets status when the active room ends', () => {
    const cleanup = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ cleanup }),
    });
    routeSignalingMessage(msg('call-ended', { room: 'r1' }), ctx);
    expect(cleanup).toHaveBeenCalled();
    expect(ctx.activeRoomRef.current).toBeNull();
    expect(ctx.setStatus).toHaveBeenCalledWith('call-ended');
  });

  it('resets dataChannelReady when the active room ends', () => {
    const setDataChannelReady = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      setDataChannelReady,
    });
    routeSignalingMessage(msg('call-ended', { room: 'r1' }), ctx);
    expect(setDataChannelReady).toHaveBeenCalledWith(false);
  });

  it('does not reset dataChannelReady when a different room ends', () => {
    const setDataChannelReady = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r2' },
      setDataChannelReady,
    });
    routeSignalingMessage(msg('call-ended', { room: 'r1' }), ctx);
    expect(setDataChannelReady).not.toHaveBeenCalled();
  });
});

describe('handleCallAlreadyClaimed', () => {
  it('does nothing when a different room is claimed', () => {
    const cleanup = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r2' },
      webrtcRef: makeWebrtcRef({ cleanup }),
    });
    routeSignalingMessage(msg('call-already-claimed', { room: 'r1' }), ctx);
    expect(cleanup).not.toHaveBeenCalled();
    expect(ctx.setStatus).not.toHaveBeenCalled();
  });

  it('cleans up, sets error, and logs when the active room is claimed', () => {
    const cleanup = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ cleanup }),
    });
    routeSignalingMessage(msg('call-already-claimed', { room: 'r1' }), ctx);
    expect(cleanup).toHaveBeenCalled();
    expect(ctx.activeRoomRef.current).toBeNull();
    expect(ctx.setStatus).toHaveBeenCalledWith('error');
    expect(ctx.addLog).toHaveBeenCalledWith('Call was taken by someone else');
  });
});

describe('handleOffer', () => {
  it('ignores an offer with no sdp', () => {
    const handleOffer = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ handleOffer }),
    });
    routeSignalingMessage(msg('offer', { room: 'r1' }), ctx);
    expect(handleOffer).not.toHaveBeenCalled();
  });

  it('sends the answer and logs on successful offer handling', async () => {
    const sendToGroup = vi.fn();
    const handleOffer = vi.fn().mockResolvedValue('answer-sdp');
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ handleOffer }),
      signalingRef: makeSignalingRef({ sendToGroup }),
    });
    routeSignalingMessage(msg('offer', { room: 'r1', sdp: 'offer-sdp' }), ctx);
    await vi.waitFor(() => expect(sendToGroup).toHaveBeenCalled());
    expect(sendToGroup).toHaveBeenCalledWith('r1', {
      type: 'answer',
      room: 'r1',
      sdp: 'answer-sdp',
    });
    expect(ctx.addLog).toHaveBeenCalledWith('Answer sent');
  });

  it('logs the error and sets status to error when offer handling fails', async () => {
    const handleOffer = vi.fn().mockRejectedValue(new Error('peer gone'));
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ handleOffer }),
    });
    routeSignalingMessage(msg('offer', { room: 'r1', sdp: 'offer-sdp' }), ctx);
    await vi.waitFor(() => expect(ctx.setStatus).toHaveBeenCalled());
    expect(ctx.setStatus).toHaveBeenCalledWith('error');
    expect(ctx.addLog).toHaveBeenCalledWith(expect.stringContaining('peer gone'));
  });
});

describe('handleIceCandidate', () => {
  it('ignores a message with no candidate', () => {
    const addIceCandidate = vi.fn();
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ addIceCandidate }),
    });
    routeSignalingMessage(msg('ice-candidate', { room: 'r1' }), ctx);
    expect(addIceCandidate).not.toHaveBeenCalled();
  });

  it('logs the error when addIceCandidate rejects', async () => {
    const addIceCandidate = vi.fn().mockRejectedValue(new Error('ice fail'));
    const ctx = makeMessageCtx({
      activeRoomRef: { current: 'r1' },
      webrtcRef: makeWebrtcRef({ addIceCandidate }),
    });
    routeSignalingMessage(
      msg('ice-candidate', { room: 'r1', candidate: {} as RTCIceCandidateInit }),
      ctx,
    );
    await vi.waitFor(() => expect(ctx.addLog).toHaveBeenCalled());
    expect(ctx.addLog).toHaveBeenCalledWith(expect.stringContaining('ice fail'));
  });
});