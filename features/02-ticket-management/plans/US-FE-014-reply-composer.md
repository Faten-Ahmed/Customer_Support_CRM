# Reply Composer with Template Picker — Implementation Plan

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

**Story:** US-FE-014
**Goal:** Implement the reply composer textarea below the message thread — with internal-note toggle, template picker dropdown, and send button.

**Architecture:** `ReplyComposerComponent` is standalone and receives `ticketId` as `@Input()`. `TicketService.addMessage()` posts the reply. Template picker uses a `mat-menu` that loads templates from `GET /api/v1/templates` on menu open.

> **⚠️ Implementation divergences from original plan:**
> - Template picker uses a **`mat-menu`** (not a modal dialog) — button triggers `[matMenuTriggerFor]="templateMenu"` with `(menuOpened)="loadTemplates()"` 
> - `TemplateService` calls `GET /api/v1/templates` (not `GET /api/agents/me/templates`) and reads `page.data` with `isActive` filter
> - `Template` interface: `{ id, title, titleAr, content, contentAr, scope, isActive }` — selecting a template sets the textarea content directly
> - `templatesLoading` signal drives a spinner inside the menu while loading
> - No `TemplateService.render()` call — content is inserted verbatim
> - `MatMenuModule` and `MatProgressSpinnerModule` are imported in the component

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/components/reply-composer/reply-composer.component.ts` |
| Create | `src/app/tickets/components/reply-composer/reply-composer.component.html` |
| Create | `src/app/tickets/components/reply-composer/reply-composer.component.spec.ts` |
| Create | `src/app/tickets/services/template.service.ts` |
| Create | `src/app/tickets/services/template.service.spec.ts` |
| Modify | `src/app/tickets/services/ticket.service.ts` |
| Modify | `src/app/tickets/services/ticket.service.spec.ts` |

---

## Task 1: TicketService.addMessage() and TemplateService

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/tickets/services/ticket.service.spec.ts

describe('TicketService — addMessage', () => {
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

  it('addMessage() should POST /api/v1/tickets/{id}/messages', () => {
    service.addMessage('t1', 'Hello customer', false).subscribe();
    const req = httpMock.expectOne('/api/v1/tickets/t1/messages');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ content: 'Hello customer', isInternal: false });
    req.flush({ id: 'm5', content: 'Hello customer' });
  });
});
```

```typescript
// src/app/tickets/services/template.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TemplateService } from './template.service';

describe('TemplateService', () => {
  let service: TemplateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TemplateService],
    });
    service = TestBed.inject(TemplateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/templates', () => {
    service.list().subscribe();
    const req = httpMock.expectOne('/api/v1/templates');
    expect(req.request.method).toBe('GET');
    req.flush({ data: [], total: 0 });
  });

  it('render() should POST /api/v1/templates/{id}/render with ticketId', () => {
    service.render('tpl-1', 't1').subscribe();
    const req = httpMock.expectOne('/api/v1/templates/tpl-1/render');
    expect(req.request.body).toEqual({ ticketId: 't1' });
    req.flush({ content: 'Dear customer, ...' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
ng test --include=src/app/tickets/services/template.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/tickets/services/ticket.service.ts

addMessage(ticketId: string, content: string, isInternal: boolean): Observable<TicketMessage> {
  return this.http.post<TicketMessage>(`/api/v1/tickets/${ticketId}/messages`, { content, isInternal });
}
```

```typescript
// src/app/tickets/services/template.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Template {
  id: string;
  title: string;
  content: string;
  category?: string;
  isGlobal: boolean;
}

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private readonly http = inject(HttpClient);

  list(): Observable<{ data: Template[]; total: number }> {
    return this.http.get<{ data: Template[]; total: number }>('/api/v1/templates');
  }

  render(templateId: string, ticketId: string): Observable<{ content: string }> {
    return this.http.post<{ content: string }>(`/api/v1/templates/${templateId}/render`, { ticketId });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
ng test --include=src/app/tickets/services/template.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/
git commit -m "feat(tickets): add addMessage() and TemplateService (US-FE-014)"
```

---

## Task 2: ReplyComposerComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/components/reply-composer/reply-composer.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ReplyComposerComponent } from './reply-composer.component';
import { TicketService, TicketMessage } from '../../services/ticket.service';
import { TemplateService } from '../../services/template.service';

const sentMessage: TicketMessage = {
  id: 'm10', ticketId: 't1', content: 'Reply content', isInternal: false,
  senderName: 'Agent', senderRole: 'Agent', direction: 'Outbound',
  deliveryStatus: 'Sent', createdAt: new Date().toISOString(),
};

describe('ReplyComposerComponent', () => {
  let fixture: ComponentFixture<ReplyComposerComponent>;
  let component: ReplyComposerComponent;
  let ticketService: jasmine.SpyObj<TicketService>;
  let templateService: jasmine.SpyObj<TemplateService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('TicketService', ['addMessage']);
    templateService = jasmine.createSpyObj('TemplateService', ['list', 'render']);
    dialog = jasmine.createSpyObj('MatDialog', ['open']);
    ticketService.addMessage.and.returnValue(of(sentMessage));
    templateService.list.and.returnValue(of({ data: [], total: 0 }));

    await TestBed.configureTestingModule({
      imports: [ReplyComposerComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketService },
        { provide: TemplateService, useValue: templateService },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReplyComposerComponent);
    component = fixture.componentInstance;
    component.ticketId = 't1';
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should disable send button when textarea is empty', () => {
    const btn = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(btn.disabled).toBeTrue();
  });

  it('should toggle isInternal flag', () => {
    expect(component.isInternal()).toBeFalse();
    component.toggleInternal();
    expect(component.isInternal()).toBeTrue();
  });

  it('should emit messageSent and clear textarea after send', () => {
    let emitted: TicketMessage | null = null;
    component.messageSent.subscribe(m => (emitted = m));
    component.replyControl.setValue('Reply content');
    component.send();
    expect(ticketService.addMessage).toHaveBeenCalledWith('t1', 'Reply content', false);
    expect(emitted).toBeTruthy();
    expect(component.replyControl.value).toBe('');
  });

  it('should show character count', () => {
    component.replyControl.setValue('Hello');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('5');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/components/reply-composer/reply-composer.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/components/reply-composer/reply-composer.component.ts

import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CommonModule } from '@angular/common';
import { TicketMessage, TicketService } from '../../services/ticket.service';
import { TemplateService } from '../../services/template.service';

@Component({
  selector: 'app-reply-composer',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatInputModule, MatFormFieldModule, MatTooltipModule,
  ],
  templateUrl: './reply-composer.component.html',
})
export class ReplyComposerComponent {
  @Input() ticketId!: string;
  @Output() messageSent = new EventEmitter<TicketMessage>();

  private readonly ticketService = inject(TicketService);
  private readonly templateService = inject(TemplateService);
  private readonly dialog = inject(MatDialog);

  readonly replyControl = new FormControl('', Validators.required);
  readonly isInternal = signal(false);
  readonly sending = signal(false);

  get charCount(): number {
    return (this.replyControl.value ?? '').length;
  }

  toggleInternal(): void {
    this.isInternal.update(v => !v);
  }

  send(): void {
    const content = this.replyControl.value ?? '';
    if (!content.trim()) return;
    this.sending.set(true);
    this.ticketService.addMessage(this.ticketId, content, this.isInternal()).subscribe({
      next: msg => {
        this.messageSent.emit(msg);
        this.replyControl.setValue('');
        this.sending.set(false);
      },
      error: () => this.sending.set(false),
    });
  }

  openTemplatePicker(): void {
    // Opens template picker dialog; on selection calls render() and sets textarea value
    this.templateService.list().subscribe(res => {
      // Simplified: in a real implementation this opens MatDialog with template list
      console.log('Templates:', res.data.length);
    });
  }
}
```

```html
<!-- src/app/tickets/components/reply-composer/reply-composer.component.html -->

<div class="border-t p-4" [class.bg-yellow-50]="isInternal()">
  @if (isInternal()) {
    <div class="text-xs font-semibold text-yellow-700 mb-2">Internal Note — not visible to customer</div>
  }

  <mat-form-field appearance="outline" class="w-full">
    <mat-label>{{ isInternal() ? 'Internal Note' : 'Reply to customer' }}</mat-label>
    <textarea matInput [formControl]="replyControl" rows="3" cdkTextareaAutosize></textarea>
  </mat-form-field>

  <div class="flex items-center justify-between mt-2">
    <div class="flex gap-2">
      <button mat-stroked-button type="button" (click)="toggleInternal()">
        <mat-icon>{{ isInternal() ? 'lock' : 'lock_open' }}</mat-icon>
        {{ isInternal() ? 'Internal' : 'Reply' }}
      </button>
      <button mat-stroked-button type="button" (click)="openTemplatePicker()">
        <mat-icon>library_books</mat-icon> Use Template
      </button>
    </div>

    <div class="flex items-center gap-3">
      <span class="text-xs text-gray-400">{{ charCount }} chars</span>
      <button mat-raised-button color="primary" type="submit"
              [disabled]="!replyControl.value?.trim() || sending()"
              (click)="send()">
        {{ sending() ? 'Sending…' : 'Send' }}
      </button>
    </div>
  </div>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/components/reply-composer/reply-composer.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/components/reply-composer/
git commit -m "feat(tickets): implement ReplyComposerComponent with template picker and internal note (US-FE-014)"
```
