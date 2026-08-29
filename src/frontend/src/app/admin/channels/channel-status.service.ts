import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChannelStatus {
  channelName: string;
  isConnected: boolean;
  lastActivityAt?: string;
  errorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class ChannelStatusService {
  private readonly http = inject(HttpClient);

  getStatus(): Observable<{ data: ChannelStatus[] }> {
    return this.http.get<{ data: ChannelStatus[] }>('/api/v1/admin/channels/status');
  }
}
