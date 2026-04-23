import { vi } from 'vitest';
import type { ConnectDependencies, AcceptCallDependencies, MessageHandlerContext } from '../../../webrtc/ExtendedInterfaces';
import type { IncomingCall, SignalingMessage } from '../../../webrtc/BaseInterfaces';
import type { WebRTCService } from '../../../webrtc/services/webrtcService';
import type { SignalingService } from '../../../webrtc/services/signalingService';

export interface SignalingMocks {
  sendToGroup?: ReturnType<typeof vi.fn>;
  joinGroup?: ReturnType<typeof vi.fn>;
  disconnect?: ReturnType<typeof vi.fn>;
  fetchActiveCalls?: ReturnType<typeof vi.fn>;
  connect?: ReturnType<typeof vi.fn>;
}

export function makeSignalingRef(mocks: SignalingMocks = {}) {
  const sendToGroupMock = mocks.sendToGroup ?? vi.fn();
  const joinGroupMock = mocks.joinGroup ?? vi.fn();
  const disconnectMock = mocks.disconnect ?? vi.fn();
  const fetchActiveCallsMock = mocks.fetchActiveCalls ?? vi.fn().mockResolvedValue([]);
  const connectMock =
    mocks.connect ??
    vi.fn().mockResolvedValue([{ urls: 'stun:stun.example.com' }]);

  const base: Partial<SignalingService> = {
    sendToGroup: (group, data) => new sendToGroupMock(group, data),
    joinGroup: (room) => new joinGroupMock(room),
    disconnect: () => new disconnectMock(),
    fetchActiveCalls: () => new fetchActiveCallsMock(),
    connect: () => new connectMock(),
  };
   return {
    current: base as SignalingService, 
  };
}

export function makeWebrtcRef(overrides: Partial<WebRTCService> = {}) {
  return {
    current: {
      cleanup: vi.fn(),
      create: vi.fn(),
      handleOffer: vi.fn().mockResolvedValue('answer-sdp'),
      addIceCandidate: vi.fn().mockResolvedValue(undefined),
      sendAnnotation: vi.fn(),
      initMic: vi.fn().mockResolvedValue(undefined),
      stopMic: vi.fn(),
      ...overrides,
    } as unknown as WebRTCService,
  };
}

export function makeConnectDeps(
  overrides: Partial<ConnectDependencies> = {},
): ConnectDependencies {
  return {
    addLog: vi.fn(),
    setStatus: vi.fn(),
    setIncomingCalls: vi.fn(),
    handleMessage: vi.fn(),
    userIdRef: { current: 'user-1' },
    signalingRef: { current: null },
    iceServersRef: { current: [] },
    activeRoomRef: { current: null },
    webrtcRef: makeWebrtcRef(),
    intentionalEndRef: { current: false },
    ...overrides,
  } as unknown as ConnectDependencies;
}

export interface MakeAcceptDepsOptions extends Partial<AcceptCallDependencies> {
  signalingMocks?: SignalingMocks;
}

export function makeAcceptDeps({
  signalingMocks,
  ...overrides
}: MakeAcceptDepsOptions = {}): AcceptCallDependencies {
  return {
    room: 'r1',
    addLog: vi.fn(),
    setStatus: vi.fn(),
    attachStream: vi.fn(),
    signalingRef: makeSignalingRef(signalingMocks),
    webrtcRef: makeWebrtcRef(),
    iceServersRef: { current: [{ urls: 'stun:stun.example.com' }] },
    activeRoomRef: { current: null },
    intentionalEndRef: { current: false },
    ...overrides,
  } as unknown as AcceptCallDependencies;
}

export interface MakeMessageCtxOptions extends Partial<MessageHandlerContext> {
  signalingMocks?: SignalingMocks;
}

export function makeMessageCtx({
  signalingMocks,
  ...overrides
}: MakeMessageCtxOptions = {}): MessageHandlerContext {
  return {
    addLog: vi.fn(),
    setStatus: vi.fn(),
    setIncomingCalls: vi.fn(),
    setDataChannelReady: vi.fn(),
    activeRoomRef: { current: null },
    signalingRef: makeSignalingRef(signalingMocks),
    webrtcRef: makeWebrtcRef(),
    intentionalEndRef: { current: false },
    ...overrides,
  } as unknown as MessageHandlerContext;
}

export interface MakeHookDepsOptions extends MakeMessageCtxOptions {
  userIdRef?: { current: string };
  iceServersRef?: { current: RTCIceServer[] };
}

export function makeHookDeps({
  signalingMocks,
  userIdRef,
  iceServersRef,
  ...overrides
}: MakeHookDepsOptions = {}) {
  return {
    ...makeMessageCtx({ signalingMocks, ...overrides }),
    userIdRef: userIdRef ?? { current: 'u1' },
    iceServersRef: iceServersRef ?? { current: [] as RTCIceServer[] },
  };
}

export function makePeerCallbackOptions(overrides: Record<string, unknown> = {}) {
  const sendToGroupMock = vi.fn<(group: string, data: SignalingMessage) => void>();

  return {
    room: 'room1',
    addLog: vi.fn(),
    setStatus: vi.fn(),
    attachStream: vi.fn(),
    signaling: {
      sendToGroup: sendToGroupMock,
    }as unknown as SignalingService,
    onFailed: vi.fn(),
    ...overrides,
  };
}

export function runIncomingCallsUpdater(
  ctx: Pick<MessageHandlerContext, 'setIncomingCalls'>,
  prev: IncomingCall[] = [],
  callIndex = 0,
) {
  const updater = vi.mocked(ctx.setIncomingCalls).mock.calls[callIndex][0];
  return typeof updater === 'function' ? updater(prev) : updater;
}