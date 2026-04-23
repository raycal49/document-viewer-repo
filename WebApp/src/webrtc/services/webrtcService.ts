import type { IceServerConfig, PeerCallbacks, AnnotationMessage } from '../BaseInterfaces.ts';

export class WebRTCService {
  private pc: RTCPeerConnection | null = null;
  private pendingCandidates: RTCIceCandidateInit[] = [];
  private dataChannel: RTCDataChannel | null = null;
  private localStream: MediaStream | null = null;

  async initMic(onMicError?: (err: Error) => void): Promise<void> {
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
    } catch (err) {
      onMicError?.(err as Error);
    }
  }

  stopMic(): void {
    this.localStream?.getTracks().forEach(t => t.stop());
    this.localStream = null;
  }

  create(iceServers: IceServerConfig[], callbacks: PeerCallbacks): void {
    this.cleanup();
    this.pendingCandidates = [];

    this.pc = new RTCPeerConnection({ iceServers });
    this.pc.ontrack = callbacks.onTrack;
    this.pc.onicecandidate = (e) => { if (e.candidate) callbacks.onIceCandidate(e.candidate); };
    this.pc.oniceconnectionstatechange = () => { if (this.pc) callbacks.onStateChange(this.pc.iceConnectionState); };

    if (this.localStream) {
      for (const track of this.localStream.getAudioTracks()) {
        this.pc.addTrack(track, this.localStream);
      }
    } else {
      this.pc.addTransceiver('audio', { direction: 'recvonly' });
    }
    this.pc.addTransceiver('video', { direction: 'recvonly' });

    this.pc.ondatachannel = (event) => {
      if (event.channel.label === 'annotations') {
        this.dataChannel = event.channel;
        this.dataChannel.onopen = () => callbacks.onDataChannelOpen?.();
      }
    };
  }

  async handleOffer(sdp: string): Promise<string> {
    if (!this.pc) throw new Error('Peer connection not initialized');

    await this.pc.setRemoteDescription({ type: 'offer', sdp });

    for (const c of this.pendingCandidates) {
      await this.pc.addIceCandidate(new RTCIceCandidate(c));
    }
    this.pendingCandidates = [];

    const answer = await this.pc.createAnswer();
    await this.pc.setLocalDescription(answer);
    return answer.sdp!;
  }

  async addIceCandidate(candidate: RTCIceCandidateInit): Promise<void> {
    if (this.pc?.remoteDescription) {
      await this.pc.addIceCandidate(new RTCIceCandidate(candidate));
    } else {
      this.pendingCandidates.push(candidate);
    }
  }

  sendAnnotation(stroke: AnnotationMessage): void {
    if (this.dataChannel?.readyState === 'open') {
      this.dataChannel.send(JSON.stringify(stroke));
    }
  }

  cleanup(): void {
    this.pc?.close();
    this.pc = null;
    this.pendingCandidates = [];
    this.dataChannel?.close();
    this.dataChannel = null;
    //voice channel not stopped here to allow 
    // use for multiple calls in one session
  }
}
