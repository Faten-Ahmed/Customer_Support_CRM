import {
  Component,
  OnInit,
  Output,
  EventEmitter,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDividerModule } from '@angular/material/divider';
import { Router } from '@angular/router';
import { NotificationService, Notification } from '../notification.service';

const ENTITY_ROUTES: Record<string, string> = {
  ticket: '/tickets',
  article: '/kb/articles',
  chat: '/chats',
};

const TYPE_ICONS: Record<string, string> = {
  TicketAssigned: 'assignment_ind',
  TicketStatusChanged: 'update',
  TicketEscalated: 'warning',
  SlaBreached: 'alarm',
  NewChatMessage: 'chat',
  ArticleApproved: 'check_circle',
  ArticleRejected: 'cancel',
  default: 'notifications',
};

@Component({
  selector: 'app-notification-inbox',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatDividerModule,
  ],
  styles: [`
    :host { display: block; }
    .notification-item { padding: 12px 16px; cursor: pointer; }
    .notification-item.unread { background: rgba(var(--mat-primary-rgb), 0.06); }
    .notification-item:hover { background: rgba(0,0,0,0.04); }
    .body-text { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
  `],
  template: `
    <div class="inbox-header" style="display:flex;align-items:center;padding:8px 16px;gap:8px;">
      <h3 style="flex:1;margin:0;">Notifications</h3>
      <mat-slide-toggle [checked]="unreadOnly()" (change)="toggleUnreadOnly()" labelPosition="before">
        Unread only
      </mat-slide-toggle>
      <button mat-button color="primary" (click)="onMarkAllRead()">Mark all as read</button>
      <button mat-icon-button (click)="close()" aria-label="Close">
        <mat-icon>close</mat-icon>
      </button>
    </div>
    <mat-divider />

    @if (notifService.loading()) {
      <div style="display:flex;justify-content:center;padding:32px;">
        <mat-spinner diameter="36" />
      </div>
    }

    @for (n of notifService.notifications(); track n.id) {
      <div
        class="notification-item"
        [class.unread]="!n.isRead"
        (click)="onNotificationClick(n)"
      >
        <div style="display:flex;gap:12px;align-items:flex-start;">
          <mat-icon [color]="n.isRead ? '' : 'primary'">{{ iconFor(n.type) }}</mat-icon>
          <div style="flex:1;min-width:0;">
            <div style="font-weight:500;font-size:14px;">{{ n.title }}</div>
            <div class="body-text" style="font-size:12px;color:#666;">{{ truncate(n.body) }}</div>
            <div style="font-size:11px;color:#999;margin-top:4px;">{{ n.createdAt | date:'short' }}</div>
          </div>
        </div>
      </div>
      <mat-divider />
    }

    @if (hasMore()) {
      <div style="text-align:center;padding:8px;">
        <button mat-button (click)="loadMore()">Load more</button>
      </div>
    }

    @if (!notifService.loading() && notifService.notifications().length === 0) {
      <div style="text-align:center;padding:32px;color:#999;">No notifications</div>
    }
  `,
})
export class NotificationInboxComponent implements OnInit {
  @Output() closePanel = new EventEmitter<void>();

  protected readonly notifService = inject(NotificationService);
  private readonly router = inject(Router);

  readonly unreadOnly = signal(false);
  readonly hasMore = signal(false);
  readonly currentPage = signal(1);
  private readonly pageSize = 20;
  private totalCount = 0;

  ngOnInit(): void {
    this.loadPage(1);
  }

  private loadPage(page: number): void {
    this.currentPage.set(page);
    this.notifService
      .list({ page, pageSize: this.pageSize, unreadOnly: this.unreadOnly() })
      .subscribe(res => {
        this.totalCount = res.totalCount;
        this.hasMore.set(page * this.pageSize < this.totalCount);
      });
  }

  toggleUnreadOnly(): void {
    this.unreadOnly.update(v => !v);
    this.loadPage(1);
  }

  onMarkAllRead(): void {
    this.notifService.markAllRead().subscribe();
  }

  onNotificationClick(n: Notification): void {
    this.notifService.markRead(n.id).subscribe();
    this.router.navigate(this.entityRoute(n.entityType, n.entityId));
  }

  loadMore(): void {
    this.loadPage(this.currentPage() + 1);
  }

  close(): void {
    this.closePanel.emit();
  }

  truncate(text: string, max = 80): string {
    return text.length > max ? text.slice(0, max) + '…' : text;
  }

  iconFor(type: string): string {
    return TYPE_ICONS[type] ?? TYPE_ICONS['default'];
  }

  entityRoute(entityType: string, entityId: string): string[] {
    const base = ENTITY_ROUTES[entityType] ?? '/notifications';
    return [base, entityId];
  }
}
