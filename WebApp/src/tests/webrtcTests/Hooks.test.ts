import { describe, it, vi, expect, beforeEach, afterEach } from 'vitest';
import { makeHookDeps } from './webrtcUtils/WebrtcFactories.ts';
import { renderHook, act } from '@testing-library/react';
import {
  useAddLog,
  useAttachStream,
  useHandleMessage,
  useConnect,
  useAcceptCall,
  useDeclineCall,
  useDisconnect,
} from '../../webrtc/hooks/webrtcHooks.ts';
import type { SignalingMessage } from '../../webrtc/BaseInterfaces.ts';

vi.mock('../../webrtc/signaling/signalingMessageHandlers', () => ({
  routeSignalingMessage: vi.fn(),
}));

vi.mock('../../webrtc/signaling/callHelpers', () => ({
  runConnect: vi.fn().mockResolvedValue(undefined),
  runAcceptCall: vi.fn().mockResolvedValue(undefined),
}));
import { routeSignalingMessage } from '../../webrtc/signaling/signalingMessageHandlers.ts';
import { runConnect, runAcceptCall } from '../../webrtc/signaling/callHelpers.ts';

beforeEach(() => {
  vi.mocked(routeSignalingMessage).mockClear();
  vi.mocked(runConnect).mockClear();
  vi.mocked(runAcceptCall).mockClear();
});

afterEach(() => vi.useRealTimers());

describe('useAddLog', () => {
  it('prepends a timestamped message to the front of the log list', () => {
    const setLogs = vi.fn();
    const { result } = renderHook(() => useAddLog(setLogs));

    act(() => result.current('connected'));

    const updater = vi.mocked(setLogs).mock.calls[0][0] as (prev: string[]) => string[];
    const updated = updater(['old']);
    expect(updated[0]).toMatch(/\[.*\] connected/);
    expect(updated[1]).toBe('old');
  });
});

describe('useAttachStream', () => {
  it('does nothing when videoRef.current is null', () => {
    const videoRef = { current: null };
    const { result } = renderHook(() => useAttachStream(videoRef));
    act(() => {
      result.current({ streams: [], track: {} } as unknown as RTCTrackEvent);
    });
  });

  it('sets srcObject to the first stream when one is present', async () => {
    const play = vi.fn().mockResolvedValue(undefined);
    const el = { srcObject: null as null, play };
    const { result } = renderHook(() => useAttachStream({ current: el as unknown as HTMLVideoElement}));
    const stream = { id: 'stream1' } as unknown as MediaStream;

    await act(async () =>
      result.current({ streams: [stream], track: {} } as unknown as RTCTrackEvent),
    );

    expect(el.srcObject).toBe(stream);
    expect(play).toHaveBeenCalled();
  });

  it('creates a new MediaStream and adds the track when srcObject is null', async() => {
    const addTrack = vi.fn();
    const fakeStream = { addTrack };
    const play = vi.fn().mockResolvedValue(undefined);
    const el = { srcObject: null as MediaStream | null, play };

    const OriginalMS = globalThis.MediaStream;
    globalThis.MediaStream = vi.fn().mockImplementation(function () {
      return fakeStream;
    }) as unknown as typeof MediaStream;

    const { result } = renderHook(() => useAttachStream({ current: el as unknown as HTMLVideoElement}));
    const track = { id: 't1' } as MediaStreamTrack;

    await act(async () =>
      result.current({ streams: [], track } as unknown as RTCTrackEvent),
    );

    expect(addTrack).toHaveBeenCalledWith(track);
    globalThis.MediaStream = OriginalMS;
  });

  it('silences AbortError from play()', async () => {
    const abortError = Object.assign(new Error('aborted'), { name: 'AbortError' });
    const play = vi.fn().mockRejectedValue(abortError);
    const el = { srcObject: null as MediaStream | null, play };
    const { result } = renderHook(() => useAttachStream({ current: el as unknown as HTMLVideoElement}));

    await expect(
      result.current({ streams: [{}], track: {} } as unknown as RTCTrackEvent)
    ).resolves.not.toThrow();
  });

  it('rethrows non-AbortError from play()', async () => {
    const notAllowed = Object.assign(new Error('not allowed'), { name: 'NotAllowedError' });
    const play = vi.fn().mockRejectedValue(notAllowed);
    const el = { srcObject: null as MediaStream | null, play };
    const { result } = renderHook(() => useAttachStream({ current: el as unknown as HTMLVideoElement}));

    let caught: unknown;
        
    try {
      await act(() =>
        result.current({ streams: [{}], track: {} } as unknown as RTCTrackEvent),
      );
    } catch (err) {
      caught = err;
    }
    expect(caught).toBe(notAllowed);
  });
});

describe('useHandleMessage', () => {
  function makeDeps() {
    return {
      addLog: vi.fn(),
      setStatus: vi.fn(),
      setIncomingCalls: vi.fn(),
      activeRoomRef: { current: null as string | null },
      webrtcRef: { current: {} },
      signalingRef: { current: null },
    };
  }

  it('omits the room bracket when room is not present', () => {
    const deps = makeDeps();
    const { result } = renderHook(() => useHandleMessage(deps as unknown as Parameters<typeof useHandleMessage>[0]));
    act(() => result.current({ type: 'call-request' } as SignalingMessage));
    const logArg: string = vi.mocked(deps.addLog).mock.calls[0][0];
    expect(logArg).not.toContain('[');
  });

  it('calls routeSignalingMessage with the message and context', () => {
    const deps = makeDeps();
    const { result } = renderHook(() => useHandleMessage(deps as unknown as Parameters<typeof useHandleMessage>[0]));
    const message = { type: 'call-ended', room: 'r1' } as SignalingMessage;
    act(() => result.current(message));
    expect(vi.mocked(routeSignalingMessage)).toHaveBeenCalledWith(message, expect.objectContaining({
      addLog: deps.addLog,
      setStatus: deps.setStatus,
    }));
  });

  it('passes a callable setDataChannelReady no-op in the message context', () => {
    const deps = makeDeps();
    const { result } = renderHook(() => useHandleMessage(deps as unknown as Parameters<typeof useHandleMessage>[0]));
    act(() => result.current({ type: 'call-ended', room: 'r1' } as SignalingMessage));
    const ctx = vi.mocked(routeSignalingMessage).mock.calls[0][1];
    expect(() => ctx.setDataChannelReady(false)).not.toThrow();
  });
});

describe('useConnect', () => {
  it('calls runConnect with the provided dependencies', async () => {
    const deps = makeHookDeps();
    const { result } = renderHook(() => useConnect(deps as unknown as Parameters<typeof useConnect>[0]));
    await act(async () => result.current());
    expect(vi.mocked(runConnect)).toHaveBeenCalledWith(expect.objectContaining({
      addLog: deps.addLog,
      userIdRef: deps.userIdRef,
    }));
  });
});

describe('useAcceptCall', () => {
  function makeDeps(activeRoom: string | null = null) {
    return {
      addLog: vi.fn(), setStatus: vi.fn(), attachStream: vi.fn(),
      signalingRef: { current: null }, 
      webrtcRef: { current: {} },
      iceServersRef: { current: [] },
      activeRoomRef: { current: activeRoom },
    };
  }

  it('logs and does not accept when already in a call', () => {
    const deps = makeDeps('r-existing');
    const { result } = renderHook(() => useAcceptCall(deps as unknown as Parameters<typeof useAcceptCall>[0]));
    act(() => result.current('r1'));
    expect(deps.addLog).toHaveBeenCalledWith('Already in a call');
    expect(vi.mocked(runAcceptCall)).not.toHaveBeenCalled();
  });

  it('sets activeRoomRef and calls runAcceptCall when not already in a call', () => {
    const deps = makeDeps(null);
    const { result } = renderHook(() => useAcceptCall(deps as unknown as Parameters<typeof useAcceptCall>[0]));
    act(() => result.current('r1'));
    expect(deps.activeRoomRef.current).toBe('r1');
    expect(vi.mocked(runAcceptCall)).toHaveBeenCalled();
  });

  it('handles runAcceptCall rejection by logging error and resetting state', async () => {
    vi.mocked(runAcceptCall).mockRejectedValueOnce(new Error('Test error'));
    const deps = makeDeps(null);
    const { result } = renderHook(() => useAcceptCall(deps as unknown as Parameters<typeof useAcceptCall>[0]));
    act(() => result.current('r1'));
    await act(async () => {});
    expect(deps.addLog).toHaveBeenCalledWith('Accept call failed: Test error');
    expect(deps.activeRoomRef.current).toBeNull();
    expect(deps.setStatus).toHaveBeenCalledWith('error');
  });
});

describe('useDeclineCall', () => {
  it('removes the declined call from the incoming calls list', () => {
    const setIncomingCalls = vi.fn();
    const signalingRef = { current: { sendToGroup: vi.fn() } };
    const { result } = renderHook(() =>
      useDeclineCall(vi.fn(), setIncomingCalls, signalingRef as unknown as Parameters<typeof useDeclineCall>[2]),
    );

    act(() => result.current('r1'));

    const updater = vi.mocked(setIncomingCalls).mock.calls[0][0] as (prev: { room: string; callerName: string; claimed: boolean }[]) => typeof prev;;
    const remaining = updater([
      { room: 'r1', callerName: 'Alice', claimed: false },
      { room: 'r2', callerName: 'Bob', claimed: false },
    ]);
    expect(remaining).toEqual([{ room: 'r2', callerName: 'Bob', claimed: false }]);
  });

  it('logs that the call was declined', () => {
    const addLog = vi.fn();
    const { result } = renderHook(() =>
      useDeclineCall(addLog, vi.fn(), { current: { sendToGroup: vi.fn() } } as unknown as Parameters<typeof useDeclineCall>[2]),
    );
    act(() => result.current('r1'));
    expect(addLog).toHaveBeenCalledWith(expect.stringContaining('r1'));
  });
});

describe('useDisconnect', () => {
  function makeDeps(activeRoom: string | null = 'r1') {
    const sendToGroup = vi.fn();
    const disconnect = vi.fn();
    return  {
    ...makeHookDeps({
      activeRoomRef: { current: activeRoom },
      signalingMocks: { sendToGroup, disconnect },
    }),
    sendToGroup,
    disconnect,
  };
  }

  it('does not send call-ended when there is no active room', () => {
    const deps = makeDeps(null);
    const { result } = renderHook(() => useDisconnect(deps as Parameters<typeof useDisconnect>[0]));
    act(() => result.current());
    expect(deps.sendToGroup).not.toHaveBeenCalled();
  });

  it('runs full teardown regardless of active room state', () => {
    const deps = makeDeps('r1');
    const { result } = renderHook(() => useDisconnect(deps as Parameters<typeof useDisconnect>[0]));
    act(() => result.current());
    expect(deps.webrtcRef.current.cleanup).toHaveBeenCalled();
    expect(deps.disconnect).toHaveBeenCalled();
    expect(deps.activeRoomRef.current).toBeNull();
    expect(deps.signalingRef.current).toBeNull();
    expect(deps.setStatus).toHaveBeenCalledWith('disconnected');
    expect(deps.setIncomingCalls).toHaveBeenCalledWith([]);
    expect(deps.addLog).toHaveBeenCalledWith('Disconnected');
  });
});
