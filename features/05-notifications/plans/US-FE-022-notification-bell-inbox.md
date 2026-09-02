# Notification Bell & Inbox — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-022
**Goal:** Implement a notification bell icon in the app header showing unread count, a slide-out inbox panel listing notifications newest-first, per-type icons, unread highlighting, mark-as-read on click with entity navigation, "Mark all as read", "Unread only" filter, "Load more" pagination, and real-time updates via SignalR `NotificationHub`.

**Architecture:** `NotificationService` is a singleton provided in root that owns all HTTP calls and exposes Angular Signals for `unreadCount`, `notifications`, and `loading`. `NotificationBellComponent` lives in the app header and owns the slide-out overlay trigger. `NotificationInboxComponent` is the slide-out panel rendered inside an Angular CDK Overlay or `MatSidenav`. SignalR events received from `SignalRService` are piped into `NotificationService` signals so both components react without polling.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/notifications/notification.service.ts` |
| Create | `src/app/notifications/notification.service.spec.ts` |
| Create | `src/app/notifications/notification-bell/notification-bell.component.ts` |
| Create | `src/app/notifications/notification-bell/notification-bell.component.spec.ts` |
| Create | `src/app/notifications/notification-inbox/notification-inbox.component.ts` |
| Create | `src/app/notifications/notification-inbox/notification-inbox.component.spec.ts` |

---

## Task 1: NotificationService — HTTP methods and signals

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/notifications/notification.service.spec.ts
import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { NotificationService, Notification, NotificationListParams } from './notification.service';

const MOCK_NOTIFICATION: Notification = {
  id: 'n1',
  type: 'TicketAssigned',
  title: 'Ticket assigned to you',
  body: 'Ticket #42 has been assigned to your queue.',
  isRead: false,
  entityType: 'ticket',
  entityId: '42',
  createdAt: '2026-08-01T10:00:00Z',
};

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotificationService],
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('list() should GET /api/notifications with params', () => {
    const params: NotificationListParams = { page: 1, pageSize: 20, unreadOnly: true };
    service.list(params).subscribe();
    const req = httpMock.expectOne(r =>
      r.url === '/api/notifications' &&
      r.params.get('page') === '1' &&
      r.params.get('pageSize') === '20' &&
      r.params.get('unreadOnly') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [MOCK_NOTIFICATION], totalCount: 1, page: 1, pageSize: 20 });
  });

  it('list() should populate notifications signal', () => {
    service.list({ page: 1, pageSize: 20 }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/notifications');
    req.flush({ items: [MOCK_NOTIFICATION], totalCount: 1, page: 1, pageSize: 20 });
    expect(service.notifications()).toEqual([MOCK_NOTIFICATION]);
  });

  it('markRead() should PUT /api/notifications/{id}/read', () => {
    service.markRead('n1').subscribe();
    const req = httpMock.expectOne('/api/notifications/n1/read');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('markRead() should update notification isRead signal locally', () => {
    // Pre-populate signal
    service['_notifications'].set([MOCK_NOTIFICATION]);
    service.markRead('n1').subscribe();
    const req = httpMock.expectOne('/api/notifications/n1/read');
    req.flush({});
    const updated = service.notifications().find(n => n.id === 'n1');
    expect(updated?.isRead).toBeTrue();
  });

  it('markAllRead() should PUT /api/notifications/mark-all-read', () => {
    service.markAllRead().subscribe();
    const req = httpMock.expectOne('/api/notifications/mark-all-read');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('markAllRead() should set all notifications isRead to true', () => {
    service['_notifications'].set([MOCK_NOTIFICATION, { ...MOCK_NOTIFICATION, id: 'n2' }]);
    service.markAllRead().subscribe();
    const req = httpMock.expectOne('/api/notifications/mark-all-read');
    req.flush({});
    expect(service.notifications().every(n => n.isRead)).toBeTrue();
    expect(service.unreadCount()).toBe(0);
  });

  it('getUnreadCount() should GET /api/notifications/unread-count and update signal', () => {
    service.getUnreadCount().subscribe();
    const req = httpMock.expectOne('/api/notifications/unread-count');
    expect(req.request.method).toBe('GET');
    req.flush({ count: 7 });
    expect(service.unreadCount()).toBe(7);
  });

  it('pushNotification() should prepend to notifications signal and increment unreadCount', () => {
    service['_unreadCount'].set(2);
    service['_notifications'].set([]);
    service.pushNotification(MOCK_NOTIFICATION);
    expect(service.notifications()[0]).toEqual(MOCK_NOTIFICATION);
    expect(service.unreadCount()).toBe(3);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/notifications/notification.service.spec.ts --watch=false
```

Expected: FAIL — `NotificationService` does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/notifications/notification.service.ts
import { Injectable, signal, computed } from '@angular/core';
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
    if (params.unreadOnly != null) httpParams = httpParams.set('unreadOnly', params.unreadOnly);

    this._loading.set(true);
    return this.http
      .get<NotificationListResponse>('/api/notifications', { params: httpParams })
      .pipe(
        tap(res => {
          this._notifications.set(res.items);
          this._loading.set(false);
        })
      );
  }

  markRead(id: string): Observable<void> {
    return this.http.put<void>(`/api/notifications/${id}/read`, {}).pipe(
      tap(() => {
        this._notifications.update(list =>
          list.map(n => (n.id === id ? { ...n, isRead: true } : n))
        );
        this._unreadCount.update(c => Math.max(0, c - 1));
      })
    );
  }

  markAllRead(): Observable<void> {
    return this.http.put<void>('/api/notifications/mark-all-read', {}).pipe(
      tap(() => {
        this._notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this._unreadCount.set(0);
      })
    );
  }

  getUnreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>('/api/notifications/unread-count').pipe(
      tap(res => this._unreadCount.set(res.count))
    );
  }

  pushNotification(notification: Notification): void {
    this._notifications.update(list => [notification, ...list]);
    if (!notification.isRead) {
      this._unreadCount.update(c => c + 1);
    }
  }
}
```

> Add `import { inject } from '@angular/core';` at the top.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/notifications/notification.service.spec.ts --watch=false
```

Expected: 9 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/notifications/notification.service.ts src/app/notifications/notification.service.spec.ts
git commit -m "feat(notifications): add NotificationService with signals and HTTP methods"
```

---

## Task 2: NotificationBellComponent — badge + panel toggle

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/notifications/notification-bell/notification-bell.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationBellComponent } from './notification-bell.component';
import { NotificationService } from '../notification.service';
import { MatBadgeModule } from '@angular/material/badge';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

describe('NotificationBellComponent', () => {
  let fixture: ComponentFixture<NotificationBellComponent>;
  let component: NotificationBellComponent;
  let notifService: jasmine.SpyObj<NotificationService>;

  const unreadCountSig = signal(0);

  beforeEach(async () => {
    notifService = jasmine.createSpyObj(
      'NotificationService',
      ['getUnreadCount', 'list', 'markRead', 'markAllRead'],
      {
        unreadCount: unreadCountSig.asReadonly(),
        notifications: signal([]).asReadonly(),
        loading: signal(false).asReadonly(),
      }
    );
    notifService.getUnreadCount.and.returnValue({ subscribe: () => {} } as any);

    await TestBed.configureTestingModule({
      imports: [
        NotificationBellComponent,
        MatBadgeModule,
        MatIconModule,
        MatButtonModule,
        NoopAnimationsModule,
        HttpClientTestingModule,
      ],
      providers: [{ provide: NotificationService, useValue: notifService }],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationBellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display bell icon button', () => {
    const btn = fixture.nativeElement.querySelector('button[mat-icon-button]');
    expect(btn).toBeTruthy();
    const icon = btn.querySelector('mat-icon');
    expect(icon.textContent.trim()).toBe('notifications');
  });

  it('should show badge with unreadCount when > 0', () => {
    unreadCountSig.set(5);
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('[matBadge]');
    expect(badge).toBeTruthy();
  });

  it('should hide badge when unreadCount is 0', () => {
    unreadCountSig.set(0);
    fixture.detectChanges();
    expect(component.showBadge()).toBeFalse();
  });

  it('toggleInbox() should flip inboxOpen signal', () => {
    expect(component.inboxOpen()).toBeFalse();
    component.toggleInbox();
    expect(component.inboxOpen()).toBeTrue();
    component.toggleInbox();
    expect(component.inboxOpen()).toBeFalse();
  });

  it('should call getUnreadCount on init', () => {
    expect(notifService.getUnreadCount).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/notifications/notification-bell/notification-bell.component.spec.ts --watch=false
```

Expected: FAIL — component does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/notifications/notification-bell/notification-bell.component.ts
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
  template: `
    <button
      mat-icon-button
      aria-label="Notifications"
      (click)="toggleInbox()"
      [matBadge]="unreadCount() || null"
      [matBadgeHidden]="!showBadge()"
      matBadgeColor="warn"
      matBadgeSize="small"
    >
      <mat-icon>notifications</mat-icon>
    </button>

    @if (inboxOpen()) {
      <app-notification-inbox
        (closePanel)="inboxOpen.set(false)"
      />
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/notifications/notification-bell/notification-bell.component.spec.ts --watch=false
```

Expected: 6 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/notifications/notification-bell/
git commit -m "feat(notifications): add NotificationBellComponent with badge and inbox toggle"
```

---

## Task 3: NotificationInboxComponent — panel, filter, mark-as-read, load more

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/notifications/notification-inbox/notification-inbox.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NotificationInboxComponent } from './notification-inbox.component';
import { NotificationService, Notification } from '../notification.service';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';

const makeNotif = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'n1',
  type: 'TicketAssigned',
  title: 'Ticket assigned',
  body: 'You have a new ticket',
  isRead: false,
  entityType: 'ticket',
  entityId: '42',
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe('NotificationInboxComponent', () => {
  let fixture: ComponentFixture<NotificationInboxComponent>;
  let component: NotificationInboxComponent;
  let notifService: jasmine.SpyObj<NotificationService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const notifsSig = signal<Notification[]>([]);
  const loadingSig = signal(false);

  beforeEach(async () => {
    notifService = jasmine.createSpyObj(
      'NotificationService',
      ['list', 'markRead', 'markAllRead'],
      {
        notifications: notifsSig.asReadonly(),
        loading: loadingSig.asReadonly(),
        unreadCount: signal(0).asReadonly(),
      }
    );
    notifService.list.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
    notifService.markRead.and.returnValue(of(undefined));
    notifService.markAllRead.and.returnValue(of(undefined));

    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [NotificationInboxComponent, NoopAnimationsModule],
      providers: [
        { provide: NotificationService, useValue: notifService },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationInboxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call list() on init', () => {
    expect(notifService.list).toHaveBeenCalledWith({ page: 1, pageSize: 20, unreadOnly: false });
  });

  it('should render notification items from signal', () => {
    notifsSig.set([makeNotif(), makeNotif({ id: 'n2', title: 'Second' })]);
    fixture.detectChanges();
    const items = fixture.nativeElement.querySelectorAll('.notification-item');
    expect(items.length).toBe(2);
  });

  it('should highlight unread notifications', () => {
    notifsSig.set([makeNotif({ isRead: false })]);
    fixture.detectChanges();
    const item = fixture.nativeElement.querySelector('.notification-item');
    expect(item.classList).toContain('unread');
  });

  it('onNotificationClick() should call markRead and navigate', fakeAsync(() => {
    const notif = makeNotif();
    component.onNotificationClick(notif);
    tick();
    expect(notifService.markRead).toHaveBeenCalledWith('n1');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/tickets', '42']);
  }));

  it('onMarkAllRead() should call markAllRead()', () => {
    component.onMarkAllRead();
    expect(notifService.markAllRead).toHaveBeenCalled();
  });

  it('toggleUnreadOnly() should reload list with unreadOnly=true', () => {
    component.toggleUnreadOnly();
    expect(component.unreadOnly()).toBeTrue();
    expect(notifService.list).toHaveBeenCalledWith({ page: 1, pageSize: 20, unreadOnly: true });
  });

  it('loadMore() should request next page and append', () => {
    component['currentPage'].set(1);
    component.loadMore();
    expect(notifService.list).toHaveBeenCalledWith({
      page: 2,
      pageSize: 20,
      unreadOnly: component.unreadOnly(),
    });
  });

  it('should emit closePanel when close button clicked', () => {
    spyOn(component.closePanel, 'emit');
    component.close();
    expect(component.closePanel.emit).toHaveBeenCalled();
  });

  it('should truncate body longer than 80 characters', () => {
    const longBody = 'a'.repeat(100);
    expect(component.truncate(longBody)).toBe('a'.repeat(80) + '…');
  });

  it('should return correct navigation route for each entity type', () => {
    expect(component.entityRoute('ticket', '5')).toEqual(['/tickets', '5']);
    expect(component.entityRoute('article', '9')).toEqual(['/kb/articles', '9']);
    expect(component.entityRoute('chat', '3')).toEqual(['/chats', '3']);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/notifications/notification-inbox/notification-inbox.component.spec.ts --watch=false
```

Expected: FAIL — component does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/notifications/notification-inbox/notification-inbox.component.ts
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
  private readonly currentPage = signal(1);
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/notifications/notification-inbox/notification-inbox.component.spec.ts --watch=false
```

Expected: 11 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/notifications/notification-inbox/
git commit -m "feat(notifications): add NotificationInboxComponent with filter, load-more, and mark-as-read"
```

---

## Task 4: Wire SignalR events into NotificationService

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/notifications/notification.service.spec.ts

describe('NotificationService — SignalR integration', () => {
  let service: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotificationService],
    });
    service = TestBed.inject(NotificationService);
  });

  it('pushNotification() with unread should increment unreadCount', () => {
    service['_unreadCount'].set(3);
    service.pushNotification({
      id: 'nx', type: 'TicketAssigned', title: 'T', body: 'B',
      isRead: false, entityType: 'ticket', entityId: '1', createdAt: new Date().toISOString(),
    });
    expect(service.unreadCount()).toBe(4);
    expect(service.notifications()[0].id).toBe('nx');
  });

  it('pushNotification() already-read should not increment unreadCount', () => {
    service['_unreadCount'].set(3);
    service.pushNotification({
      id: 'nx', type: 'TicketAssigned', title: 'T', body: 'B',
      isRead: true, entityType: 'ticket', entityId: '1', createdAt: new Date().toISOString(),
    });
    expect(service.unreadCount()).toBe(3);
  });

  it('setUnreadCount() should update signal directly', () => {
    service.setUnreadCount(12);
    expect(service.unreadCount()).toBe(12);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/notifications/notification.service.spec.ts --watch=false
```

Expected: FAIL — `setUnreadCount` does not exist.

- [ ] **Step 3: Implement**

```typescript
// Add to NotificationService class in notification.service.ts:

setUnreadCount(count: number): void {
  this._unreadCount.set(count);
}
```

> `SignalRService` (US-FE-023) will call `notificationService.pushNotification(payload)` and `notificationService.setUnreadCount(count)` when `ReceiveNotification` and `UnreadCountUpdated` events fire.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/notifications/notification.service.spec.ts --watch=false
```

Expected: 12 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/notifications/notification.service.ts src/app/notifications/notification.service.spec.ts
git commit -m "feat(notifications): expose setUnreadCount for SignalR event bridging"
```
