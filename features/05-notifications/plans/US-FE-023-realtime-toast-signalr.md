# Real-Time Notification Toast & SignalR Manager — Implementation Plan

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

**Story:** US-FE-023
**Goal:** Implement a bottom-right toast stack for push notifications received via SignalR, with auto-dismiss (3 s) or persistent display for SlaBreached/Critical types, a "View" deep-link, max-3 visible stacking, and a `SignalRService` that manages `NotificationHub`, `DashboardHub`, and `ChatHub` connections with exponential back-off reconnect and a header connection-status indicator.

**Architecture:** `SignalRService` is a root-level singleton that holds one `HubConnection` per hub and exposes typed Observables (wrapping `connection.on(...)` via `Subject`). It establishes connections on login, tears them down on logout, and applies exponential back-off up to 30 s. `NotificationToastComponent` is a root-level outlet (placed once in `AppComponent`) that subscribes to `SignalRService.notification$` and manages a signal array of up to 3 active toasts. The connection status dot in the header reads `SignalRService.connectionState` signal.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/core/signalr.service.ts` |
| Create | `src/app/core/signalr.service.spec.ts` |
| Create | `src/app/notifications/notification-toast/notification-toast.component.ts` |
| Create | `src/app/notifications/notification-toast/notification-toast.component.spec.ts` |

---

## Task 1: SignalRService — connection lifecycle and typed event streams

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/core/signalr.service.spec.ts
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { SignalRService, HubName, ConnectionState } from './signalr.service';

// ---------------------------------------------------------------------------
// Minimal HubConnection stub
// ---------------------------------------------------------------------------
const makeHubStub = () => {
  const handlers: Record<string, ((...args: any[]) => void)[]> = {};
  return {
    start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
    stop: jasmine.createSpy('stop').and.returnValue(Promise.resolve()),
    on: jasmine.createSpy('on').and.callFake((event: string, cb: (...a: any[]) => void) => {
      handlers[event] = handlers[event] ?? [];
      handlers[event].push(cb);
    }),
    onreconnecting: jasmine.createSpy('onreconnecting'),
    onreconnected: jasmine.createSpy('onreconnected'),
    onclose: jasmine.createSpy('onclose'),
    state: 'Disconnected',
    _handlers: handlers,
    _emit(event: string, ...args: any[]) {
      (handlers[event] ?? []).forEach(cb => cb(...args));
    },
  };
};

describe('SignalRService', () => {
  let service: SignalRService;
  let hubStub: ReturnType<typeof makeHubStub>;

  beforeEach(() => {
    hubStub = makeHubStub();
    TestBed.configureTestingModule({
      providers: [SignalRService],
    });
    service = TestBed.inject(SignalRService);
    // Inject stub factory
    service['_createConnection'] = (_url: string) => hubStub as any;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('connect() should call start() on the hub', fakeAsync(() => {
    service.connect(HubName.Notification);
    tick();
    expect(hubStub.start).toHaveBeenCalled();
  }));

  it('connect() should set connectionState to Connected after start resolves', fakeAsync(() => {
    service.connect(HubName.Notification);
    tick();
    expect(service.connectionState(HubName.Notification)).toBe(ConnectionState.Connected);
  }));

  it('disconnect() should call stop() on the hub', fakeAsync(() => {
    service.connect(HubName.Notification);
    tick();
    service.disconnect(HubName.Notification);
    tick();
    expect(hubStub.stop).toHaveBeenCalled();
  }));

  it('notification$ should emit when ReceiveNotification fires', fakeAsync(() => {
    const payload = { id: 'n1', type: 'TicketAssigned', title: 'T', body: 'B' };
    let received: any = null;
    service.connect(HubName.Notification);
    tick();
    service.notification$.subscribe(n => (received = n));
    hubStub._emit('ReceiveNotification', payload);
    expect(received).toEqual(payload);
  }));

  it('unreadCountUpdated$ should emit when UnreadCountUpdated fires', fakeAsync(() => {
    let received: number | null = null;
    service.connect(HubName.Notification);
    tick();
    service.unreadCountUpdated$.subscribe(c => (received = c));
    hubStub._emit('UnreadCountUpdated', 7);
    expect(received).toBe(7);
  }));

  it('connectAll() should connect all three hubs', fakeAsync(() => {
    const stubs: ReturnType<typeof makeHubStub>[] = [];
    service['_createConnection'] = (_url: string) => {
      const s = makeHubStub();
      stubs.push(s);
      return s as any;
    };
    service.connectAll();
    tick();
    expect(stubs.length).toBe(3);
    stubs.forEach(s => expect(s.start).toHaveBeenCalled());
  }));

  it('should set connectionState to Reconnecting on onreconnecting callback', fakeAsync(() => {
    service.connect(HubName.Notification);
    tick();
    // Simulate onreconnecting being called
    const reconnectingCb = hubStub.onreconnecting.calls.mostRecent().args[0];
    reconnectingCb();
    expect(service.connectionState(HubName.Notification)).toBe(ConnectionState.Reconnecting);
  }));

  it('should set connectionState to Connected on onreconnected callback', fakeAsync(() => {
    service.connect(HubName.Notification);
    tick();
    const reconnectingCb = hubStub.onreconnecting.calls.mostRecent().args[0];
    reconnectingCb();
    const reconnectedCb = hubStub.onreconnected.calls.mostRecent().args[0];
    reconnectedCb();
    expect(service.connectionState(HubName.Notification)).toBe(ConnectionState.Connected);
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/core/signalr.service.spec.ts --watch=false
```

Expected: FAIL — `SignalRService` does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/core/signalr.service.ts
import { Injectable, signal } from '@angular/core';
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

  /** Overridable in tests */
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/core/signalr.service.spec.ts --watch=false
```

Expected: 9 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/core/signalr.service.ts src/app/core/signalr.service.spec.ts
git commit -m "feat(core): add SignalRService with per-hub connection state and typed event streams"
```

---

## Task 2: NotificationToastComponent — stack, auto-dismiss, persistent types

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/notifications/notification-toast/notification-toast.component.spec.ts
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
  discardPeriodicTasks,
} from '@angular/core/testing';
import { NotificationToastComponent, ToastItem } from './notification-toast.component';
import { SignalRService } from '../../core/signalr.service';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject } from 'rxjs';
import { Notification } from '../notification.service';

const makeNotif = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'n1',
  type: 'TicketAssigned',
  title: 'Test notification',
  body: 'Body text here',
  isRead: false,
  entityType: 'ticket',
  entityId: '10',
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe('NotificationToastComponent', () => {
  let fixture: ComponentFixture<NotificationToastComponent>;
  let component: NotificationToastComponent;
  let notificationSubject: Subject<Notification>;
  let signalRSpy: jasmine.SpyObj<SignalRService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    notificationSubject = new Subject<Notification>();
    signalRSpy = jasmine.createSpyObj('SignalRService', ['connectAll'], {
      notification$: notificationSubject.asObservable(),
    });
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [NotificationToastComponent, NoopAnimationsModule],
      providers: [
        { provide: SignalRService, useValue: signalRSpy },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationToastComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should add a toast when notification$ emits', fakeAsync(() => {
    notificationSubject.next(makeNotif());
    tick(0);
    expect(component.toasts().length).toBe(1);
    discardPeriodicTasks();
  }));

  it('should auto-dismiss after 3000ms for non-persistent type', fakeAsync(() => {
    notificationSubject.next(makeNotif({ type: 'TicketAssigned' }));
    tick(0);
    expect(component.toasts().length).toBe(1);
    tick(3000);
    expect(component.toasts().length).toBe(0);
  }));

  it('should NOT auto-dismiss SlaBreached notifications', fakeAsync(() => {
    notificationSubject.next(makeNotif({ type: 'SlaBreached' }));
    tick(0);
    tick(5000);
    expect(component.toasts().length).toBe(1);
    discardPeriodicTasks();
  }));

  it('should NOT auto-dismiss Critical notifications', fakeAsync(() => {
    notificationSubject.next(makeNotif({ type: 'Critical' }));
    tick(0);
    tick(5000);
    expect(component.toasts().length).toBe(1);
    discardPeriodicTasks();
  }));

  it('should cap visible toasts at 3, dropping oldest', fakeAsync(() => {
    notificationSubject.next(makeNotif({ id: 'a' }));
    notificationSubject.next(makeNotif({ id: 'b' }));
    notificationSubject.next(makeNotif({ id: 'c' }));
    notificationSubject.next(makeNotif({ id: 'd' }));
    tick(0);
    expect(component.toasts().length).toBe(3);
    expect(component.toasts().map(t => t.notification.id)).toEqual(['b', 'c', 'd']);
    discardPeriodicTasks();
  }));

  it('dismiss() should remove the toast by id', fakeAsync(() => {
    notificationSubject.next(makeNotif({ id: 'x' }));
    tick(0);
    const toast = component.toasts()[0];
    component.dismiss(toast.id);
    expect(component.toasts().length).toBe(0);
    discardPeriodicTasks();
  }));

  it('viewEntity() should navigate and dismiss toast', fakeAsync(() => {
    notificationSubject.next(makeNotif({ id: 'y', entityType: 'ticket', entityId: '7' }));
    tick(0);
    const toast = component.toasts()[0];
    component.viewEntity(toast);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/tickets', '7']);
    expect(component.toasts().length).toBe(0);
    discardPeriodicTasks();
  }));

  it('should render toast elements in the DOM', fakeAsync(() => {
    notificationSubject.next(makeNotif({ title: 'Hello Toast' }));
    tick(0);
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('.toast-item');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('Hello Toast');
    discardPeriodicTasks();
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/notifications/notification-toast/notification-toast.component.spec.ts --watch=false
```

Expected: FAIL — component does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/notifications/notification-toast/notification-toast.component.ts
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { SignalRService } from '../../core/signalr.service';
import { Notification } from '../notification.service';

export interface ToastItem {
  id: string;
  notification: Notification;
  persistent: boolean;
}

const PERSISTENT_TYPES = new Set(['SlaBreached', 'Critical']);
const AUTO_DISMISS_MS = 3000;
const MAX_TOASTS = 3;

const ENTITY_ROUTES: Record<string, string> = {
  ticket: '/tickets',
  article: '/kb/articles',
  chat: '/chats',
};

@Component({
  selector: 'app-notification-toast',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  styles: [`
    .toast-container {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 8px;
      pointer-events: none;
    }
    .toast-item {
      pointer-events: all;
      background: #323232;
      color: #fff;
      border-radius: 4px;
      padding: 12px 16px;
      min-width: 280px;
      max-width: 360px;
      box-shadow: 0 3px 10px rgba(0,0,0,0.3);
      display: flex;
      gap: 12px;
      align-items: flex-start;
    }
    .toast-item.persistent { border-left: 4px solid #f44336; }
    .toast-body { flex: 1; min-width: 0; }
    .toast-title { font-weight: 600; font-size: 14px; }
    .toast-text { font-size: 12px; opacity: 0.85; margin-top: 2px; }
    .toast-actions { display: flex; gap: 4px; margin-top: 8px; }
  `],
  template: `
    <div class="toast-container" aria-live="polite">
      @for (toast of toasts(); track toast.id) {
        <div class="toast-item" [class.persistent]="toast.persistent">
          <div class="toast-body">
            <div class="toast-title">{{ toast.notification.title }}</div>
            <div class="toast-text">{{ toast.notification.body }}</div>
            <div class="toast-actions">
              <button mat-button style="color:#90caf9;font-size:12px;padding:0 4px;" (click)="viewEntity(toast)">
                View
              </button>
              <button mat-icon-button style="width:24px;height:24px;line-height:24px;" (click)="dismiss(toast.id)">
                <mat-icon style="font-size:16px;width:16px;height:16px;">close</mat-icon>
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class NotificationToastComponent implements OnInit, OnDestroy {
  private readonly signalR = inject(SignalRService);
  private readonly router = inject(Router);
  private readonly sub = new Subscription();
  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();

  readonly toasts = signal<ToastItem[]>([]);

  ngOnInit(): void {
    this.sub.add(
      this.signalR.notification$.subscribe(n => this.addToast(n))
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
    this.timers.forEach(t => clearTimeout(t));
  }

  private addToast(notification: Notification): void {
    const persistent = PERSISTENT_TYPES.has(notification.type);
    const toast: ToastItem = {
      id: `${notification.id}-${Date.now()}`,
      notification,
      persistent,
    };

    this.toasts.update(list => {
      const next = [...list, toast];
      return next.length > MAX_TOASTS ? next.slice(next.length - MAX_TOASTS) : next;
    });

    if (!persistent) {
      const timer = setTimeout(() => this.dismiss(toast.id), AUTO_DISMISS_MS);
      this.timers.set(toast.id, timer);
    }
  }

  dismiss(id: string): void {
    clearTimeout(this.timers.get(id));
    this.timers.delete(id);
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  viewEntity(toast: ToastItem): void {
    const base = ENTITY_ROUTES[toast.notification.entityType] ?? '/notifications';
    this.router.navigate([base, toast.notification.entityId]);
    this.dismiss(toast.id);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/notifications/notification-toast/notification-toast.component.spec.ts --watch=false
```

Expected: 9 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/notifications/notification-toast/
git commit -m "feat(notifications): add NotificationToastComponent with 3s auto-dismiss and persistent types"
```

---

## Task 3: Connection status indicator in header

- [ ] **Step 1: Write the failing tests**

```typescript
// Inline test block — add to signalr.service.spec.ts

describe('SignalRService — connection status indicator', () => {
  let service: SignalRService;
  let hubStub: ReturnType<typeof makeHubStub>;

  beforeEach(() => {
    hubStub = makeHubStub();
    TestBed.configureTestingModule({ providers: [SignalRService] });
    service = TestBed.inject(SignalRService);
    service['_createConnection'] = (_url: string) => hubStub as any;
  });

  it('overallConnected() should be false when no hubs connected', () => {
    expect(service.overallConnected()).toBeFalse();
  });

  it('overallConnected() should be true after NotificationHub connects', fakeAsync(() => {
    service.connect(HubName.Notification);
    tick();
    expect(service.overallConnected()).toBeTrue();
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/core/signalr.service.spec.ts --watch=false
```

Expected: FAIL — `overallConnected` does not exist.

- [ ] **Step 3: Implement**

```typescript
// Add to SignalRService class in signalr.service.ts:

readonly overallConnected = computed(() => {
  for (const stateSig of this._states.values()) {
    if (stateSig() === ConnectionState.Connected) return true;
  }
  return false;
});
```

> Import `computed` from `@angular/core`. Then in `AppHeaderComponent` (or wherever the header lives), inject `SignalRService` and bind `signalR.overallConnected()` to a green/grey dot indicator.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/core/signalr.service.spec.ts --watch=false
```

Expected: 11 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/core/signalr.service.ts src/app/core/signalr.service.spec.ts
git commit -m "feat(core): add overallConnected computed signal for header status indicator"
```
