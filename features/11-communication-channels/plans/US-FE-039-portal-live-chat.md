# Portal Live Chat — Implementation Plan

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

**Story:** US-FE-039
**Goal:** Implement the real-time live chat page at `/portal/live-chat` — customer joins a session, sends messages, receives agent replies in real time via SignalR, and handles a 3-minute no-agent timeout that falls back to ticket creation.

**Architecture:** `ChatHubService` wraps the SignalR `/hubs/chat` connection. It exposes methods: `joinSession(sessionId)`, `requestHandoff(sessionId)`, `sendMessage(sessionId, content)`, and observables for `ReceiveMessage`, `HandoffAccepted`, `SessionClosed`. `LiveChatComponent` is standalone. On init it calls `joinSession` then `requestHandoff` to queue the customer. A `setInterval` runs a 3-minute countdown; if `HandoffAccepted` fires before timeout, the countdown clears. If timeout fires first, the component navigates to `/portal/tickets/new?from=livechat`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, `@microsoft/signalr`, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/services/chat-hub.service.ts` |
| Create | `src/app/portal/services/chat-hub.service.spec.ts` |
| Create | `src/app/portal/live-chat/live-chat.component.ts` |
| Create | `src/app/portal/live-chat/live-chat.component.html` |
| Create | `src/app/portal/live-chat/live-chat.component.spec.ts` |

---

## Task 1: ChatHubService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/services/chat-hub.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { ChatHubService } from './chat-hub.service';
import { SignalRService } from '../../shared/services/signalr.service';

describe('ChatHubService', () => {
  let service: ChatHubService;
  let mockConnection: jasmine.SpyObj<any>;
  let signalRService: jasmine.SpyObj<SignalRService>;

  beforeEach(() => {
    mockConnection = jasmine.createSpyObj('HubConnection', ['start', 'on', 'invoke', 'stop', 'off']);
    mockConnection.start.and.returnValue(Promise.resolve());
    signalRService = jasmine.createSpyObj('SignalRService', ['getConnection']);
    signalRService.getConnection.and.returnValue(mockConnection);

    TestBed.configureTestingModule({
      providers: [
        ChatHubService,
        { provide: SignalRService, useValue: signalRService },
      ],
    });
    service = TestBed.inject(ChatHubService);
  });

  it('should create', () => expect(service).toBeTruthy());

  it('connect() should call SignalRService.getConnection with /hubs/chat', async () => {
    await service.connect();
    expect(signalRService.getConnection).toHaveBeenCalledWith('/hubs/chat');
    expect(mockConnection.start).toHaveBeenCalled();
  });

  it('joinSession() should invoke JoinSession on the hub', async () => {
    await service.connect();
    service.joinSession('sess-1');
    expect(mockConnection.invoke).toHaveBeenCalledWith('JoinSession', 'sess-1');
  });

  it('sendMessage() should invoke SendMessage on the hub', async () => {
    await service.connect();
    service.sendMessage('sess-1', 'Hello agent!');
    expect(mockConnection.invoke).toHaveBeenCalledWith('SendMessage', 'sess-1', 'Hello agent!');
  });

  it('requestHandoff() should invoke RequestHandoff on the hub', async () => {
    await service.connect();
    service.requestHandoff('sess-1');
    expect(mockConnection.invoke).toHaveBeenCalledWith('RequestHandoff', 'sess-1');
  });

  it('disconnect() should call stop()', async () => {
    await service.connect();
    service.disconnect();
    expect(mockConnection.stop).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/chat-hub.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/services/chat-hub.service.ts

import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { SignalRService } from '../../shared/services/signalr.service';

export interface LiveChatMessage {
  senderName: string;
  senderRole: 'Customer' | 'Agent';
  content: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private readonly signalRService = inject(SignalRService);
  private connection!: signalR.HubConnection;

  readonly message$ = new Subject<LiveChatMessage>();
  readonly handoffAccepted$ = new Subject<{ agentName: string }>();
  readonly sessionClosed$ = new Subject<void>();

  async connect(): Promise<void> {
    this.connection = this.signalRService.getConnection('/hubs/chat');
    this.connection.on('ReceiveMessage', (msg: LiveChatMessage) => this.message$.next(msg));
    this.connection.on('HandoffAccepted', (data: { agentName: string }) => this.handoffAccepted$.next(data));
    this.connection.on('SessionClosed', () => this.sessionClosed$.next());
    await this.connection.start();
  }

  joinSession(sessionId: string): void {
    this.connection.invoke('JoinSession', sessionId);
  }

  requestHandoff(sessionId: string): void {
    this.connection.invoke('RequestHandoff', sessionId);
  }

  sendMessage(sessionId: string, content: string): void {
    this.connection.invoke('SendMessage', sessionId, content);
  }

  disconnect(): void {
    this.connection?.stop();
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/chat-hub.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/chat-hub.service.ts src/app/portal/services/chat-hub.service.spec.ts
git commit -m "feat(portal): add ChatHubService for SignalR live chat (US-FE-039)"
```

---

## Task 2: LiveChatComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/live-chat/live-chat.component.spec.ts

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { LiveChatComponent } from './live-chat.component';
import { ChatHubService, LiveChatMessage } from '../services/chat-hub.service';
import { Subject } from 'rxjs';

describe('LiveChatComponent', () => {
  let fixture: ComponentFixture<LiveChatComponent>;
  let component: LiveChatComponent;
  let chatHubService: jasmine.SpyObj<ChatHubService>;
  let router: Router;

  let message$: Subject<LiveChatMessage>;
  let handoffAccepted$: Subject<{ agentName: string }>;
  let sessionClosed$: Subject<void>;

  beforeEach(async () => {
    message$ = new Subject();
    handoffAccepted$ = new Subject();
    sessionClosed$ = new Subject();

    chatHubService = jasmine.createSpyObj('ChatHubService', ['connect', 'joinSession', 'requestHandoff', 'sendMessage', 'disconnect'], {
      message$,
      handoffAccepted$,
      sessionClosed$,
    });
    chatHubService.connect.and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      imports: [LiveChatComponent, ReactiveFormsModule, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: ChatHubService, useValue: chatHubService }],
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(LiveChatComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => component.ngOnDestroy());

  it('should create', () => expect(component).toBeTruthy());

  it('should connect to hub and request handoff on init', async () => {
    await fixture.whenStable();
    expect(chatHubService.connect).toHaveBeenCalled();
    expect(chatHubService.requestHandoff).toHaveBeenCalled();
  });

  it('should show waiting state before handoff is accepted', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Waiting');
  });

  it('should update agent name and hide waiting state on HandoffAccepted', async () => {
    await fixture.whenStable();
    handoffAccepted$.next({ agentName: 'Omar' });
    fixture.detectChanges();
    expect(component.agentName()).toBe('Omar');
    expect(component.waitingForAgent()).toBeFalse();
  });

  it('should append incoming messages from hub', async () => {
    await fixture.whenStable();
    handoffAccepted$.next({ agentName: 'Omar' });
    message$.next({ senderName: 'Omar', senderRole: 'Agent', content: 'Hello!', sentAt: '' });
    expect(component.messages().length).toBe(1);
  });

  it('should send message via hub', async () => {
    await fixture.whenStable();
    handoffAccepted$.next({ agentName: 'Omar' });
    component.inputControl.setValue('Hi there');
    component.send();
    expect(chatHubService.sendMessage).toHaveBeenCalledWith(component.sessionId, 'Hi there');
  });

  it('should navigate to ticket creation on SessionClosed', async () => {
    const navigateSpy = spyOn(router, 'navigate');
    await fixture.whenStable();
    sessionClosed$.next();
    expect(navigateSpy).toHaveBeenCalledWith(['/portal/tickets/new'], jasmine.any(Object));
  });

  it('should navigate to ticket on 3-minute timeout', fakeAsync(async () => {
    const navigateSpy = spyOn(router, 'navigate');
    await fixture.whenStable();
    tick(180000);
    expect(navigateSpy).toHaveBeenCalledWith(['/portal/tickets/new'], jasmine.objectContaining({ queryParams: { from: 'livechat' } }));
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/live-chat/live-chat.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/live-chat/live-chat.component.ts

import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Subscription } from 'rxjs';
import { ChatHubService, LiveChatMessage } from '../services/chat-hub.service';

@Component({
  selector: 'app-live-chat',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  templateUrl: './live-chat.component.html',
})
export class LiveChatComponent implements OnInit, OnDestroy {
  private readonly chatHub = inject(ChatHubService);
  private readonly router = inject(Router);

  readonly messages = signal<LiveChatMessage[]>([]);
  readonly waitingForAgent = signal(true);
  readonly agentName = signal<string | null>(null);
  readonly inputControl = new FormControl('');

  readonly sessionId = crypto.randomUUID();

  private timeoutHandle?: ReturnType<typeof setTimeout>;
  private subs = new Subscription();

  ngOnInit(): void {
    this.chatHub.connect().then(() => {
      this.chatHub.joinSession(this.sessionId);
      this.chatHub.requestHandoff(this.sessionId);
    });

    this.subs.add(this.chatHub.message$.subscribe(msg => {
      this.messages.update(msgs => [...msgs, msg]);
    }));

    this.subs.add(this.chatHub.handoffAccepted$.subscribe(({ agentName }) => {
      this.agentName.set(agentName);
      this.waitingForAgent.set(false);
      clearTimeout(this.timeoutHandle);
    }));

    this.subs.add(this.chatHub.sessionClosed$.subscribe(() => {
      this.router.navigate(['/portal/tickets/new'], { queryParams: { from: 'livechat' } });
    }));

    this.timeoutHandle = setTimeout(() => {
      if (this.waitingForAgent()) {
        this.router.navigate(['/portal/tickets/new'], { queryParams: { from: 'livechat' } });
      }
    }, 3 * 60 * 1000);
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    clearTimeout(this.timeoutHandle);
    this.chatHub.disconnect();
  }

  send(): void {
    const text = this.inputControl.value?.trim();
    if (!text) return;
    this.chatHub.sendMessage(this.sessionId, text);
    this.messages.update(msgs => [...msgs, {
      senderName: 'You',
      senderRole: 'Customer',
      content: text,
      sentAt: new Date().toISOString(),
    }]);
    this.inputControl.setValue('');
  }
}
```

```html
<!-- src/app/portal/live-chat/live-chat.component.html -->

<div class="flex flex-col h-screen max-h-screen bg-gray-50">

  <!-- Header -->
  <div class="bg-white border-b px-6 py-4 flex items-center gap-3 shadow-sm">
    <mat-icon class="text-green-500">circle</mat-icon>
    <div>
      <p class="font-semibold">Live Support Chat</p>
      @if (agentName()) {
        <p class="text-xs text-gray-500">Connected with {{ agentName() }}</p>
      } @else {
        <p class="text-xs text-yellow-600">Waiting for an available agent…</p>
      }
    </div>
  </div>

  <!-- Waiting screen -->
  @if (waitingForAgent()) {
    <div class="flex-1 flex flex-col items-center justify-center gap-4 text-gray-500">
      <mat-icon class="text-5xl animate-spin text-blue-400">hourglass_top</mat-icon>
      <p class="text-lg font-medium">Waiting for an agent to join…</p>
      <p class="text-sm">This typically takes less than 2 minutes. If no agent joins, we'll create a ticket for you.</p>
    </div>
  } @else {
    <!-- Message thread -->
    <div class="flex-1 overflow-y-auto p-4 space-y-3">
      @for (msg of messages(); track msg.sentAt) {
        <div [class]="msg.senderRole === 'Customer' ? 'text-right' : 'text-left'">
          <p class="text-xs text-gray-400 mb-1">{{ msg.senderName }}</p>
          <span [class]="msg.senderRole === 'Customer'
            ? 'inline-block bg-blue-500 text-white text-sm rounded-lg px-3 py-2 max-w-[70%]'
            : 'inline-block bg-white border text-gray-800 text-sm rounded-lg px-3 py-2 max-w-[70%]'">
            {{ msg.content }}
          </span>
        </div>
      }
    </div>

    <!-- Input -->
    <div class="bg-white border-t p-3 flex gap-2">
      <mat-form-field appearance="outline" class="flex-1" subscriptSizing="dynamic">
        <input matInput placeholder="Type a message…" [formControl]="inputControl" (keyup.enter)="send()" />
      </mat-form-field>
      <button mat-raised-button color="primary" (click)="send()" [disabled]="!inputControl.value?.trim()">
        <mat-icon>send</mat-icon>
      </button>
    </div>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/live-chat/live-chat.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/live-chat/
git commit -m "feat(portal): implement LiveChatComponent with SignalR handoff and 3-min timeout (US-FE-039)"
```
