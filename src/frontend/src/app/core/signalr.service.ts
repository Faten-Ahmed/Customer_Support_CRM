import { Injectable, signal, computed } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { Notification } from '../notifications/notification.service';

export enum HubName {
  Notification = 'NotificationHub',
  Dashboard = 'DashboardHub',
  Chat = 'ChatHub',
}

export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting',
}

const HUB_URLS: Record<HubName, string> = {
  [HubName.Notification]: '/hubs/notifications',
  [HubName.Dashboard]: '/hubs/dashboard',
  [HubName.Chat]: '/hubs/chat',
};

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly connections = new Map<HubName, signalR.HubConnection>();
  private readonly _states = new Map<HubName, ReturnType<typeof signal<ConnectionState>>>();

  private readonly _notification$ = new Subject<Notification>();
  private readonly _unreadCountUpdated$ = new Subject<number>();

  readonly notification$: Observable<Notification> = this._notification$.asObservable();
  readonly unreadCountUpdated$: Observable<number> = this._unreadCountUpdated$.asObservable();

  readonly overallConnected = computed(() => {
    for (const stateSig of this._states.values()) {
      if (stateSig() === ConnectionState.Connected) return true;
    }
    return false;
  });

  protected _createConnection(url: string): signalR.HubConnection {
    return new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => localStorage.getItem('access_token') ?? '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: ctx => Math.min(1000 * 2 ** ctx.previousRetryCount, 30_000),
      })
      .build();
  }

  connectionState(hub: HubName): ConnectionState {
    return this._states.get(hub)?.() ?? ConnectionState.Disconnected;
  }

  connect(hub: HubName): void {
    if (this.connections.has(hub)) return;

    const stateSignal = signal<ConnectionState>(ConnectionState.Connecting);
    this._states.set(hub, stateSignal);

    const conn = this._createConnection(HUB_URLS[hub]);
    this.connections.set(hub, conn);

    conn.onreconnecting(() => stateSignal.set(ConnectionState.Reconnecting));
    conn.onreconnected(() => stateSignal.set(ConnectionState.Connected));
    conn.onclose(() => stateSignal.set(ConnectionState.Disconnected));

    if (hub === HubName.Notification) {
      conn.on('ReceiveNotification', (payload: Notification) => this._notification$.next(payload));
      conn.on('UnreadCountUpdated', (count: number) => this._unreadCountUpdated$.next(count));
    }

    conn.start().then(() => stateSignal.set(ConnectionState.Connected));
  }

  connectAll(): void {
    Object.values(HubName).forEach(hub => this.connect(hub as HubName));
  }

  disconnect(hub: HubName): void {
    const conn = this.connections.get(hub);
    if (!conn) return;
    conn.stop().then(() => {
      this.connections.delete(hub);
      this._states.get(hub)?.set(ConnectionState.Disconnected);
    });
  }

  disconnectAll(): void {
    Object.values(HubName).forEach(hub => this.disconnect(hub as HubName));
  }
}
