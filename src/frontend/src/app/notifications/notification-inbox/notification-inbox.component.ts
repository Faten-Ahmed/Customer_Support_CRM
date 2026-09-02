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

const APP_ROUTES: Record<string, string> = {
  ticket: '/app/tickets',
  article: '/app/kb/articles',
  chat: '/app/chats',
};

const PORTAL_ROUTES: Record<string, string> = {
  ticket: '/portal/tickets',
  article: '/portal/kb',
  chat: '/portal/chats',
  CsatSurvey: '/portal/surveys',
};

const TYPE_ICONS: Record<string, string> = {
  TicketAssigned: 'assignment_ind',
  TicketStatusChanged: 'update',
  TicketEscalated: 'warning',
  SlaBreached: 'alarm',
  NewChatMessage: 'chat',
  ArticleApproved: 'check_circle',
  ArticleRejected: 'cancel',
  SurveyAvailable: 'star_rate',
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

    .inbox-header {
      padding: 12px 16px 0;
    }
    .inbox-title-row {
      display: flex;
      align-items: center;
      margin-bottom: 8px;
    }
    .inbox-title {
      flex: 1;
      margin: 0;
      font-size: 16px;
      font-weight: 600;
      color: #212121;
    }
    .inbox-actions-row {
      display: flex;
      align-items: center;
      padding-bottom: 8px;
      gap: 4px;
    }
    .spacer { flex: 1; }

    .notification-item {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      padding: 12px 16px;
      cursor: pointer;
      transition: background 0.15s;
    }
    .notification-item:hover { background: rgba(0,0,0,0.04); }
    .notification-item.unread { background: #e8f0fe; }
    .notification-item.unread:hover { background: #dce8fd; }

    .notif-icon-wrap {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background: #e8eaf6;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .notif-icon-wrap.unread { background: #c5cae9; }
    .notif-icon-wrap mat-icon { font-size: 20px; width: 20px; height: 20px; color: #3f51b5; }

    .notif-content { flex: 1; min-width: 0; }
    .notif-title {
      font-weight: 500;
      font-size: 13px;
      color: #212121;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .notif-body {
      font-size: 12px;
      color: #616161;
      margin-top: 2px;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .notif-time { font-size: 11px; color: #9e9e9e; margin-top: 4px; }

    .unread-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #3f51b5;
      flex-shrink: 0;
      margin-top: 6px;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 40px 16px;
      color: #9e9e9e;
    }
    .empty-state mat-icon { font-size: 48px; width: 48px; height: 48px; margin-bottom: 12px; opacity: 0.4; }
    .empty-state p { margin: 0; font-size: 14px; }

    .load-more-row { text-align: center; padding: 8px 0; }
  `],
  template: `
    <div class="inbox-header">
      <div class="inbox-title-row">
        <h3 class="inbox-title">Notifications</h3>
        <button mat-icon-button (click)="close()" aria-label="Close">
          <mat-icon>close</mat-icon>
        </button>
      </div>
      <div class="inbox-actions-row">
        <mat-slide-toggle
          [checked]="unreadOnly()"
          (change)="toggleUnreadOnly()"
          labelPosition="after"
          style="font-size:13px;">
          Unread only
        </mat-slide-toggle>
        <span class="spacer"></span>
        <button mat-button color="primary" style="font-size:12px;" (click)="onMarkAllRead()">
          Mark all read
        </button>
      </div>
    </div>
    <mat-divider />

    @if (notifService.loading()) {
      <div style="display:flex;justify-content:center;padding:32px;">
        <mat-spinner diameter="32" />
      </div>
    }

    @for (n of notifService.notifications(); track n.id) {
      <div
        class="notification-item"
        [class.unread]="!n.isRead"
        (click)="onNotificationClick(n)"
      >
        <div class="notif-icon-wrap" [class.unread]="!n.isRead">
          <mat-icon>{{ iconFor(n.type) }}</mat-icon>
        </div>
        <div class="notif-content">
          <div class="notif-title">{{ n.title }}</div>
          <div class="notif-body">{{ n.body }}</div>
          <div class="notif-time">{{ n.createdAt | date:'MMM d, h:mm a' }}</div>
        </div>
        @if (!n.isRead) {
          <div class="unread-dot"></div>
        }
      </div>
      <mat-divider />
    }

    @if (hasMore()) {
      <div class="load-more-row">
        <button mat-button color="primary" (click)="loadMore()">Load more</button>
      </div>
    }

    @if (!notifService.loading() && notifService.notifications().length === 0) {
      <div class="empty-state">
        <mat-icon>notifications_none</mat-icon>
        <p>No notifications</p>
      </div>
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
    this.notifService.markAllRead().subscribe(() => {
      if (this.unreadOnly()) this.loadPage(1);
    });
  }

  onNotificationClick(n: Notification): void {
    this.notifService.markRead(n.id).subscribe();
    const route = this.entityRoute(n.entityType, n.entityId);
    if (route) {
      this.router.navigate(route);
      this.closePanel.emit();
    }
  }

  loadMore(): void {
    this.loadPage(this.currentPage() + 1);
  }

  close(): void {
    this.closePanel.emit();
  }

  iconFor(type: string): string {
    return TYPE_ICONS[type] ?? TYPE_ICONS['default'];
  }

  entityRoute(entityType: string, entityId: string): string[] | null {
    const routes = this.router.url.startsWith('/portal') ? PORTAL_ROUTES : APP_ROUTES;
    const base = routes[entityType];
    return base ? [base, entityId] : null;
  }
}
