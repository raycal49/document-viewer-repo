import type { MessageHandlerContext, SignalingMessageHandler } from '../ExtendedInterfaces.ts';
import type {SignalingMessage} from '../BaseInterfaces.ts';

const CLAIMED_CALL_REMOVE_DELAY_MS = 3000;

function handleCallRequest(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  if (!msg.room || !msg.callerName) return;
  const { room, callerName } = msg;
  MsgHandlerContext.setIncomingCalls((prev) => {
    if (prev.some((c) => c.room === room)) return prev;
    return [...prev, { room, callerName, claimed: false }];
  });
}

function handleCallClaimed(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  if (MsgHandlerContext.activeRoomRef.current === msg.room) return;
  MsgHandlerContext.setIncomingCalls((prev) =>
    prev.map((c) => (c.room === msg.room ? { ...c, claimed: true } : c)),
  );
  setTimeout(
    () => MsgHandlerContext.setIncomingCalls((prev) => prev.filter((c) => c.room !== msg.room)),
    CLAIMED_CALL_REMOVE_DELAY_MS,
  );
}

function handleCallEnded(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  MsgHandlerContext.setIncomingCalls((prev) => prev.filter((c) => c.room !== msg.room));
  if (MsgHandlerContext.activeRoomRef.current === msg.room) {
    MsgHandlerContext.addLog('handleCallEnded fired');
    MsgHandlerContext.intentionalEndRef.current = true;
    MsgHandlerContext.activeRoomRef.current = null;
    MsgHandlerContext.webrtcRef.current.cleanup();
    MsgHandlerContext.setDataChannelReady(false);
    MsgHandlerContext.setStatus('call-ended');
  }
}

function handleCallAlreadyClaimed(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  if (MsgHandlerContext.activeRoomRef.current !== msg.room) return;
  MsgHandlerContext.activeRoomRef.current = null;
  MsgHandlerContext.webrtcRef.current.cleanup();
  MsgHandlerContext.setStatus('error');
  MsgHandlerContext.addLog('Call was taken by someone else');
}

function handleOffer(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  const activeRoom = MsgHandlerContext.activeRoomRef.current;
  if (activeRoom !== msg.room || !msg.sdp) return;
  MsgHandlerContext.webrtcRef.current
    .handleOffer(msg.sdp)
    .then((answerSdp) => {
      MsgHandlerContext.signalingRef.current?.sendToGroup(activeRoom, { type: 'answer', room: activeRoom, sdp: answerSdp });
      MsgHandlerContext.addLog('Answer sent');
    })
    .catch((err: unknown) => {
      MsgHandlerContext.addLog(`Offer handling failed: ${(err as Error).message}`);
      MsgHandlerContext.setStatus('error');
    });
}

function handleIceCandidate(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  if (MsgHandlerContext.activeRoomRef.current !== msg.room || !msg.candidate) return;
  MsgHandlerContext.webrtcRef.current
    .addIceCandidate(msg.candidate)
    .catch((err: unknown) => MsgHandlerContext.addLog(`ICE error: ${(err as Error).message}`));
}

const SIGNALING_MESSAGE_HANDLERS: Partial<Record<string, SignalingMessageHandler>> = {
  'call-request': handleCallRequest,
  'call-claimed': handleCallClaimed,
  'call-ended': handleCallEnded,
  'call-already-claimed': handleCallAlreadyClaimed,
  offer: handleOffer,
  'ice-candidate': handleIceCandidate,
};

export function routeSignalingMessage(msg: SignalingMessage, MsgHandlerContext: MessageHandlerContext): void {
  const handler = SIGNALING_MESSAGE_HANDLERS[msg.type];
  if (handler) handler(msg, MsgHandlerContext);
}
