import { useState, useCallback } from 'react';
import { annotationLogger } from './AnnotationLogger';
import type { AnnotationLogData, WebRTCLogData, LogEntry } from './AnnotationLogger';

export function useAnnotationLogger() {
  const [logs, setLogs] = useState<string[]>([]);

  const refreshLogs = useCallback((category?: LogEntry['category']) => {
    setLogs(annotationLogger.getFormattedLogs(category));
  }, []);

  const startNewCall = useCallback((callId: string) => {
    annotationLogger.startNewCall(callId);
    refreshLogs();
  }, [refreshLogs]);

  const endCall = useCallback(() => {
    annotationLogger.endCall();
    refreshLogs();
  }, [refreshLogs]);

  const logAnnotationSent = useCallback((data: AnnotationLogData) => {
    annotationLogger.logAnnotationSent(data);
    refreshLogs();
  }, [refreshLogs]);

  const logAnnotationCleared = useCallback(() => {
    annotationLogger.logAnnotationCleared();
    refreshLogs();
  }, [refreshLogs]);

  const logWebRTC = useCallback((data: WebRTCLogData) => {
    annotationLogger.logWebRTCEvent(data);
    refreshLogs();
  }, [refreshLogs]);

  const logConnection = useCallback((message: string, data?: Record<string, unknown>) => {
    annotationLogger.logConnectionEvent(message, data);
    refreshLogs();
  }, [refreshLogs]);

  const logSystem = useCallback((message: string, data?: Record<string, unknown>) => {
    annotationLogger.logSystemEvent(message, data);
    refreshLogs();
  }, [refreshLogs]);

  const clearLogs = useCallback(() => {
    annotationLogger.clear();
    refreshLogs();
  }, [refreshLogs]);

  const exportLogs = useCallback(() => {
    const json = annotationLogger.exportToJSON();
    downloadJSON(json, `call-logs-${Date.now()}.json`);
  }, []);

  const exportCategoryLogs = useCallback((category: LogEntry['category']) => {
    const json = annotationLogger.exportByCategoryToJSON(category);
    downloadJSON(json, `call-logs-${category}-${Date.now()}.json`);
  }, []);

  const exportAnnotations = useCallback((webAppUserId?: string | number, questUserId?: string | number) => {
    const json = annotationLogger.exportAnnotationsToJSON(webAppUserId, questUserId);
    downloadJSON(json, `annotations-${Date.now()}.json`);
  }, []);

  return {
    logs,
    logAnnotationSent,
    logAnnotationCleared,
    logWebRTC,
    logConnection,
    logSystem,
    clearLogs,
    exportLogs,
    exportCategoryLogs,
    exportAnnotations,
    refreshLogs,
    startNewCall,
    endCall
  };
}

// Helper function outside hook (not using React hooks)
function downloadJSON(json: string, filename: string): void {
  const blob = new Blob([json], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}