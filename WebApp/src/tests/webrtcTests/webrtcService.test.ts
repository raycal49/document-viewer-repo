import { describe, it, expect, vi, beforeEach } from 'vitest';
import { WebRTCService } from '../../webrtc/services/webrtcService';
import type { AnnotationStroke } from '../../webrtc/BaseInterfaces';

type MockDataChannel = {
  label: string;
  readyState: RTCDataChannelState;
  onopen: (() => void) | null;
  send: ReturnType<typeof vi.fn>;
  close: ReturnType<typeof vi.fn>;
};

function makeDataChannel(label: string, readyState: RTCDataChannelState = 'open'): MockDataChannel {
  return { label, readyState, onopen: null, send: vi.fn(), close: vi.fn() };
}

class MockPeerConnection {
  ontrack: ((e: RTCTrackEvent) => void) | null = null;
  onicecandidate: ((e: { candidate: RTCIceCandidate | null }) => void) | null = null;
  oniceconnectionstatechange: (() => void) | null = null;
  ondatachannel: ((e: { channel: MockDataChannel }) => void) | null = null;
  iceConnectionState: RTCIceConnectionState = 'new';
  remoteDescription: RTCSessionDescription | null = null;

  close = vi.fn();
  addTransceiver = vi.fn();
  addTrack = vi.fn();
  setRemoteDescription = vi.fn().mockResolvedValue(undefined);
  createAnswer = vi.fn().mockResolvedValue({ sdp: 'answer-sdp', type: 'answer' });
  setLocalDescription = vi.fn().mockResolvedValue(undefined);
  addIceCandidate = vi.fn().mockResolvedValue(undefined);
}

let mockPc: MockPeerConnection;

beforeEach(() => {
  mockPc = new MockPeerConnection();
  vi.stubGlobal('RTCPeerConnection', vi.fn().mockImplementation(function () {
    return mockPc;
  }));
  vi.stubGlobal('RTCIceCandidate', vi.fn().mockImplementation(function (init: RTCIceCandidateInit) {
    return init;
  }));
});

function makeCallbacks(extra: { onDataChannelOpen?: () => void } = {}) {
  return {
    onTrack: vi.fn(),
    onIceCandidate: vi.fn(),
    onStateChange: vi.fn(),
    ...extra,
  };
}

const iceServers = [{ urls: 'stun:stun.example.com' }] as RTCIceServer[];

describe('create: lifecycle', () => {
  it('closes any existing peer connection before creating a new one', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    const firstPc = mockPc;
    mockPc = new MockPeerConnection();
    vi.mocked(RTCPeerConnection).mockImplementation(function () { return mockPc; });

    svc.create(iceServers, makeCallbacks());
    expect(firstPc.close).toHaveBeenCalled();
  });

  it('resets pending candidates on each create', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());

    await svc.addIceCandidate({ candidate: 'cand1' } as RTCIceCandidateInit);

    mockPc = new MockPeerConnection();
    vi.mocked(RTCPeerConnection).mockImplementation(function () { return mockPc; });
    svc.create(iceServers, makeCallbacks());

    mockPc.remoteDescription = {} as RTCSessionDescription;
    await svc.handleOffer('offer-sdp');
    expect(mockPc.addIceCandidate).not.toHaveBeenCalled();
  });

  it('creates RTCPeerConnection with the provided ICE servers', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    expect(RTCPeerConnection).toHaveBeenCalledWith({ iceServers });
  });

});

describe('create', () => {
  it('wires ontrack, onicecandidate, oniceconnectionstatechange and adds transceivers', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    expect(mockPc.ontrack).toBeTypeOf('function');
    expect(mockPc.onicecandidate).toBeTypeOf('function');
    expect(mockPc.oniceconnectionstatechange).toBeTypeOf('function');
    expect(mockPc.addTransceiver).toHaveBeenCalledWith('video', { direction: 'recvonly' });
    expect(mockPc.addTransceiver).toHaveBeenCalledWith('audio', { direction: 'recvonly' });
  });

  it('calls onIceCandidate callback when a candidate is produced', () => {
    const cbs = makeCallbacks();
    const svc = new WebRTCService();
    svc.create(iceServers, cbs);
    const candidate = { candidate: 'cand1' } as RTCIceCandidate;
    mockPc.onicecandidate!({ candidate });
    expect(cbs.onIceCandidate).toHaveBeenCalledWith(candidate);
  });

  it('does not call onIceCandidate when candidate is null', () => {
    const cbs = makeCallbacks();
    const svc = new WebRTCService();
    svc.create(iceServers, cbs);
    mockPc.onicecandidate!({ candidate: null });
    expect(cbs.onIceCandidate).not.toHaveBeenCalled();
  });

  it('calls onStateChange with the current ICE connection state', () => {
    const cbs = makeCallbacks();
    const svc = new WebRTCService();
    svc.create(iceServers, cbs);
    mockPc.iceConnectionState = 'connected';
    mockPc.oniceconnectionstatechange!();
    expect(cbs.onStateChange).toHaveBeenCalledWith('connected');
  });

  it('does not call onStateChange when pc is null at time of state change', () => {
    const cbs = makeCallbacks();
    const svc = new WebRTCService();
    svc.create(iceServers, cbs);

    const handler = mockPc.oniceconnectionstatechange!;
    svc.cleanup(); 
    handler();

    expect(cbs.onStateChange).not.toHaveBeenCalled();
  });
});

describe('handleOffer', () => {
  it('throws when called before create()', async () => {
    const svc = new WebRTCService();
    await expect(svc.handleOffer('sdp')).rejects.toThrow('Peer connection not initialized');
  });

  it('sets remote description, creates and returns answer sdp', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    mockPc.remoteDescription = {} as RTCSessionDescription;
    const result = await svc.handleOffer('offer-sdp');
    expect(mockPc.setRemoteDescription).toHaveBeenCalledWith({ type: 'offer', sdp: 'offer-sdp' });
    expect(mockPc.createAnswer).toHaveBeenCalled();
    expect(mockPc.setLocalDescription).toHaveBeenCalled();
    expect(result).toBe('answer-sdp');
  });

  it('flushes queued candidates before creating the answer', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());

    await svc.addIceCandidate({ candidate: 'c1' } as RTCIceCandidateInit);
    await svc.addIceCandidate({ candidate: 'c2' } as RTCIceCandidateInit);

    mockPc.remoteDescription = {} as RTCSessionDescription;
    await svc.handleOffer('offer-sdp');

    expect(mockPc.addIceCandidate).toHaveBeenCalledTimes(2);
  });

  it('clears pending candidates after flushing', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    await svc.addIceCandidate({ candidate: 'c1' } as RTCIceCandidateInit);

    mockPc.remoteDescription = {} as RTCSessionDescription;
    await svc.handleOffer('offer-sdp');

    mockPc.addIceCandidate.mockClear();
    await svc.handleOffer('offer-sdp2');
    expect(mockPc.addIceCandidate).not.toHaveBeenCalled();
  });
});


describe('addIceCandidate', () => {
  it('adds the candidate immediately when remoteDescription is set', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    mockPc.remoteDescription = {} as RTCSessionDescription;

    await svc.addIceCandidate({ candidate: 'cand1' } as RTCIceCandidateInit);
    expect(mockPc.addIceCandidate).toHaveBeenCalled();
  });

  it('queues the candidate when remoteDescription is not set', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    mockPc.remoteDescription = null;

    await svc.addIceCandidate({ candidate: 'cand1' } as RTCIceCandidateInit);
    expect(mockPc.addIceCandidate).not.toHaveBeenCalled();
  });
});


describe('cleanup', () => {
  it('closes the peer connection when one exists', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    svc.cleanup();
    expect(mockPc.close).toHaveBeenCalled();
  });

  it('does not throw when called with no peer connection', () => {
    const svc = new WebRTCService();
    expect(() => svc.cleanup()).not.toThrow();
  });

  it('clears pending candidates on cleanup', async () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    await svc.addIceCandidate({ candidate: 'c1' } as RTCIceCandidateInit);

    svc.cleanup();

    mockPc = new MockPeerConnection();
    vi.mocked(RTCPeerConnection).mockImplementation(function () { return mockPc; });
    svc.create(iceServers, makeCallbacks());
    mockPc.remoteDescription = {} as RTCSessionDescription;
    await svc.handleOffer('sdp');
    expect(mockPc.addIceCandidate).not.toHaveBeenCalled();
  });

  it('closes the data channel when one exists', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());

    const channel = makeDataChannel('annotations');
    mockPc.ondatachannel!({ channel });

    svc.cleanup();
    expect(channel.close).toHaveBeenCalled();
  });
});

describe('data channel', () => {
  it('registers an ondatachannel handler on the peer connection', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    expect(mockPc.ondatachannel).toBeTypeOf('function');
  });

  it('calls onDataChannelOpen when the AnnotationChannel opens', () => {
    const onDataChannelOpen = vi.fn();
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks({ onDataChannelOpen }));

    const channel = makeDataChannel('annotations');
    mockPc.ondatachannel!({ channel });
    channel.onopen!();

    expect(onDataChannelOpen).toHaveBeenCalledOnce();
  });

  it('does not store the channel or wire onopen for non-AnnotationChannel labels', () => {
    const onDataChannelOpen = vi.fn();
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks({ onDataChannelOpen }));

    const channel = makeDataChannel('SomeOtherChannel');
    mockPc.ondatachannel!({ channel });
    channel.onopen?.();

    expect(onDataChannelOpen).not.toHaveBeenCalled();
  });

  it('does not throw when onDataChannelOpen is absent and the channel opens', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());

    const channel = makeDataChannel('annotations');
    mockPc.ondatachannel!({ channel });
    expect(() => channel.onopen!()).not.toThrow();
  });
});

describe('sendAnnotation', () => {
  it('sends the stroke as JSON when the data channel is open', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());

    const channel = makeDataChannel('annotations', 'open');
    mockPc.ondatachannel!({ channel });

    const stroke: AnnotationStroke = { Type: 'stroke', StrokeType: 'permanent', Vector: [[1, 2]], Color: [255, 0, 0] };
    svc.sendAnnotation(stroke);

    expect(channel.send).toHaveBeenCalledWith(JSON.stringify(stroke));
  });

  it('does nothing when the data channel readyState is not open', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());

    const channel = makeDataChannel('annotations', 'connecting');
    mockPc.ondatachannel!({ channel });

    svc.sendAnnotation({ Type: 'stroke', StrokeType: 'permanent', Vector: [], Color: [0, 0, 0] });

    expect(channel.send).not.toHaveBeenCalled();
  });

  it('does nothing when no data channel has been assigned', () => {
    const svc = new WebRTCService();
    svc.create(iceServers, makeCallbacks());
    // Never trigger ondatachannel — dataChannel stays null
    expect(() => svc.sendAnnotation({ Type: 'stroke', StrokeType: 'permanent', Vector: [], Color: [0, 0, 0] })).not.toThrow();
  });
});

function stubGetUserMedia(stream: object) {
  Object.defineProperty(global.navigator, 'mediaDevices', {
    value: { getUserMedia: vi.fn().mockResolvedValue(stream) },
    configurable: true,
  });
}

describe('initMic', () => {
  it('stores the stream returned by getUserMedia', async () => {
    const stop = vi.fn();
    const fakeStream = {
      getAudioTracks: vi.fn().mockReturnValue([{ stop }]),
      getTracks: vi.fn().mockReturnValue([{ stop }]),
    };
    stubGetUserMedia(fakeStream);
    const svc = new WebRTCService();
    await svc.initMic();
    svc.stopMic();
    expect(stop).toHaveBeenCalled();
  });

  it('calls onMicError when getUserMedia rejects', async () => {
    const error = new Error('Permission denied');
    Object.defineProperty(global.navigator, 'mediaDevices', {
      value: { getUserMedia: vi.fn().mockRejectedValue(error) },
      configurable: true,
    });
    const onMicError = vi.fn();
    const svc = new WebRTCService();
    await svc.initMic(onMicError);
    expect(onMicError).toHaveBeenCalledWith(error);
  });
});

describe('stopMic', () => {
  it('does not throw when no stream is set', () => {
    const svc = new WebRTCService();
    expect(() => svc.stopMic()).not.toThrow();
  });
});

describe('create with localStream', () => {
  it('adds audio tracks via addTrack when a local stream is set', async () => {
    const fakeTrack = { stop: vi.fn() };
    const fakeStream = {
      getAudioTracks: vi.fn().mockReturnValue([fakeTrack]),
      getTracks: vi.fn().mockReturnValue([fakeTrack]),
    };
    stubGetUserMedia(fakeStream);
    const svc = new WebRTCService();
    await svc.initMic();
    svc.create(iceServers, makeCallbacks());
    expect(mockPc.addTrack).toHaveBeenCalledWith(fakeTrack, fakeStream);
  });
});