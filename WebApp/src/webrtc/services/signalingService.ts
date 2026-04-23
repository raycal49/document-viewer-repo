import type { IceServerConfig, SignalingMessage, SignalingCallbacks } from '../BaseInterfaces.ts';

export class SignalingService {
  private ws: WebSocket | null = null;
  private serverUrl: string;
  private userId: string;
  private callbacks: SignalingCallbacks;
  private intentionallyClosed = false;
  private connected: boolean = false;
  private messageQueue: unknown[] = [];
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnecting = false;

  constructor(serverUrl: string, userId: string, callbacks: SignalingCallbacks) {
    this.serverUrl = serverUrl;
    this.userId = userId;
    this.callbacks = callbacks;
  }

  async connect(): Promise<IceServerConfig[]> {
    const iceServers = await this.fetchIceConfig();
    const { url } = await this.negotiate();   
    await this.openWebSocket(url);

    return iceServers;
  }

  async fetchIceConfig(retries = 3): Promise<IceServerConfig[]> {
    for (let i = 0; i < retries; i++) {
      try {
        return await this.fetchIceConfigAttempt();
      } catch (error: unknown) {
        const message = error instanceof Error ? error.message : String(error);
        if (i === retries - 1) {
          this.callbacks.onError(`Failed to get ICE config: ${message}`);
          throw error;
        }
        await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, i)));
      }
    }
    throw new Error('Unreachable');
  }
 
  private async fetchIceConfigAttempt(): Promise<IceServerConfig[]> {
    const res = await fetch(`${this.serverUrl}/ice-config`, {
      signal: AbortSignal.timeout(10000)
    });
 
    if (!res.ok) {
      throw new Error(`ICE config failed: ${res.status} ${res.statusText}`);
    }
 
    const data: { iceServers: IceServerConfig[] } = await res.json();
 
    if (!data.iceServers || !Array.isArray(data.iceServers)) {
      throw new Error('Invalid ICE config response');
    }
 
    return data.iceServers;
  }

  async fetchActiveCalls(): Promise<{ room: string; callerName: string }[]> {
    try {
      const res = await fetch(`${this.serverUrl}/active-calls`);
      if (!res.ok) return [];
      const data: { calls: { room: string; callerName: string }[] } = await res.json();
      return data.calls ?? [];
    } catch {
      return [];
    }
  }

  joinGroup(group: string): void {
    this.wsSend({ type: 'joinGroup', group });
  }

  sendToGroup(group: string, data: SignalingMessage): void {
    this.wsSend({ type: 'sendToGroup', group, dataType: 'json', data });
  }

  disconnect(): void {
    this.intentionallyClosed = true;
    if (this.ws) {
      this.ws.onopen = null;
      this.ws.onmessage = null;
      this.ws.onclose = null;
      this.ws.onerror = null;
      
      if (this.ws.readyState === WebSocket.OPEN || 
          this.ws.readyState === WebSocket.CONNECTING) {
        this.ws.close();
      }
      
      this.ws = null;
    }
    
    this.connected = false;
    this.messageQueue = [];
    this.reconnecting = false;
    this.reconnectAttempts = 0;
  }

  private async negotiate(): Promise<{ url: string }> {
    const res = await fetch(
      `${this.serverUrl}/negotiate?userId=${this.userId}&role=viewer`
    );
    if (!res.ok) throw new Error(`Negotiation failed: ${res.status}`);
    return res.json();
  }

  private openWebSocket(url: string): Promise<void> {
    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(url, 'json.webpubsub.azure.v1');

      this.ws.onopen = () => {
        this.connected = true;
        this.reconnectAttempts = 0;
        
        this.joinGroup('lobby');        
        this.flushMessageQueue();        
        this.callbacks.onConnected();
        resolve();
      };

      this.ws.onmessage = (event: MessageEvent) => {
        const parsed = this.parseMessage(event.data as string);
        if (parsed) this.callbacks.onMessage(parsed);
      };

      this.ws.onclose = async () => {
        this.connected = false;
        this.callbacks.onDisconnected();

        if (this.intentionallyClosed) return;
        
        if (!this.reconnecting && this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnecting = true;
          this.reconnectAttempts++;
          
          await new Promise(r => setTimeout(r, 2000));
          
          try {
            await this.connect();
            this.reconnecting = false;
            this.callbacks.onReconnected?.();
          } catch (error: unknown) {
            this.reconnecting = false;
            const message = error instanceof Error ? error.message : String(error);
            if (this.reconnectAttempts >= this.maxReconnectAttempts) {
              this.callbacks.onError('Failed to reconnect: ' + message);
            }
          }
        }
      };

      this.ws.onerror = () => {
        const error = 'WebSocket connection error';
        this.callbacks.onError(error);
        reject(new Error(error));
      };
    });
  }

  private parseMessage(raw: string): SignalingMessage | null {
    try {
      const envelope = JSON.parse(raw);
      if (envelope.type === 'ack') return null;

      let data: unknown = envelope.data;
      if (typeof data === 'string') data = JSON.parse(data);

      const msg = data as SignalingMessage | undefined;
      return msg?.type ? msg : null;
    } catch{
      return null;
    }
  }

  private wsSend(message: unknown): void {
    if (this.connected && this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    } else {
      this.messageQueue.push(message);
    }
  }
  
  private flushMessageQueue(): void {
    while (this.messageQueue.length > 0) {
      const msg = this.messageQueue.shift();
      if (msg && this.ws?.readyState === WebSocket.OPEN) {
        this.ws.send(JSON.stringify(msg));
      }
    }
  }
}
