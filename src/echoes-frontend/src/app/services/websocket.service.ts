import { Injectable, signal } from '@angular/core';
import { parseServerMessage, ServerMessage } from '../models/server-messages';

export type MessageHandler = (message: ServerMessage) => void;

@Injectable({ providedIn: 'root' })
export class WebSocketService {
  private ws: WebSocket | null = null;
  private _isConnected = signal(false);
  private messageHandler: MessageHandler | null = null;

  readonly isConnected = this._isConnected.asReadonly();

  onMessage(handler: MessageHandler): void {
    this.messageHandler = handler;
  }

  connect(url: string = 'ws://localhost:5000/ws'): void {
    this.ws = new WebSocket(url);

    this.ws.onopen = () => {
      this._isConnected.set(true);
    };

    this.ws.onmessage = (event: MessageEvent) => {
      const msg = parseServerMessage(event.data as string);
      if (msg && this.messageHandler) {
        this.messageHandler(msg);
      }
    };

    this.ws.onclose = () => {
      this._isConnected.set(false);
    };

    this.ws.onerror = () => {
      this._isConnected.set(false);
    };
  }

  disconnect(): void {
    this.ws?.close();
    this.ws = null;
    this._isConnected.set(false);
  }

  send(payload: object): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(payload));
    }
  }

  get connected(): boolean {
    return this.ws !== null && this.ws.readyState === WebSocket.OPEN;
  }
}
