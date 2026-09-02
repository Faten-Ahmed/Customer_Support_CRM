import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatBadgeModule } from '@angular/material/badge';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NotificationService } from '../notification.service';
import { NotificationInboxComponent } from '../notification-inbox/notification-inbox.component';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [
    CommonModule,
    MatBadgeModule,
    MatIconModule,
    MatButtonModule,
    NotificationInboxComponent,
  ],
  styles: [`
    :host { position: relative; }
    .inbox-overlay {
      position: fixed;
      top: 64px;
      right: 16px;
      width: 380px;
      max-height: 70vh;
      overflow-y: auto;
      background: white;
      border-radius: 8px;
      box-shadow: 0 4px 20px rgba(0,0,0,0.15);
      z-index: 1000;
    }
  `],
  template: `
    <button
      mat-icon-button
      aria-label="Notifications"
      (click)="toggleInbox()"
      [matBadge]="unreadCount() > 0 ? unreadCount() : null"
      [matBadgeHidden]="!showBadge()"
      matBadgeColor="warn"
      matBadgeSize="small"
    >
      <mat-icon>notifications</mat-icon>
    </button>

    @if (inboxOpen()) {
      <div class="inbox-overlay">
        <app-notification-inbox
          (closePanel)="inboxOpen.set(false)"
        />
      </div>
    }
  `,
})
export class NotificationBellComponent implements OnInit {
  private readonly notifService = inject(NotificationService);

  readonly inboxOpen = signal(false);
  readonly unreadCount = this.notifService.unreadCount;
  readonly showBadge = computed(() => this.notifService.unreadCount() > 0);

  ngOnInit(): void {
    this.notifService.getUnreadCount().subscribe();
  }

  toggleInbox(): void {
    this.inboxOpen.update(v => !v);
  }
}
