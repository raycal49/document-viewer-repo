import type React from 'react';
import { useCallback } from 'react';
import type { IncomingCall, SignalingMessage } from '../BaseInterfaces.ts';
import type { SignalingService } from '../services/signalingService.ts';
import { routeSignalingMessage } from '../signaling/signalingMessageHandlers.ts';
import { runConnect, runAcceptCall } from '../signaling/callHelpers.ts';
import type {
  AcceptCallHookDependencies,
  ConnectHookDependencies,
  DisconnectHookDependencies,
  HandleMessageDependencies,
} from '../ExtendedInterfaces.ts';

// Add [WEBRTC] tag and 24-hour time format
export function useAddLog(setLogs: React.Dispatch<React.SetStateAction<string[]>>) {
  return useCallback(
    (msg: string) => {
      const time = new Date().toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: false
      });
      setLogs((prev) => [`${time} [WEBRTC] ${msg}`, ...prev]);
    },
    [setLogs],
  );
}

export function useAttachStream(videoRef: React.RefObject<HTMLVideoElement | null>) {
  return useCallback(
    (trackEvent: RTCTrackEvent) => {
      const el = videoRef.current;
      if (!el) return;
      if (trackEvent.streams[0]) {
        el.srcObject = trackEvent.streams[0];
      } else {
        const stream = (el.srcObject as MediaStream) ?? new MediaStream();
        stream.addTrack(trackEvent.track);
        el.srcObject = stream;
      }
      return el.play().catch((err: DOMException) => {
        if (err.name !== 'AbortError') throw err;
      });
    },
    [videoRef],
  );
}

export function useHandleMessage(deps: HandleMessageDependencies) {
  const { addLog, setStatus, setIncomingCalls, activeRoomRef, webrtcRef, signalingRef, intentionalEndRef } = deps;
  return useCallback(
    (msg: SignalingMessage) => {
      addLog(`← ${msg.type}${msg.room ? ` [${msg.room}]` : ''}`);
      routeSignalingMessage(msg, { addLog, setStatus, setIncomingCalls, activeRoomRef, webrtcRef, signalingRef, setDataChannelReady: () => {}, intentionalEndRef });
    },
    [addLog, setStatus, setIncomingCalls, activeRoomRef, webrtcRef, signalingRef, intentionalEndRef],
  );
}

export function useConnect(deps: ConnectHookDependencies) {
  const { addLog, setStatus, setIncomingCalls, handleMessage, userIdRef, signalingRef, iceServersRef, activeRoomRef, webrtcRef, intentionalEndRef } = deps;
  return useCallback(
    () => runConnect({ addLog, setStatus, setIncomingCalls, handleMessage, userIdRef, signalingRef, iceServersRef, activeRoomRef, webrtcRef, intentionalEndRef }),
    [addLog, setStatus, setIncomingCalls, handleMessage, userIdRef, signalingRef, iceServersRef, activeRoomRef, webrtcRef, intentionalEndRef],
  );
}

export function useAcceptCall(deps: AcceptCallHookDependencies) {
  const { addLog, setStatus, attachStream, signalingRef, webrtcRef, iceServersRef, activeRoomRef, onDataChannelOpen, intentionalEndRef } = deps;
  return useCallback(
    (room: string) => {
      if (activeRoomRef.current) { addLog('Already in a call'); return; }
      activeRoomRef.current = room;
      runAcceptCall({ room, addLog, setStatus, attachStream, signalingRef, webrtcRef, iceServersRef, activeRoomRef, onDataChannelOpen, intentionalEndRef, 
      }).catch((err: unknown) => {
        addLog(`Accept call failed: ${(err as Error).message}`);
        activeRoomRef.current = null;
        setStatus('error');
      });
    },
    [addLog, setStatus, attachStream, signalingRef, webrtcRef, iceServersRef, activeRoomRef, onDataChannelOpen, intentionalEndRef],
  );
}

export function useDeclineCall(
  addLog: (msg: string) => void,
  setIncomingCalls: React.Dispatch<React.SetStateAction<IncomingCall[]>>,
  signalingRef: { current: SignalingService | null },
) {
  return useCallback(
    (room: string) => {
      setIncomingCalls((prev) => prev.filter((c) => c.room !== room));
      signalingRef.current?.sendToGroup(room, { type: 'call-declined', room });
      addLog(`Declined call from room ${room}`);
    },
    [addLog, setIncomingCalls, signalingRef],
  );
}

export function useDisconnect(deps: DisconnectHookDependencies) {
  const { addLog, setStatus, setIncomingCalls, signalingRef, webrtcRef, activeRoomRef, setDataChannelReady } = deps;
  return useCallback(() => {
    const room = activeRoomRef.current;
    if (room) signalingRef.current?.sendToGroup(room, { type: 'call-ended', room });
    activeRoomRef.current = null;
    webrtcRef.current.cleanup();
    webrtcRef.current.stopMic();
    signalingRef.current?.disconnect();
    signalingRef.current = null;
    setDataChannelReady(false);
    setStatus('disconnected');
    setIncomingCalls([]);
    addLog('Disconnected');
  }, [activeRoomRef, webrtcRef, signalingRef, setStatus, setIncomingCalls, addLog, setDataChannelReady]);
}