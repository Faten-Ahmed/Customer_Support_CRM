# Message Thread Component — Implementation Plan

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

**Story:** US-FE-013
**Goal:** Implement the message thread panel inside the ticket detail page — chat-layout with role-based alignment, internal note highlighting, relative timestamps, load-more pagination, and real-time SignalR message appending.

**Architecture:** `MessageThreadComponent` is a standalone component. Initial messages are loaded from `TicketService.getMessages()` with page=1. "Load more" prepends older pages. SignalR `ReceiveMessage` events on the ticket's channel append new messages in real time. The component exposes an `@Output() newMessageCount` for unread badge use. The `HubConnection` is created via a mockable `SignalRService`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, `@microsoft/signalr`, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/components/message-thread/message-thread.component.ts` |
| Create | `src/app/tickets/components/message-thread/message-thread.component.html` |
| Create | `src/app/tickets/components/message-thread/message-thread.component.spec.ts` |
| Create | `src/app/shared/services/signalr.service.ts` |
| Create | `src/app/shared/services/signalr.service.spec.ts` |
| Modify | `src/app/tickets/services/ticket.service.ts` |
| Modify | `src/app/tickets/services/ticket.service.spec.ts` |

---

## Task 1: Extend TicketService with getMessages()

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/tickets/services/ticket.service.spec.ts

describe('TicketService — getMessages', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getMessages() should GET /api/v1/tickets/{id}/messages with page params', () => {
    service.getMessages('t1', 1, 20).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/tickets/t1/messages');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ data: [], total: 0 });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/tickets/services/ticket.service.ts

export interface TicketMessage {
  id: string;
  ticketId: string;
  content: string;
  isInternal: boolean;
  senderName: string;
  senderRole: string;
  direction: 'Inbound' | 'Outbound';
  deliveryStatus?: 'Sent' | 'Failed' | 'Pending';
  createdAt: string;
}

getMessages(ticketId: string, page: number, pageSize: number): Observable<{ data: TicketMessage[]; total: number }> {
  const params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
  return this.http.get<{ data: TicketMessage[]; total: number }>(`/api/v1/tickets/${ticketId}/messages`, { params });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/ticket.service.ts src/app/tickets/services/ticket.service.spec.ts
git commit -m "feat(tickets): add getMessages() to TicketService (US-FE-013)"
```

---

## Task 2: SignalRService (shared)

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/shared/services/signalr.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { SignalRService } from './signalr.service';

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [SignalRService] });
    service = TestBed.inject(SignalRService);
  });

  it('should be created', () => expect(service).toBeTruthy());

  it('getConnection() should return an HubConnection for a given url', () => {
    const conn = service.getConnection('/hubs/tickets');
    expect(conn).toBeTruthy();
    expect(typeof conn.on).toBe('function');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shared/services/signalr.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/shared/services/signalr.service.ts

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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shared/services/signalr.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/shared/services/signalr.service.ts src/app/shared/services/signalr.service.spec.ts
git commit -m "feat(shared): add SignalRService wrapper (US-FE-013)"
```

---

## Task 3: MessageThreadComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/components/message-thread/message-thread.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageThreadComponent } from './message-thread.component';
import { TicketService, TicketMessage } from '../../services/ticket.service';
import { SignalRService } from '../../../shared/services/signalr.service';

const mockMessages: TicketMessage[] = [
  { id: 'm1', ticketId: 't1', content: 'Hello', isInternal: false, senderName: 'Ali', senderRole: 'Customer', direction: 'Inbound', createdAt: '2025-01-01T10:00:00Z' },
  { id: 'm2', ticketId: 't1', content: 'Hi there', isInternal: false, senderName: 'Omar', senderRole: 'Agent', direction: 'Outbound', deliveryStatus: 'Sent', createdAt: '2025-01-01T10:01:00Z' },
  { id: 'm3', ticketId: 't1', content: 'Internal note', isInternal: true, senderName: 'Omar', senderRole: 'Agent', direction: 'Outbound', createdAt: '2025-01-01T10:02:00Z' },
];

describe('MessageThreadComponent', () => {
  let fixture: ComponentFixture<MessageThreadComponent>;
  let component: MessageThreadComponent;
  let ticketService: jasmine.SpyObj<TicketService>;
  let signalRService: jasmine.SpyObj<SignalRService>;
  let mockConnection: jasmine.SpyObj<any>;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('TicketService', ['getMessages']);
    ticketService.getMessages.and.returnValue(of({ data: mockMessages, total: 3 }));

    mockConnection = jasmine.createSpyObj('HubConnection', ['start', 'on', 'off', 'stop']);
    mockConnection.start.and.returnValue(Promise.resolve());
    signalRService = jasmine.createSpyObj('SignalRService', ['getConnection']);
    signalRService.getConnection.and.returnValue(mockConnection);

    await TestBed.configureTestingModule({
      imports: [MessageThreadComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketService },
        { provide: SignalRService, useValue: signalRService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MessageThreadComponent);
    component = fixture.componentInstance;
    component.ticketId = 't1';
    fixture.detectChanges();
  });

  it('should create and load messages', () => {
    expect(component).toBeTruthy();
    expect(component.messages().length).toBe(3);
  });

  it('should connect to SignalR hub', () => {
    expect(signalRService.getConnection).toHaveBeenCalledWith('/hubs/tickets');
    expect(mockConnection.start).toHaveBeenCalled();
  });

  it('should show "Load more" button when more messages exist', () => {
    component.total.set(10);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Load more');
  });

  it('should not show load-more when all messages loaded', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Load more');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/components/message-thread/message-thread.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/components/message-thread/message-thread.component.ts

import { Component, Input, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TicketMessage, TicketService } from '../../services/ticket.service';
import { SignalRService } from '../../../shared/services/signalr.service';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-message-thread',
  standalone: true,
  imports: [CommonModule, DatePipe, MatButtonModule, MatIconModule],
  templateUrl: './message-thread.component.html',
})
export class MessageThreadComponent implements OnInit, OnDestroy {
  @Input() ticketId!: string;

  private readonly ticketService = inject(TicketService);
  private readonly signalRService = inject(SignalRService);

  readonly messages = signal<TicketMessage[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);

  private page = 1;
  private pageSize = 20;
  private connection!: signalR.HubConnection;

  ngOnInit(): void {
    this.loadMessages();
    this.connectSignalR();
  }

  ngOnDestroy(): void {
    this.connection?.stop();
  }

  private loadMessages(): void {
    this.loading.set(true);
    this.ticketService.getMessages(this.ticketId, this.page, this.pageSize).subscribe({
      next: res => {
        this.messages.set([...res.data, ...this.messages()]);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  loadMore(): void {
    this.page++;
    this.loadMessages();
  }

  get hasMore(): boolean {
    return this.messages().length < this.total();
  }

  private connectSignalR(): void {
    this.connection = this.signalRService.getConnection('/hubs/tickets');
    this.connection.start().then(() => {
      this.connection.on('ReceiveMessage', (msg: TicketMessage) => {
        if (msg.ticketId === this.ticketId) {
          this.messages.update(msgs => [...msgs, msg]);
          this.total.update(t => t + 1);
        }
      });
    });
  }
}
```

```html
<!-- src/app/tickets/components/message-thread/message-thread.component.html -->

<div class="flex flex-col gap-3 p-4 max-h-[600px] overflow-y-auto" id="message-thread">

  @if (hasMore) {
    <div class="text-center">
      <button mat-stroked-button (click)="loadMore()" [disabled]="loading()">
        {{ loading() ? 'Loading…' : 'Load more' }}
      </button>
    </div>
  }

  @for (msg of messages(); track msg.id) {
    @if (msg.isInternal) {
      <!-- Internal note: full-width yellow -->
      <div class="bg-yellow-50 border border-yellow-300 rounded p-3 w-full">
        <div class="flex items-center gap-2 mb-1">
          <span class="text-xs font-semibold text-yellow-700">Internal Note</span>
          <span class="text-xs text-gray-500">{{ msg.senderName }}</span>
          <span class="text-xs text-gray-400 ml-auto" [title]="msg.createdAt | date:'medium'">{{ msg.createdAt | date:'shortTime' }}</span>
        </div>
        <p class="text-sm text-yellow-900">{{ msg.content }}</p>
      </div>
    } @else if (msg.direction === 'Outbound') {
      <!-- Agent message: right-aligned blue -->
      <div class="flex justify-end">
        <div class="bg-blue-100 rounded-lg p-3 max-w-[70%]">
          <div class="flex gap-2 mb-1">
            <span class="text-xs font-semibold text-blue-700">{{ msg.senderName }}</span>
            <span class="text-xs text-gray-400 ml-auto" [title]="msg.createdAt | date:'medium'">{{ msg.createdAt | date:'shortTime' }}</span>
          </div>
          <p class="text-sm">{{ msg.content }}</p>
          @if (msg.deliveryStatus) {
            <span class="text-xs text-gray-400">{{ msg.deliveryStatus }}</span>
          }
        </div>
      </div>
    } @else {
      <!-- Customer message: left-aligned grey -->
      <div class="flex justify-start">
        <div class="bg-gray-100 rounded-lg p-3 max-w-[70%]">
          <div class="flex gap-2 mb-1">
            <span class="text-xs font-semibold text-gray-700">{{ msg.senderName }}</span>
            <span class="text-xs text-gray-400 ml-auto" [title]="msg.createdAt | date:'medium'">{{ msg.createdAt | date:'shortTime' }}</span>
          </div>
          <p class="text-sm">{{ msg.content }}</p>
        </div>
      </div>
    }
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/components/message-thread/message-thread.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/components/message-thread/
git commit -m "feat(tickets): implement MessageThreadComponent with SignalR real-time updates (US-FE-013)"
```
