import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface Notification {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  entityType: 'ticket' | 'article' | 'chat' | string;
  entityId: string;
  createdAt: string;
}

export interface NotificationListParams {
  page?: number;
  pageSize?: number;
  unreadOnly?: boolean;
}

export interface NotificationListResponse {
  items: Notification[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);

  readonly _notifications = signal<Notification[]>([]);
  readonly _unreadCount = signal<number>(0);
  readonly _loading = signal<boolean>(false);

  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly loading = this._loading.asReadonly();

  list(params: NotificationListParams = {}): Observable<NotificationListResponse> {
    let httpParams = new HttpParams();
    if (params.page != null) httpParams = httpParams.set('page', params.page);
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.unreadOnly === true) httpParams = httpParams.set('unreadOnly', 'true');

    this._loading.set(true);
    return this.http
      .get<NotificationListResponse>('/api/v1/notifications', { params: httpParams })
      .pipe(
        tap(res => {
          this._notifications.set(res.items);
          this._loading.set(false);
        })
      );
  }

  markRead(id: string): Observable<void> {
    return this.http.put<void>(`/api/v1/notifications/${id}/read`, {}).pipe(
      tap(() => {
        this._notifications.update(list =>
          list.map(n => (n.id === id ? { ...n, isRead: true } : n))
        );
        this._unreadCount.update(c => Math.max(0, c - 1));
      })
    );
  }

  markAllRead(): Observable<void> {
    return this.http.put<void>('/api/v1/notifications/read-all', {}).pipe(
      tap(() => {
        this._notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this._unreadCount.set(0);
      })
    );
  }

  getUnreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>('/api/v1/notifications/unread-count').pipe(
      tap(res => this._unreadCount.set(res.count))
    );
  }

  pushNotification(notification: Notification): void {
    this._notifications.update(list => [notification, ...list]);
    if (!notification.isRead) {
      this._unreadCount.update(c => c + 1);
    }
  }

  setUnreadCount(count: number): void {
    this._unreadCount.set(count);
  }
}
