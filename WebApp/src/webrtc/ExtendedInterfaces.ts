import type React from 'react';
import type { SignalingService } from './services/signalingService.ts';
import type { WebRTCService } from './services/webrtcService.ts';
import type {ConnectionStatus, IncomingCall, IceServerConfig, SignalingMessage, AnnotationStroke} from './BaseInterfaces.ts';


export interface RefLike<T> {
  current: T;
}

export interface LoggingContext {
  addLog: (logMessage: string) => void;
  setStatus: (status: ConnectionStatus) => void;
}

export interface CallContext extends LoggingContext {
  setIncomingCalls: React.Dispatch<React.SetStateAction<IncomingCall[]>>;
}

export interface WebRTCRefs {
  signalingRef: RefLike<SignalingService | null>;
  webrtcRef: RefLike<WebRTCService>;
  iceServersRef: RefLike<IceServerConfig[]>;
  activeRoomRef: RefLike<string | null>;
}

export interface MessageHandlerContext extends CallContext {
  activeRoomRef: RefLike<string | null>;
  webrtcRef: RefLike<WebRTCService>;
  signalingRef: RefLike<SignalingService | null>;
  setDataChannelReady: React.Dispatch<React.SetStateAction<boolean>>;
  intentionalEndRef: React.RefObject<boolean>;
}

export interface ConnectDependencies extends CallContext {
  handleMessage: (signalingMessage: SignalingMessage) => void;
  userIdRef: RefLike<string>;
  signalingRef: RefLike<SignalingService | null>;
  iceServersRef: RefLike<IceServerConfig[]>;
  activeRoomRef: RefLike<string | null>;
  webrtcRef: RefLike<WebRTCService>;
  intentionalEndRef: React.RefObject<boolean>;
}

export interface AcceptCallDependencies extends LoggingContext {
  room: string;
  attachStream: (trackEvent: RTCTrackEvent) => void;
  signalingRef: RefLike<SignalingService | null>;
  webrtcRef: RefLike<WebRTCService>;
  iceServersRef: RefLike<IceServerConfig[]>;
  activeRoomRef: RefLike<string | null>;
  onDataChannelOpen?: () => void;
  intentionalEndRef: React.RefObject<boolean>;
}

export interface HandleMessageDependencies extends CallContext, Omit<WebRTCRefs, 'iceServersRef'> {
  intentionalEndRef: React.RefObject<boolean>;
}

export interface ConnectHookDependencies extends CallContext {
  handleMessage: (signalingMessage: SignalingMessage) => void;
  userIdRef: RefLike<string>;
  signalingRef: RefLike<SignalingService | null>;
  iceServersRef: RefLike<IceServerConfig[]>;
  activeRoomRef: RefLike<string | null>;
  webrtcRef: RefLike<WebRTCService>;
  intentionalEndRef: React.RefObject<boolean>;
}

export interface AcceptCallHookDependencies extends LoggingContext, WebRTCRefs {
  attachStream: (trackEvent: RTCTrackEvent) => void;
  onDataChannelOpen?: () => void;
  intentionalEndRef: React.RefObject<boolean>;
}

export interface DisconnectHookDependencies extends CallContext, Omit<WebRTCRefs, 'iceServersRef'> {
  setDataChannelReady: React.Dispatch<React.SetStateAction<boolean>>;
}

export interface PeerCallbacksOptions extends LoggingContext {
  room: string;
  attachStream: (trackEvent: RTCTrackEvent) => void;
  signaling: SignalingService | null;
  onFailed: () => void;
  onDataChannelOpen?: () => void;
  intentionalEndRef?: React.RefObject<boolean>;
}

export interface UseWebRTCCallbacksDependencies extends Omit<CallContext, 'addLog'>, WebRTCRefs {
  setLogs: React.Dispatch<React.SetStateAction<string[]>>;
  videoRef: React.RefObject<HTMLVideoElement | null>;
  userIdRef: RefLike<string>;
  setDataChannelReady: React.Dispatch<React.SetStateAction<boolean>>;
  intentionalEndRef: React.RefObject<boolean>;
}

export type SignalingMessageHandler = (
  signalingMessage: SignalingMessage,
  messageHandlerContext: MessageHandlerContext,
) => void;

export interface UseWebRTCReturn {
  status: ConnectionStatus;
  incomingCalls: IncomingCall[];
  logs: string[];
  videoRef: React.RefObject<HTMLVideoElement | null>;
  connect: () => void;
  acceptCall: (room: string) => void;
  declineCall: (room: string) => void;
  disconnect: () => void;
  leaveCall: () => void;
  dataChannelReady: boolean;
  sendAnnotation: (stroke: AnnotationStroke) => void;
}
