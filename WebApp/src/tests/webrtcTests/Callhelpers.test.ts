import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { runConnect, runAcceptCall } from '../../webrtc/signaling/callHelpers.ts';
import type { IncomingCall } from '../../webrtc/BaseInterfaces.ts';
import { mergeIncomingCalls } from '../../webrtc/signaling/callHelpers.ts';
import { makeConnectDeps, makeAcceptDeps, makeWebrtcRef } from './webrtcUtils/WebrtcFactories.ts';
import { mockSignalingInstance, lastCallbacks, resetSignalingMocks} from './webrtcUtils/SignaligServiceMock.ts';
import type { PeerCallbacksOptions } from '../../webrtc/ExtendedInterfaces.ts';
import { SignalingService } from '../../webrtc/services/signalingService.ts';

vi.mock('../../webrtc/services/signalingService', () => 
  import('./webrtcUtils/SignaligServiceMock.ts'),
);

vi.mock('../../webrtc/signaling/peerCallbacks', () => ({
  makePeerCallbacks: vi.fn().mockImplementation((opts: PeerCallbacksOptions) => {
    capturedPeerCallbackOptions = opts;
    return { onTrack: vi.fn() };
  }),
}));

let capturedPeerCallbackOptions: PeerCallbacksOptions | Record<string, never> = {};


describe('runConnect', () => {
  beforeEach(() => {
    resetSignalingMocks();
  });

  afterEach(() => vi.useRealTimers());

  it('sets status to disconnected when signaling disconnects', async () => {
  const deps = makeConnectDeps();
  await runConnect(deps);
  lastCallbacks.onDisconnected?.();
  expect(deps.setStatus).toHaveBeenCalledWith('disconnected');
  expect(deps.addLog).toHaveBeenCalledWith(expect.stringContaining('WS closed'));
  });

  it('does not set status to disconnected when the disconnect is intentional', async () => {
    const intentionalEndRef = { current: true };
    const deps = makeConnectDeps({ intentionalEndRef });
    await runConnect(deps);
    lastCallbacks.onDisconnected?.();
    expect(deps.setStatus).not.toHaveBeenCalledWith('disconnected');
  });
  it('sets status to error when signaling reports an error', async () => {
    const deps = makeConnectDeps();
    await runConnect(deps);
    lastCallbacks.onError?.('something broke');
    expect(deps.setStatus).toHaveBeenCalledWith('error');
    expect(deps.addLog).toHaveBeenCalledWith(expect.stringContaining('something broke'));
  });

  it('populates incoming calls when the server returns active calls', async () => {
    vi.mocked(mockSignalingInstance.fetchActiveCalls).mockResolvedValue([
      { room: 'r1', callerName: 'Alice' },
    ]);
    const deps = makeConnectDeps();
    await runConnect(deps);
    expect(deps.setIncomingCalls).toHaveBeenCalledWith([
      { room: 'r1', callerName: 'Alice', claimed: false },
    ]);
    expect(deps.addLog).toHaveBeenCalledWith(expect.stringContaining('1 active call'));
  });

  it('sets status to error and logs when connect() throws', async () => {
    vi.mocked(mockSignalingInstance.connect).mockRejectedValue(new Error('timeout'));
    const deps = makeConnectDeps();
    await runConnect(deps);
    expect(deps.setStatus).toHaveBeenCalledWith('error');
    expect(deps.addLog).toHaveBeenCalledWith(expect.stringContaining('timeout'));
  });

  it('rejoins the active room when reconnected', async () => {
    const deps = makeConnectDeps({ activeRoomRef: { current: 'room-1' } });
    await runConnect(deps);
    lastCallbacks.onReconnected?.();
    expect(mockSignalingInstance.joinGroup).toHaveBeenCalledWith('room-1');
  });

  it('does not join a group when reconnected with no active room', async () => {
    const deps = makeConnectDeps({ activeRoomRef: { current: null } });
    await runConnect(deps);
    lastCallbacks.onReconnected?.();
    expect(mockSignalingInstance.joinGroup).not.toHaveBeenCalled();
  });

  it('logs when initMic reports a mic error', async () => {
    const mockInitMic = vi.fn().mockImplementation(async (cb?: (err: Error) => void) => cb?.(new Error('no mic')));
    const deps = makeConnectDeps({ webrtcRef: makeWebrtcRef({ initMic: mockInitMic }) });
    await runConnect(deps);
    expect(deps.addLog).toHaveBeenCalledWith(expect.stringContaining('no mic'));
  });
});

describe('runAcceptCall', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    resetSignalingMocks();
  });

  afterEach(() => vi.useRealTimers());

  it('sends call-accepted, sets status to connecting, and logs after the delay', async () => {
    const deps = makeAcceptDeps({
      signalingRef: { current: mockSignalingInstance as unknown as SignalingService },
    });
    await runAcceptCall(deps);
    vi.runAllTimers();
    
    expect(mockSignalingInstance.sendToGroup).toHaveBeenCalledWith('r1', {
      type: 'call-accepted',
      room: 'r1',
    });
    expect(deps.setStatus).toHaveBeenCalledWith('connecting');
    expect(deps.addLog).toHaveBeenCalledWith(expect.stringContaining('r1'));
  });

  it('clears activeRoomRef when onFailed is called', () => {
  const deps = makeAcceptDeps({ activeRoomRef: { current: 'r1' }});

  runAcceptCall(deps);
  capturedPeerCallbackOptions.onFailed();
  expect(deps.activeRoomRef.current).toBeNull();
});

  it('logs when ICE config refresh fails', async () => {
    vi.mocked(mockSignalingInstance.fetchIceConfig).mockRejectedValueOnce(new Error('Network error'));
    const deps = makeAcceptDeps({
      signalingRef: { current: mockSignalingInstance as unknown as SignalingService },
    });
    await runAcceptCall(deps);
    expect(deps.addLog).toHaveBeenCalledWith('ICE config refresh failed, using cached credentials');
  });

  it('does not throw when intentionalEndRef is absent', async () => {
    const deps = makeAcceptDeps({
      signalingRef: { current: mockSignalingInstance as unknown as SignalingService },
      intentionalEndRef: undefined,
    });
    await runAcceptCall(deps);
    vi.runAllTimers();
  });

  it('handles fetchIceConfig returning null', async () => {
    vi.mocked(mockSignalingInstance.fetchIceConfig).mockResolvedValueOnce(null);
    const deps = makeAcceptDeps({
      signalingRef: { current: mockSignalingInstance as unknown as SignalingService },
    });
    await runAcceptCall(deps);
    vi.runAllTimers();
    // Verify no refresh log is made
    expect(deps.addLog).not.toHaveBeenCalledWith(expect.stringContaining('Refreshed'));
    // Verify the rest of the function still executes normally
    expect(mockSignalingInstance.sendToGroup).toHaveBeenCalled();
  });
});

describe('mergeIncomingCalls', () => {

  it('returns an empty array when both lists are empty', () => {
    expect(mergeIncomingCalls([], [])).toEqual([]);
  });

  it('does not mutate the original prev array', () => {
    const prev: IncomingCall[] = [{ room: 'r1', callerName: 'Alice', claimed: false }];
    mergeIncomingCalls(prev, [{ room: 'r2', callerName: 'Bob' }]);
    expect(prev).toHaveLength(1);
  });
  
});
