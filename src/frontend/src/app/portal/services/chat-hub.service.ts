import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthStore } from '../../auth/auth.store';

export interface ChatMessage {
  id: string;
  sessionId: string;
  senderRole: 'Customer' | 'Agent' | 'System';
  senderId?: string;
  body: string;
  sentAt: string;
}

export interface HandoffAcceptedEvent {
  sessionId: string;
  agentName: string;
}

export interface SessionClosedEvent {
  sessionId: string;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  private connection: signalR.HubConnection | null = null;

  readonly message$ = new Subject<ChatMessage>();
  readonly handoffAccepted$ = new Subject<HandoffAcceptedEvent>();
  readonly sessionClosed$ = new Subject<SessionClosedEvent>();
  readonly agentTyping$ = new Subject<void>();

  connect(): void {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => this.authStore.token() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveMessage', (msg: ChatMessage) => this.message$.next(msg));
    this.connection.on('HandoffAccepted', (evt: HandoffAcceptedEvent) => this.handoffAccepted$.next(evt));
    this.connection.on('SessionClosed', (evt: SessionClosedEvent) => this.sessionClosed$.next(evt));
    this.connection.on('AgentTyping', () => this.agentTyping$.next());

    this.connection.start().catch(err => console.error('ChatHub connection failed:', err));
  }

  async startSession(departmentId?: string): Promise<string> {
    this.ensureConnected();
    return await this.connection!.invoke<string>('StartSession', departmentId ?? null);
  }

  async sendMessage(sessionId: string, body: string): Promise<void> {
    this.ensureConnected();
    await this.connection!.invoke('SendMessage', sessionId, body);
  }

  async customerTyping(sessionId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('CustomerTyping', sessionId);
    }
  }

  async closeSession(sessionId: string): Promise<void> {
    this.ensureConnected();
    await this.connection!.invoke('CloseSession', sessionId, 'Customer closed');
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
  }

  private ensureConnected(): void {
    if (this.connection?.state !== signalR.HubConnectionState.Connected) {
      throw new Error('ChatHub is not connected.');
    }
  }
}
