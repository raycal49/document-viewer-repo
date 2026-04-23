import { useEffect, useRef, useState, useCallback } from 'react';
import { SignalingService } from '../services/signalingService.ts';
import { WebRTCService } from '../services/webrtcService.ts';
import { mergeIncomingCalls, useWebRTCCallbacks} from './useWebRTCCallbacks.ts';
import type { ConnectionStatus, IncomingCall, IceServerConfig, AnnotationMessage } from '../BaseInterfaces.ts';

const POLL_ACTIVE_CALLS_INTERVAL_MS = 4000;

function generateUserId(): string {
  return `web-${Math.random().toString(36).slice(2, 7)}`;
}

export function useWebRTC() {
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');
  const [incomingCalls, setIncomingCalls] = useState<IncomingCall[]>([]);
  const [logs, setLogs] = useState<string[]>([]);
  const [dataChannelReady, setDataChannelReady] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);
  const signalingRef = useRef<SignalingService | null>(null);
  const webrtcRef = useRef(new WebRTCService());
  const iceServersRef = useRef<IceServerConfig[]>([]);
  const activeRoomRef = useRef<string | null>(null);
  const userIdRef = useRef(generateUserId());
  const intentionalEndRef = useRef(false);
  

  const clearLogs = useCallback(() => {
    setLogs([]);
  }, []);
  const { connect, acceptCall, declineCall, disconnect, leaveCall } = useWebRTCCallbacks({
    setStatus,
    setIncomingCalls,
    setLogs,
    videoRef,
    signalingRef,
    webrtcRef,
    iceServersRef,
    activeRoomRef,
    userIdRef,
    setDataChannelReady,
    intentionalEndRef
  });

  const sendAnnotation = useCallback((stroke: AnnotationMessage) => {
    webrtcRef.current.sendAnnotation(stroke);
  }, []);

  useEffect(() => {
    if (status !== 'waiting' || !signalingRef.current) return;

    const poll = async () => {
      try {
        const calls = await signalingRef.current!.fetchActiveCalls();
        if (calls.length > 0) {
          setIncomingCalls((prev) => mergeIncomingCalls(prev, calls));
        }
      } catch {
        // non-critical
      }
    };

    const id = setInterval(poll, POLL_ACTIVE_CALLS_INTERVAL_MS);
    return () => clearInterval(id);
  }, [status, setIncomingCalls]);

  useEffect(() => {
    const webrtc = webrtcRef.current;
    const signaling = signalingRef.current;
    return () => {
      webrtc.cleanup();
      signaling?.disconnect();
    };
  }, []);

  return { 
    status, incomingCalls, logs, videoRef, 
    connect, acceptCall, declineCall, disconnect, 
    leaveCall, dataChannelReady, sendAnnotation, clearLogs};
}
