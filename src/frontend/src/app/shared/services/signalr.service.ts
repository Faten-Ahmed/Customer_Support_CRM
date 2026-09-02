import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthStore } from '../../auth/auth.store';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly authStore = inject(AuthStore);
  private readonly connections = new Map<string, signalR.HubConnection>();

  getConnection(hubUrl: string): signalR.HubConnection {
    if (!this.connections.has(hubUrl)) {
      const conn = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => this.authStore.getToken() ?? '',
        })
        .withAutomaticReconnect()
        .build();
      this.connections.set(hubUrl, conn);
    }
    return this.connections.get(hubUrl)!;
  }
}
