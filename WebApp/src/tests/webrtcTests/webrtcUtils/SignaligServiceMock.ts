import { vi } from 'vitest';

export const mockSignalingInstance = {
  connect: vi.fn().mockResolvedValue([{ urls: 'stun:stun.example.com' }]),
  fetchActiveCalls: vi.fn().mockResolvedValue([]),
  fetchIceConfig: vi.fn().mockResolvedValue([{ urls: 'stun:stun.example.com' }]),
  joinGroup: vi.fn(),
  sendToGroup: vi.fn(),
  disconnect: vi.fn(),
};

export interface CapturedCallbacks {
  onConnected?: () => void;
  onDisconnected?: () => void;
  onReconnected?: () => void;
  onError?: (err: string) => void;
}

export let lastCallbacks: CapturedCallbacks = {};

// Must use a regular function (not arrow) so `new SignalingService()` works
export const SignalingService = vi.fn().mockImplementation(
  function (_url: string, _id: string, callbacks: CapturedCallbacks) {
    lastCallbacks = callbacks;
    callbacks.onConnected?.();
    return mockSignalingInstance;
  },
);

export function resetSignalingMocks() {
  lastCallbacks = {};
  vi.mocked(SignalingService).mockClear();
  vi.mocked(mockSignalingInstance.connect).mockReset().mockResolvedValue([{ urls: 'stun:stun.example.com' }]);
  vi.mocked(mockSignalingInstance.fetchActiveCalls).mockReset().mockResolvedValue([]);
  vi.mocked(mockSignalingInstance.fetchIceConfig).mockReset().mockResolvedValue([{ urls: 'stun:stun.example.com' }]);
  vi.mocked(mockSignalingInstance.joinGroup).mockReset();
  vi.mocked(mockSignalingInstance.sendToGroup).mockReset();
  vi.mocked(mockSignalingInstance.disconnect).mockReset();
}