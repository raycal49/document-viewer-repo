export interface LogEntry {
  timestamp: number;
  category: 'annotation' | 'webrtc' | 'connection' | 'system';
  message: string;
  data?: Record<string, unknown>;
}

export interface AnnotationLogData {
  type: string;
  color: string;
  pointCount: number;
  strokeType?: string;
}

export interface WebRTCLogData {
  event: string;
  iceState?: string;
  candidate?: string;
  [key: string]: unknown;
}

class AnnotationLogger {
  private logs: LogEntry[] = [];
  private maxLogs = 500;
  private currentCallId: string | null = null;

  // Start a new call session (clears logs)
  startNewCall(callId: string): void {
    this.currentCallId = callId;
    this.logs = [];
    this.addLog('system', `New call started: ${callId}`);
  }

  // End current call
  endCall(): void {
    if (this.currentCallId) {
      this.addLog('system', `Call ended: ${this.currentCallId}`);
      this.currentCallId = null;
    }
  }

  // Add annotation log
  logAnnotationSent(data: AnnotationLogData): void {
    const message = `Sending ${data.type} annotation with color ${data.color} (${data.pointCount} points)`;
    this.addLog('annotation', message, data);
  }

  logAnnotationCleared(): void {
    this.addLog('annotation', 'Clearing all annotations');
  }

  // Add WebRTC logs
  logWebRTCEvent(data: WebRTCLogData): void {
    const message = this.formatWebRTCMessage(data);
    this.addLog('webrtc', message, data);
  }

  logConnectionEvent(message: string, data?: Record<string, unknown>): void {
    this.addLog('connection', message, data);
  }

  logSystemEvent(message: string, data?: Record<string, unknown>): void {
    this.addLog('system', message, data);
  }

  // Get logs with optional filtering
  getLogs(category?: LogEntry['category']): LogEntry[] {
    if (category) {
      return this.logs.filter(log => log.category === category);
    }
    return [...this.logs];
  }

  // Get formatted log strings for UI display
  getFormattedLogs(category?: LogEntry['category']): string[] {
    const logsToFormat = category 
      ? this.logs.filter(log => log.category === category)
      : this.logs;
    return logsToFormat.map(log => this.formatLogEntry(log));
  }

  // Clear all logs
  clear(): void {
    this.logs = [];
  }

  //Export logs as JSON (for download)
  exportToJSON(): string {
    return JSON.stringify({
      exportedAt: new Date().toISOString(),
      callId: this.currentCallId,
      logCount: this.logs.length,
      logs: this.logs
    }, null, 2);
  }

  // Export logs by category
  exportByCategoryToJSON(category: LogEntry['category']): string {
    const categoryLogs = this.logs.filter(log => log.category === category);
    return JSON.stringify({
      exportedAt: new Date().toISOString(),
      callId: this.currentCallId,
      category,
      logCount: categoryLogs.length,
      logs: categoryLogs
    }, null, 2);
  }

  //Export ONLY annotations (no message field, just data)
  exportAnnotationsToJSON(webAppUserId?: string | number, questUserId?: string | number): string {
    const annotationLogs = this.logs.filter(log => log.category === 'annotation');
    
    return JSON.stringify({
      exportedAt: new Date().toISOString(),
      callId: this.currentCallId,
      ...(webAppUserId && { WebAppUserID: webAppUserId }),
      ...(questUserId && { QuestUserID: questUserId }),
      logs: annotationLogs.map(log => ({
        timestamp: log.timestamp,
        category: log.category,
        data: log.data
      }))
    }, null, 2);
  }

  // Private helpers
  private addLog(
    category: LogEntry['category'],
    message: string,
    data?: Record<string, unknown> | AnnotationLogData | WebRTCLogData
    ): void {
    const entry: LogEntry = {
        timestamp: Date.now(),
        category,
        message,
        ...(data && { data: data as Record<string, unknown> })
    };
    this.logs.unshift(entry);
    if (this.logs.length > this.maxLogs) {
        this.logs = this.logs.slice(0, this.maxLogs);
    }
    }

  private formatLogEntry(log: LogEntry): string {
    const time = new Date(log.timestamp).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false
    });
    const category = `[${log.category.toUpperCase()}]`;
    return `${time} ${category} ${log.message}`;
  }

  //Reduced complexity using object lookup instead of switch
  private formatWebRTCMessage(data: WebRTCLogData): string {
    const messages: Record<string, string> = {
      'ice-candidate': 'ICE candidate generated',
      'ice-state-change': `ICE connection state: ${data.iceState || 'unknown'}`,
      'offer-created': 'WebRTC offer created',
      'answer-received': 'WebRTC answer received',
      'peer-connected': 'Peer connection established',
      'peer-disconnected': 'Peer connection lost',
      'data-channel-open': 'Data channel opened',
      'data-channel-closed': 'Data channel closed',
    };
    return messages[data.event] || `WebRTC event: ${data.event}`;
  }
}

export const annotationLogger = new AnnotationLogger();



