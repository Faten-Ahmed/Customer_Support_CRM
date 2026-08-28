# Agent AI Assistant — Implementation Plan

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

**Story:** US-FE-041
**Goal:** Implement the agent-side AI assistant — a slide-in panel accessible from the sidebar, with a persistent chat session (session ID stored in `sessionStorage`), suggested KB articles displayed as clickable cards, and a "New Chat" button that resets the session.

**Architecture:** `AgentAiAssistantComponent` is standalone, placed inside `AppShellComponent` and toggled via a signal. It reuses `AiChatService.sendMessage()` with `context: 'agent'`. Session ID is stored in `sessionStorage` under `agent_ai_session_id` so it persists across panel open/close within the same browser tab but resets on tab close. "New Chat" clears the session ID, generates a new one, and clears the message history.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/shared/agent-ai-assistant/agent-ai-assistant.component.ts` |
| Create | `src/app/shared/agent-ai-assistant/agent-ai-assistant.component.html` |
| Create | `src/app/shared/agent-ai-assistant/agent-ai-assistant.component.spec.ts` |

---

## Task 1: AgentAiAssistantComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/shared/agent-ai-assistant/agent-ai-assistant.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AgentAiAssistantComponent } from './agent-ai-assistant.component';
import { AiChatService, ChatResponse } from '../../portal/services/ai-chat.service';

describe('AgentAiAssistantComponent', () => {
  let fixture: ComponentFixture<AgentAiAssistantComponent>;
  let component: AgentAiAssistantComponent;
  let chatService: jasmine.SpyObj<AiChatService>;

  const mockResponse: ChatResponse = {
    reply: 'Here is how to resolve billing issues.',
    handoffRequired: false,
    suggestedArticles: [{ id: 'a1', title: 'Billing FAQ' }],
  };

  beforeEach(async () => {
    chatService = jasmine.createSpyObj('AiChatService', ['sendMessage']);
    chatService.sendMessage.and.returnValue(of(mockResponse));
    sessionStorage.removeItem('agent_ai_session_id');

    await TestBed.configureTestingModule({
      imports: [AgentAiAssistantComponent, ReactiveFormsModule, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: AiChatService, useValue: chatService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AgentAiAssistantComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => sessionStorage.removeItem('agent_ai_session_id'));

  it('should create', () => expect(component).toBeTruthy());

  it('should generate a sessionId and store it in sessionStorage', () => {
    expect(component.sessionId).toBeTruthy();
    expect(sessionStorage.getItem('agent_ai_session_id')).toBe(component.sessionId);
  });

  it('should reuse existing sessionId from sessionStorage', () => {
    sessionStorage.setItem('agent_ai_session_id', 'existing-id');
    const fresh = TestBed.createComponent(AgentAiAssistantComponent);
    fresh.detectChanges();
    expect(fresh.componentInstance.sessionId).toBe('existing-id');
    fresh.destroy();
  });

  it('should send message with context=agent', () => {
    component.inputControl.setValue('How do I handle escalations?');
    component.send();
    expect(chatService.sendMessage).toHaveBeenCalledWith(jasmine.objectContaining({ context: 'agent' }));
  });

  it('should append user and assistant messages to history', () => {
    component.inputControl.setValue('Test question');
    component.send();
    expect(component.messages().some(m => m.content === 'Test question' && m.role === 'user')).toBeTrue();
    expect(component.messages().some(m => m.content === mockResponse.reply && m.role === 'assistant')).toBeTrue();
  });

  it('should display suggested articles after response', () => {
    component.inputControl.setValue('billing');
    component.send();
    expect(component.suggestedArticles().length).toBe(1);
    expect(component.suggestedArticles()[0].title).toBe('Billing FAQ');
  });

  it('newChat() should clear messages, articles, and generate a new sessionId', () => {
    component.inputControl.setValue('hello');
    component.send();
    const oldSessionId = component.sessionId;
    component.newChat();
    expect(component.messages().length).toBe(0);
    expect(component.suggestedArticles().length).toBe(0);
    expect(component.sessionId).not.toBe(oldSessionId);
  });

  it('should not send empty message', () => {
    component.inputControl.setValue('');
    component.send();
    expect(chatService.sendMessage).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shared/agent-ai-assistant/agent-ai-assistant.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/shared/agent-ai-assistant/agent-ai-assistant.component.ts

import { Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AiChatService, ChatMessage } from '../../portal/services/ai-chat.service';

const SESSION_KEY = 'agent_ai_session_id';

@Component({
  selector: 'app-agent-ai-assistant',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatTooltipModule],
  templateUrl: './agent-ai-assistant.component.html',
})
export class AgentAiAssistantComponent {
  private readonly chatService = inject(AiChatService);

  readonly messages = signal<ChatMessage[]>([]);
  readonly suggestedArticles = signal<{ id: string; title: string }[]>([]);
  readonly typing = signal(false);
  readonly inputControl = new FormControl('');

  sessionId: string;

  constructor() {
    const stored = sessionStorage.getItem(SESSION_KEY);
    this.sessionId = stored ?? this.generateSession();
  }

  private generateSession(): string {
    const id = crypto.randomUUID();
    sessionStorage.setItem(SESSION_KEY, id);
    return id;
  }

  send(): void {
    const text = this.inputControl.value?.trim();
    if (!text) return;

    this.messages.update(msgs => [...msgs, { role: 'user', content: text, timestamp: new Date() }]);
    this.inputControl.setValue('');
    this.typing.set(true);
    this.suggestedArticles.set([]);

    this.chatService.sendMessage({ sessionId: this.sessionId, message: text, context: 'agent' }).subscribe({
      next: res => {
        this.typing.set(false);
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: res.reply, timestamp: new Date() }]);
        if (res.suggestedArticles?.length) this.suggestedArticles.set(res.suggestedArticles);
      },
      error: () => {
        this.typing.set(false);
        this.messages.update(msgs => [...msgs, {
          role: 'assistant',
          content: 'I'm having trouble responding right now. Please try again.',
          timestamp: new Date(),
        }]);
      },
    });
  }

  newChat(): void {
    this.messages.set([]);
    this.suggestedArticles.set([]);
    sessionStorage.removeItem(SESSION_KEY);
    this.sessionId = this.generateSession();
  }
}
```

```html
<!-- src/app/shared/agent-ai-assistant/agent-ai-assistant.component.html -->

<div class="flex flex-col h-full bg-white border-l">

  <!-- Header -->
  <div class="bg-purple-600 text-white px-4 py-3 flex items-center justify-between">
    <div class="flex items-center gap-2">
      <mat-icon>auto_awesome</mat-icon>
      <span class="font-semibold">AI Assistant</span>
    </div>
    <button mat-stroked-button class="text-white border-white text-xs" (click)="newChat()" matTooltip="Start a new conversation">
      <mat-icon>refresh</mat-icon> New Chat
    </button>
  </div>

  <!-- Messages -->
  <div class="flex-1 overflow-y-auto p-4 space-y-3 bg-gray-50">
    @if (messages().length === 0) {
      <div class="text-center text-gray-400 mt-10">
        <mat-icon class="text-4xl text-purple-300">smart_toy</mat-icon>
        <p class="text-sm mt-2">Ask me anything to assist with your tickets.</p>
      </div>
    }

    @for (msg of messages(); track msg.timestamp) {
      <div [class]="msg.role === 'user' ? 'text-right' : 'text-left'">
        <span [class]="msg.role === 'user'
          ? 'inline-block bg-purple-500 text-white text-sm rounded-lg px-3 py-2 max-w-[85%]'
          : 'inline-block bg-white border text-gray-800 text-sm rounded-lg px-3 py-2 max-w-[85%]'">
          {{ msg.content }}
        </span>
      </div>
    }

    @if (typing()) {
      <div class="text-left">
        <span class="inline-block bg-white border text-gray-400 text-sm rounded-lg px-3 py-2">Thinking…</span>
      </div>
    }

    <!-- Suggested articles -->
    @if (suggestedArticles().length > 0) {
      <div class="mt-2">
        <p class="text-xs text-gray-500 font-medium mb-1">Relevant KB articles:</p>
        @for (article of suggestedArticles(); track article.id) {
          <a [routerLink]="['/kb', article.id]" target="_blank"
             class="block text-xs text-purple-600 underline hover:text-purple-800 py-0.5">
            {{ article.title }}
          </a>
        }
      </div>
    }
  </div>

  <!-- Input -->
  <div class="border-t bg-white p-3 flex gap-2">
    <mat-form-field appearance="outline" class="flex-1" subscriptSizing="dynamic">
      <input matInput placeholder="Ask AI…" [formControl]="inputControl" (keyup.enter)="send()" />
    </mat-form-field>
    <button mat-raised-button color="primary" (click)="send()" [disabled]="!inputControl.value?.trim() || typing()">
      <mat-icon>send</mat-icon>
    </button>
  </div>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shared/agent-ai-assistant/agent-ai-assistant.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/shared/agent-ai-assistant/
git commit -m "feat(shell): implement AgentAiAssistantComponent with sessionStorage persistence (US-FE-041)"
```
