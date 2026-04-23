import { makePeerCallbacks } from './peerCallbacks.ts';
import { SignalingService } from '../services/signalingService.ts';
import type { AcceptCallDependencies, ConnectDependencies } from '../ExtendedInterfaces.ts';
import type {IncomingCall} from '../BaseInterfaces.ts';

const SIGNALING_SERVER_URL = import.meta.env.VITE_SIGNALING_SERVER_URL ?? 'https://ar-signalingserver.azurewebsites.net';
const JOIN_NOTIFY_DELAY_MS = 50;

export function mergeIncomingCalls(
  previousCalls: IncomingCall[],
  freshCalls: { room: string; callerName: string }[],
): IncomingCall[] {
  const existingRoomIds = new Set(previousCalls.map((c) => c.room));
  const newCalls = freshCalls
    .filter((c) => !existingRoomIds.has(c.room))
    .map((c) => ({ ...c, claimed: false }));
  return newCalls.length > 0 ? [...previousCalls, ...newCalls] : previousCalls;
}

export async function runConnect(ConnectDependencies: ConnectDependencies): Promise<void> {
  const { addLog, setStatus, setIncomingCalls, handleMessage, userIdRef, signalingRef, iceServersRef, activeRoomRef, webrtcRef, intentionalEndRef } = ConnectDependencies;
  await webrtcRef.current.initMic((err) => addLog(`Mic unavailable: ${err.message}`));
  
  const signaling = new SignalingService(SIGNALING_SERVER_URL, userIdRef.current, {
    onConnected: () => { addLog('Connected to signaling server'); setStatus('waiting'); },
    onDisconnected: () => { 
      addLog(`WS closed — intentionalEnd=${intentionalEndRef?.current}`);
      if(!intentionalEndRef.current) setStatus('disconnected');
    },
    onReconnected: () => {
      const room = activeRoomRef.current;
      if (room) signaling.joinGroup(room);
    },
    onError: (err: string) => { addLog(`Error: ${err}`); setStatus('error'); },
    onMessage: handleMessage,
  });
  signalingRef.current = signaling;
  try {
    iceServersRef.current = await signaling.connect();
    addLog(`Got ${iceServersRef.current.length} ICE server(s)`);
    const activeCalls = await signaling.fetchActiveCalls();
    if (activeCalls.length > 0) {
      setIncomingCalls(activeCalls.map((c) => ({ ...c, claimed: false })));
      addLog(`Loaded ${activeCalls.length} active call(s)`);
    }
  } catch (err) {
    addLog(`Connection failed: ${(err as Error).message}`);
    setStatus('error');
  }
}

export async function runAcceptCall(AcceptCallDependencies: AcceptCallDependencies): Promise<void> {
  const { room, addLog, setStatus, attachStream, signalingRef, webrtcRef, iceServersRef, activeRoomRef, onDataChannelOpen, intentionalEndRef } = AcceptCallDependencies;
  if (intentionalEndRef) intentionalEndRef.current = false;
  try {
    const freshIceServers = await signalingRef.current?.fetchIceConfig();
    if (freshIceServers) {
      iceServersRef.current = freshIceServers;
      addLog(`Refreshed ${freshIceServers.length} ICE server(s)`);
    }
  } catch {
    addLog('ICE config refresh failed, using cached credentials');
  }

  const peerCallbacks = makePeerCallbacks({
    room, addLog, setStatus, attachStream,
    signaling: signalingRef.current,
    onFailed: () => {activeRoomRef.current = null},
    onDataChannelOpen, intentionalEndRef,
  });
  webrtcRef.current.create(iceServersRef.current, peerCallbacks);
  signalingRef.current?.joinGroup(room);
  setTimeout(() => {
    signalingRef.current?.sendToGroup(room, { type: 'call-accepted', room });
    setStatus('connecting');
    addLog(`Accepted call in room ${room}`);
  }, JOIN_NOTIFY_DELAY_MS);
}
