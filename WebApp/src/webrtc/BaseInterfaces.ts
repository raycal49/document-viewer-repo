export type ConnectionStatus =
  | 'disconnected'
  | 'waiting'
  | 'connecting'
  | 'streaming'
  | 'error'
  | 'call-ended';

export interface IncomingCall {
  room: string;
  callerName: string;
  claimed: boolean;
}

export type StrokeType = 'fading' | 'permanent';

export interface AnnotationStroke {
  Type: 'stroke';
  Vector: [number, number][];
  Color:  [number, number, number];
  StrokeType: StrokeType;
  FadeDuration?: number;
}

export interface ClearAnnotations {
  Type: 'clear-annotations';
}

export type AnnotationMessage = AnnotationStroke | ClearAnnotations;

export interface IceServerConfig {
  urls: string | string[];
  username?: string;
  credential?: string;
}

export interface PeerCallbacks {
  onTrack: (event: RTCTrackEvent) => void;
  onIceCandidate: (candidate: RTCIceCandidate) => void;
  onStateChange: (state: RTCIceConnectionState) => void;
  onDataChannelOpen?: () => void
  onMicError?: (err: Error) => void;
}

export interface SignalingCallbacks {
  onConnected: () => void;
  onDisconnected: () => void;
  onReconnected: () => void;
  onError: (error: string) => void;
  onMessage: (message: SignalingMessage) => void;
}

/** Message format exchanged over the Azure Web PubSub signaling channel. */
export interface SignalingMessage {
  type: string;
  room?: string;
  callerName?: string;
  sdp?: string;
  candidate?: RTCIceCandidateInit;
  senderId?: string;
  claimedBy?: string;
  annotation?: AnnotationStroke;
}
