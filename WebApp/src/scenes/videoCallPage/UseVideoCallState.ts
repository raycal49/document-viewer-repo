import { useState, useEffect, useCallback } from 'react';
import { useWebRTC } from '../../webrtc/hooks/useWebRTC.ts';
import { useAnnotationLogger } from './UseAnnotationLogger.tsx';
import { strokeToAnnotation, makeClearAnnotations } from './Annotation/AnnotationLogic.ts';
import type { StrokeType, AnnotationMessage } from './Annotation/AnnotationLogic.ts';
import type { Stroke } from './Annotation/AnnotationCanvas.tsx';

function useAnnotationHandlers(params: {
  strokeColor: string;
  strokeType: StrokeType;
  logAnnotationSent: (data: { type: string; color: string; pointCount: number; strokeType?: string }) => void;
  logAnnotationCleared: () => void;
  sendAnnotation: (annotation: AnnotationMessage) => void;
}) {
  const { strokeColor, strokeType, logAnnotationSent, logAnnotationCleared, sendAnnotation } = params;

  const handleStroke = useCallback((stroke: Stroke) => {
    const annotation = strokeToAnnotation({ ...stroke, color: strokeColor }, strokeType);
    logAnnotationSent({ type: strokeType, color: strokeColor, pointCount: stroke.points.length, strokeType });
    sendAnnotation(annotation);
  }, [strokeColor, strokeType, logAnnotationSent, sendAnnotation]);

  const handleClearAnnotations = useCallback(() => {
    logAnnotationCleared();
    sendAnnotation(makeClearAnnotations());
  }, [logAnnotationCleared, sendAnnotation]);

  return { handleStroke, handleClearAnnotations };
}

export function useVideoCallState() {
  const {status, incomingCalls, logs: webrtcLogs, videoRef, connect, acceptCall, declineCall, disconnect, leaveCall, sendAnnotation, clearLogs: clearWebRTCLogs, } = useWebRTC();

  const {logs: annotationLogs, logAnnotationSent, logAnnotationCleared, logConnection, logSystem, startNewCall, endCall,} = useAnnotationLogger();

  const [annotationOn, setAnnotationOn]   = useState(false);
  const [strokeType, setStrokeType]       = useState<StrokeType>('fading');
  const [strokeColor, setStrokeColor]     = useState('#FF0000');

  const isStreaming    = status === 'streaming';
  const drawingEnabled = isStreaming && annotationOn;

  useEffect(() => {
    if (status === 'connecting') {
      startNewCall(`call-${Date.now()}`);
      clearWebRTCLogs();
    } else if (status === 'call-ended') {
      endCall();
      leaveCall();
    } else if (status === 'disconnected') {
      endCall();
    }
  }, [status, startNewCall, endCall, clearWebRTCLogs, leaveCall]);

  useEffect(() => {
    logConnection(`Status: ${status}`);
  }, [status, logConnection]);

  const handleToggleDrawing = useCallback(() => {
    setAnnotationOn((prev) => {
      const next = !prev;
      logSystem(`Drawing ${next ? 'enabled' : 'disabled'}`);
      return next;
    });
  }, [logSystem]);

  const handleToggleStrokeType = useCallback(() => {
    setStrokeType((prev) => {
      const next = prev === 'fading' ? 'permanent' : 'fading';
      logSystem(`Stroke type: ${next}`);
      return next;
    });
  }, [logSystem]);

  const { handleStroke, handleClearAnnotations } = useAnnotationHandlers({strokeColor, strokeType,logAnnotationSent,logAnnotationCleared, sendAnnotation, });

  const combinedLogs = [...annotationLogs, ...webrtcLogs];

  return {
    status,
    incomingCalls: status === 'waiting' ? incomingCalls : [],
    logs: combinedLogs,
    videoRef,
    connect,
    acceptCall,
    declineCall,
    disconnect,
    leaveCall,
    drawingEnabled,
    videoVisible: isStreaming,
    strokeType,
    strokeColor,
    handleToggleDrawing,
    handleToggleStrokeType,
    handleClearAnnotations,
    handleStroke,
    handleColorChange: setStrokeColor,
  };
}
