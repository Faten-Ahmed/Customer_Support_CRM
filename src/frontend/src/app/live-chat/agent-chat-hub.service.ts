import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthStore } from '../auth/auth.store';

export interface HandoffRequestedEvent {
  sessionId: string;
  customerName: string;
  departmentId?: string;
}

export interface AgentChatMessage {
  id: string;
  sessionId: string;
  senderRole: 'Customer' | 'Agent' | 'System';
  senderId?: string;
  body: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class AgentChatHubService {
  private readonly authStore = inject(AuthStore);
  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  readonly handoffRequested$ = new Subject<HandoffRequestedEvent>();
  readonly handoffAccepted$ = new Subject<{ sessionId: string; agentName: string }>();
  readonly message$ = new Subject<AgentChatMessage>();
  readonly customerTyping$ = new Subject<void>();
  readonly sessionClosed$ = new Subject<{ sessionId: string; reason: string }>();

  connect(): Promise<void> {
    if (this.connection) return this.startPromise ?? Promise.resolve();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => this.authStore.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('HandoffRequested', (evt: HandoffRequestedEvent) =>
      this.handoffRequested$.next(evt));
    this.connection.on('HandoffAccepted', (evt: { sessionId: string; agentName: string }) =>
      this.handoffAccepted$.next(evt));
    this.connection.on('ReceiveMessage', (msg: AgentChatMessage) =>
      this.message$.next(msg));
    this.connection.on('CustomerTyping', () =>
      this.customerTyping$.next());
    this.connection.on('SessionClosed', (evt: { sessionId: string; reason: string }) =>
      this.sessionClosed$.next(evt));

    this.startPromise = this.connection.start();
    return this.startPromise;
  }

  async subscribeToDepartment(departmentId?: string): Promise<void> {
    this.ensureConnected();
    await this.connection!.invoke('SubscribeToDepartment', departmentId ?? null);
  }

  async acceptHandoff(sessionId: string): Promise<void> {
    this.ensureConnected();
    await this.connection!.invoke('AcceptHandoff', sessionId);
  }

  async sendMessage(sessionId: string, body: string): Promise<void> {
    this.ensureConnected();
    await this.connection!.invoke('SendMessage', sessionId, body);
  }

  async agentTyping(sessionId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('AgentTyping', sessionId);
    }
  }

  async closeSession(sessionId: string, reason = 'Resolved'): Promise<void> {
    this.ensureConnected();
    await this.connection!.invoke('CloseSession', sessionId, reason);
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
    this.startPromise = null;
  }

  private ensureConnected(): void {
    if (this.connection?.state !== signalR.HubConnectionState.Connected) {
      throw new Error('AgentChatHub is not connected.');
    }
  }
}
