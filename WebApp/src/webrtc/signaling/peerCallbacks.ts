import type { PeerCallbacksOptions } from '../ExtendedInterfaces';

export function makePeerCallbacks(options: PeerCallbacksOptions) {
  const { room, addLog, setStatus, attachStream, signaling, onFailed, onDataChannelOpen, intentionalEndRef } = options;
  return {
    onTrack: (trackEvent: RTCTrackEvent) => {
      addLog(`Track: ${trackEvent.track.kind}`);
      attachStream(trackEvent);
      setStatus('streaming');
    },
    onIceCandidate: (candidate: RTCIceCandidate) => {
      signaling?.sendToGroup(room, {
        type: 'ice-candidate',
        room,
        candidate: {
          candidate: candidate.candidate,
          sdpMid: candidate.sdpMid,
          sdpMLineIndex: candidate.sdpMLineIndex,
        },
      });
    },onMicError: (err: Error) => {
      addLog(`Mic error: ${err.message}`);
    },
    onStateChange: (iceConnectionState: RTCIceConnectionState) => {
      addLog(`ICE: ${iceConnectionState}`);
      if (iceConnectionState === 'failed' && !intentionalEndRef?.current) {
        onFailed();
        setStatus('error');
      } else if (iceConnectionState === 'disconnected' && !intentionalEndRef?.current) {
        setTimeout(() => {
          onFailed();
          setStatus('error');
        }, 5000);
      }
    },
    onDataChannelOpen,
  };
}
