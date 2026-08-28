# Portal Chatbot Widget — Implementation Plan

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

**Story:** US-FE-038
**Goal:** Implement a floating AI chatbot widget for the customer portal — a bubble in the bottom-right corner that opens a chat panel, sends messages to the AI service, shows typing indicators, displays suggested KB articles, and triggers live chat handoff when `handoffRequired: true`.

**Architecture:** `AiChatService.sendMessage()` POSTs to `/api/v1/portal/chat/message` with `{ sessionId, message }` and returns `{ reply, suggestedArticles?, handoffRequired }`. `ChatbotWidgetComponent` is standalone, overlaid on all portal pages (placed in `PortalShellComponent`). Session ID is generated once per widget lifecycle (`crypto.randomUUID()`). When `handoffRequired: true`, the component emits `(handoff)` output and the shell navigates to `/portal/live-chat`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/services/ai-chat.service.ts` |
| Create | `src/app/portal/services/ai-chat.service.spec.ts` |
| Create | `src/app/portal/chatbot/chatbot-widget.component.ts` |
| Create | `src/app/portal/chatbot/chatbot-widget.component.html` |
| Create | `src/app/portal/chatbot/chatbot-widget.component.spec.ts` |

---

## Task 1: AiChatService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/services/ai-chat.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AiChatService } from './ai-chat.service';

describe('AiChatService', () => {
  let service: AiChatService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AiChatService],
    });
    service = TestBed.inject(AiChatService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sendMessage() should POST /api/v1/portal/chat/message', () => {
    service.sendMessage({ sessionId: 's1', message: 'Hello', context: 'portal' }).subscribe();
    const req = httpMock.expectOne('/api/v1/portal/chat/message');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.sessionId).toBe('s1');
    expect(req.request.body.message).toBe('Hello');
    req.flush({ reply: 'Hi there!', handoffRequired: false });
  });

  it('sendMessage() should propagate handoffRequired in response', () => {
    let result: any;
    service.sendMessage({ sessionId: 's1', message: 'agent please', context: 'portal' }).subscribe(r => result = r);
    const req = httpMock.expectOne('/api/v1/portal/chat/message');
    req.flush({ reply: 'Connecting you to an agent…', handoffRequired: true });
    expect(result.handoffRequired).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/ai-chat.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/services/ai-chat.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface ChatRequest {
  sessionId: string;
  message: string;
  context: 'portal' | 'agent';
}

export interface ChatResponse {
  reply: string;
  handoffRequired?: boolean;
  suggestedArticles?: { id: string; title: string }[];
}

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private readonly http = inject(HttpClient);

  sendMessage(payload: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>('/api/v1/portal/chat/message', payload);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/ai-chat.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/ai-chat.service.ts src/app/portal/services/ai-chat.service.spec.ts
git commit -m "feat(portal): add AiChatService (US-FE-038)"
```

---

## Task 2: ChatbotWidgetComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/chatbot/chatbot-widget.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ChatbotWidgetComponent } from './chatbot-widget.component';
import { AiChatService, ChatResponse } from '../services/ai-chat.service';

describe('ChatbotWidgetComponent', () => {
  let fixture: ComponentFixture<ChatbotWidgetComponent>;
  let component: ChatbotWidgetComponent;
  let chatService: jasmine.SpyObj<AiChatService>;

  const mockResponse: ChatResponse = {
    reply: 'Try resetting your password from the login page.',
    handoffRequired: false,
  };

  beforeEach(async () => {
    chatService = jasmine.createSpyObj('AiChatService', ['sendMessage']);
    chatService.sendMessage.and.returnValue(of(mockResponse));

    await TestBed.configureTestingModule({
      imports: [ChatbotWidgetComponent, ReactiveFormsModule, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: AiChatService, useValue: chatService }],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatbotWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should start with widget closed', () => {
    expect(component.open()).toBeFalse();
  });

  it('should toggle open on bubble click', () => {
    component.toggleOpen();
    expect(component.open()).toBeTrue();
  });

  it('should send message and append to history', () => {
    component.inputControl.setValue('Hello');
    component.send();
    expect(chatService.sendMessage).toHaveBeenCalled();
    expect(component.messages().some(m => m.content === 'Hello')).toBeTrue();
    expect(component.messages().some(m => m.content === mockResponse.reply)).toBeTrue();
  });

  it('should show typing indicator while waiting for response', () => {
    chatService.sendMessage.and.returnValue(of(mockResponse));
    component.inputControl.setValue('Test');
    component.send();
    expect(component.messages().length).toBeGreaterThan(0);
  });

  it('should set handoffRequired signal when response has handoffRequired=true', () => {
    chatService.sendMessage.and.returnValue(of({ reply: 'Agent coming…', handoffRequired: true }));
    component.inputControl.setValue('I need a human');
    component.send();
    expect(component.handoffRequired()).toBeTrue();
  });

  it('should display suggested articles when returned', () => {
    chatService.sendMessage.and.returnValue(of({
      reply: 'Here are some articles',
      suggestedArticles: [{ id: 'a1', title: 'Password Reset Guide' }],
      handoffRequired: false,
    }));
    component.inputControl.setValue('password');
    component.send();
    expect(component.suggestedArticles().length).toBe(1);
  });

  it('should not send empty message', () => {
    component.inputControl.setValue('  ');
    component.send();
    expect(chatService.sendMessage).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/chatbot/chatbot-widget.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/chatbot/chatbot-widget.component.ts

import { Component, inject, signal, Output, EventEmitter } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AiChatService, ChatMessage } from '../services/ai-chat.service';

@Component({
  selector: 'app-chatbot-widget',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  templateUrl: './chatbot-widget.component.html',
})
export class ChatbotWidgetComponent {
  private readonly chatService = inject(AiChatService);

  @Output() handoff = new EventEmitter<void>();

  readonly open = signal(false);
  readonly typing = signal(false);
  readonly handoffRequired = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly suggestedArticles = signal<{ id: string; title: string }[]>([]);
  readonly inputControl = new FormControl('');

  private readonly sessionId = crypto.randomUUID();

  toggleOpen(): void {
    this.open.update(v => !v);
  }

  send(): void {
    const text = this.inputControl.value?.trim();
    if (!text) return;

    this.messages.update(msgs => [...msgs, { role: 'user', content: text, timestamp: new Date() }]);
    this.inputControl.setValue('');
    this.typing.set(true);
    this.suggestedArticles.set([]);

    this.chatService.sendMessage({ sessionId: this.sessionId, message: text, context: 'portal' }).subscribe({
      next: res => {
        this.typing.set(false);
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: res.reply, timestamp: new Date() }]);
        if (res.suggestedArticles?.length) this.suggestedArticles.set(res.suggestedArticles);
        if (res.handoffRequired) {
          this.handoffRequired.set(true);
          this.handoff.emit();
        }
      },
      error: () => {
        this.typing.set(false);
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: 'Sorry, I'm having trouble responding. Please try again.', timestamp: new Date() }]);
      },
    });
  }
}
```

```html
<!-- src/app/portal/chatbot/chatbot-widget.component.html -->

<!-- Floating bubble -->
<div class="fixed bottom-6 right-6 z-50 flex flex-col items-end gap-3">

  <!-- Chat panel -->
  @if (open()) {
    <div class="w-80 bg-white rounded-xl shadow-2xl border flex flex-col overflow-hidden" style="height: 420px;">

      <!-- Header -->
      <div class="bg-blue-600 text-white px-4 py-3 flex items-center justify-between">
        <span class="font-semibold">Support Assistant</span>
        <button mat-icon-button (click)="toggleOpen()" class="text-white">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <!-- Messages -->
      <div class="flex-1 overflow-y-auto p-3 space-y-3 bg-gray-50">
        @if (messages().length === 0) {
          <p class="text-sm text-gray-400 text-center mt-4">Hi! How can I help you today?</p>
        }
        @for (msg of messages(); track msg.timestamp) {
          <div [class]="msg.role === 'user' ? 'text-right' : 'text-left'">
            <span [class]="msg.role === 'user'
              ? 'inline-block bg-blue-500 text-white text-sm rounded-lg px-3 py-2 max-w-[85%]'
              : 'inline-block bg-white border text-gray-800 text-sm rounded-lg px-3 py-2 max-w-[85%]'">
              {{ msg.content }}
            </span>
          </div>
        }

        @if (typing()) {
          <div class="text-left">
            <span class="inline-block bg-white border text-gray-400 text-sm rounded-lg px-3 py-2">
              Typing…
            </span>
          </div>
        }

        <!-- Suggested articles -->
        @if (suggestedArticles().length > 0) {
          <div class="mt-2">
            <p class="text-xs text-gray-500 mb-1">Related articles:</p>
            @for (article of suggestedArticles(); track article.id) {
              <a [routerLink]="['/portal/kb', article.id]"
                 class="block text-xs text-blue-600 underline hover:text-blue-800 py-0.5">
                {{ article.title }}
              </a>
            }
          </div>
        }

        <!-- Handoff banner -->
        @if (handoffRequired()) {
          <div class="bg-yellow-50 border border-yellow-300 rounded p-2 text-xs text-yellow-800">
            Connecting you to a live agent…
            <a routerLink="/portal/live-chat" class="text-blue-600 underline ml-1">Join chat</a>
          </div>
        }
      </div>

      <!-- Input -->
      <div class="border-t bg-white p-2 flex gap-2">
        <input
          class="flex-1 border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-blue-400"
          placeholder="Type a message…"
          [formControl]="inputControl"
          (keyup.enter)="send()"
        />
        <button mat-icon-button color="primary" (click)="send()">
          <mat-icon>send</mat-icon>
        </button>
      </div>
    </div>
  }

  <!-- Bubble button -->
  <button mat-fab color="primary" (click)="toggleOpen()" matTooltip="{{ open() ? 'Close chat' : 'Chat with us' }}">
    <mat-icon>{{ open() ? 'close' : 'chat_bubble' }}</mat-icon>
  </button>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/chatbot/chatbot-widget.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/chatbot/
git commit -m "feat(portal): implement ChatbotWidgetComponent with AI chat and handoff (US-FE-038)"
```
