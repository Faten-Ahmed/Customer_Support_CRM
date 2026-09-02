import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { ChannelStatusService, ChannelStatus } from './channel-status.service';

@Component({
  selector: 'app-channel-status',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, MatButtonModule],
  template: `
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
      <h2 style="margin:0;">Channel Status</h2>
      <button mat-stroked-button (click)="load()">
        <mat-icon>refresh</mat-icon> Refresh
      </button>
    </div>
    @if (loading()) {
      <div style="display:flex;justify-content:center;padding:40px;">
        <mat-spinner diameter="40" />
      </div>
    } @else {
      <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:16px;">
        @for (ch of channels(); track ch.channelName) {
          <mat-card [style.border-left]="'4px solid ' + (ch.isConnected ? '#4caf50' : '#f44336')">
            <mat-card-content style="padding-top:16px;">
              <div style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                <mat-icon [style.color]="ch.isConnected ? '#4caf50' : '#f44336'">
                  {{ ch.isConnected ? 'check_circle' : 'error' }}
                </mat-icon>
                <strong>{{ ch.channelName }}</strong>
              </div>
              <p style="margin:4px 0;font-size:13px;color:#555;">
                Status: <span [style.color]="ch.isConnected ? '#4caf50' : '#f44336'">
                  {{ ch.isConnected ? 'Connected' : 'Disconnected' }}
                </span>
              </p>
              @if (ch.lastActivityAt) {
                <p style="margin:4px 0;font-size:12px;color:#888;">
                  Last activity: {{ ch.lastActivityAt | date:'medium' }}
                </p>
              }
              @if (ch.errorMessage) {
                <p style="margin:4px 0;font-size:12px;color:#f44336;">
                  {{ ch.errorMessage }}
                </p>
              }
            </mat-card-content>
          </mat-card>
        }
        @if (channels().length === 0) {
          <p style="color:#666;">No channel data available.</p>
        }
      </div>
    }
  `,
})
export class ChannelStatusComponent implements OnInit {
  private readonly channelService = inject(ChannelStatusService);

  readonly channels = signal<ChannelStatus[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.channelService.getStatus().subscribe({
      next: res => { this.channels.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
