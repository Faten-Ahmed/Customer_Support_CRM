# AI Ticket Panels — Implementation Plan

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

**Story:** US-FE-040
**Goal:** Implement the AI assistance panel in the internal ticket detail view — a collapsible right panel with four AI tools: Summarize, Suggest Reply, Suggest Category, and Suggest Articles. Each tool has its own loading spinner, result area, and handles `503 AI_PROVIDER_UNAVAILABLE` with a user-friendly error message.

**Architecture:** `AiService` wraps `/api/v1/ai/**` endpoints. `AiTicketPanelComponent` is standalone, added to the ticket detail layout as an `@Input() ticketId`. The four tools are independent — each has a `signal<string | null>` for result and `signal<boolean>` for loading. The panel is collapsible via a `collapsed` signal, persisted to `localStorage`. 503 errors show an inline "AI service temporarily unavailable" message per tool.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/services/ai.service.ts` |
| Create | `src/app/tickets/services/ai.service.spec.ts` |
| Create | `src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.ts` |
| Create | `src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.html` |
| Create | `src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.spec.ts` |

---

## Task 1: AiService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/services/ai.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AiService } from './ai.service';

describe('AiService', () => {
  let service: AiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AiService],
    });
    service = TestBed.inject(AiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('summarize() should POST /api/v1/ai/tickets/:id/summarize', () => {
    service.summarize('t1').subscribe();
    const req = httpMock.expectOne('/api/v1/ai/tickets/t1/summarize');
    expect(req.request.method).toBe('POST');
    req.flush({ summary: 'Customer has billing issue.' });
  });

  it('suggestReply() should POST /api/v1/ai/tickets/:id/suggest-reply', () => {
    service.suggestReply('t1').subscribe();
    const req = httpMock.expectOne('/api/v1/ai/tickets/t1/suggest-reply');
    expect(req.request.method).toBe('POST');
    req.flush({ reply: 'Thank you for contacting us…' });
  });

  it('suggestCategory() should POST /api/v1/ai/tickets/:id/suggest-category', () => {
    service.suggestCategory('t1').subscribe();
    const req = httpMock.expectOne('/api/v1/ai/tickets/t1/suggest-category');
    expect(req.request.method).toBe('POST');
    req.flush({ departmentId: 'd1', categoryId: 'c2', confidence: 0.91 });
  });

  it('suggestArticles() should POST /api/v1/ai/tickets/:id/suggest-articles', () => {
    service.suggestArticles('t1').subscribe();
    const req = httpMock.expectOne('/api/v1/ai/tickets/t1/suggest-articles');
    expect(req.request.method).toBe('POST');
    req.flush({ articles: [{ id: 'a1', title: 'How to reset password', url: '/kb/a1' }] });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/services/ai.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/services/ai.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AiSummary { summary: string; }
export interface AiReply { reply: string; }
export interface AiCategory { departmentId: string; categoryId: string; confidence: number; }
export interface AiArticles { articles: { id: string; title: string; url: string }[]; }

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);

  summarize(ticketId: string): Observable<AiSummary> {
    return this.http.post<AiSummary>(`/api/v1/ai/tickets/${ticketId}/summarize`, {});
  }

  suggestReply(ticketId: string): Observable<AiReply> {
    return this.http.post<AiReply>(`/api/v1/ai/tickets/${ticketId}/suggest-reply`, {});
  }

  suggestCategory(ticketId: string): Observable<AiCategory> {
    return this.http.post<AiCategory>(`/api/v1/ai/tickets/${ticketId}/suggest-category`, {});
  }

  suggestArticles(ticketId: string): Observable<AiArticles> {
    return this.http.post<AiArticles>(`/api/v1/ai/tickets/${ticketId}/suggest-articles`, {});
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/ai.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/ai.service.ts src/app/tickets/services/ai.service.spec.ts
git commit -m "feat(tickets): add AiService for AI ticket tools (US-FE-040)"
```

---

## Task 2: AiTicketPanelComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { AiTicketPanelComponent } from './ai-ticket-panel.component';
import { AiService } from '../services/ai.service';

describe('AiTicketPanelComponent', () => {
  let fixture: ComponentFixture<AiTicketPanelComponent>;
  let component: AiTicketPanelComponent;
  let aiService: jasmine.SpyObj<AiService>;

  beforeEach(async () => {
    aiService = jasmine.createSpyObj('AiService', ['summarize', 'suggestReply', 'suggestCategory', 'suggestArticles']);
    aiService.summarize.and.returnValue(of({ summary: 'Billing dispute.' }));
    aiService.suggestReply.and.returnValue(of({ reply: 'Thank you…' }));
    aiService.suggestCategory.and.returnValue(of({ departmentId: 'd1', categoryId: 'c1', confidence: 0.9 }));
    aiService.suggestArticles.and.returnValue(of({ articles: [{ id: 'a1', title: 'FAQ', url: '/kb/a1' }] }));

    await TestBed.configureTestingModule({
      imports: [AiTicketPanelComponent, NoopAnimationsModule],
      providers: [{ provide: AiService, useValue: aiService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AiTicketPanelComponent);
    component = fixture.componentInstance;
    component.ticketId = 't1';
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should start collapsed by default', () => {
    expect(component.collapsed()).toBeTrue();
  });

  it('should toggle collapsed state', () => {
    component.toggleCollapsed();
    expect(component.collapsed()).toBeFalse();
  });

  it('summarize() should call AiService and set summary signal', () => {
    component.summarize();
    expect(aiService.summarize).toHaveBeenCalledWith('t1');
    expect(component.summary()).toBe('Billing dispute.');
  });

  it('suggestReply() should call AiService and set reply signal', () => {
    component.suggestReply();
    expect(aiService.suggestReply).toHaveBeenCalledWith('t1');
    expect(component.suggestedReply()).toBe('Thank you…');
  });

  it('suggestCategory() should call AiService and set category signal', () => {
    component.suggestCategory();
    expect(aiService.suggestCategory).toHaveBeenCalledWith('t1');
    expect(component.suggestedCategory()?.departmentId).toBe('d1');
  });

  it('suggestArticles() should call AiService and set articles signal', () => {
    component.suggestArticles();
    expect(aiService.suggestArticles).toHaveBeenCalledWith('t1');
    expect(component.suggestedArticles().length).toBe(1);
  });

  it('should set error signal on 503 AI_PROVIDER_UNAVAILABLE for summarize', () => {
    aiService.summarize.and.returnValue(throwError(() => ({ status: 503, error: { code: 'AI_PROVIDER_UNAVAILABLE' } })));
    component.summarize();
    expect(component.summaryError()).toBe('AI service temporarily unavailable. Please try again later.');
  });

  it('should set loading=false after summary completes', () => {
    component.summarize();
    expect(component.loadingSummary()).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.ts

import { Component, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { AiService, AiCategory } from '../services/ai.service';

const AI_UNAVAILABLE_MSG = 'AI service temporarily unavailable. Please try again later.';

@Component({
  selector: 'app-ai-ticket-panel',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDividerModule],
  templateUrl: './ai-ticket-panel.component.html',
})
export class AiTicketPanelComponent {
  @Input() ticketId!: string;

  private readonly aiService = inject(AiService);

  readonly collapsed = signal(true);

  readonly summary = signal<string | null>(null);
  readonly summaryError = signal<string | null>(null);
  readonly loadingSummary = signal(false);

  readonly suggestedReply = signal<string | null>(null);
  readonly replyError = signal<string | null>(null);
  readonly loadingReply = signal(false);

  readonly suggestedCategory = signal<AiCategory | null>(null);
  readonly categoryError = signal<string | null>(null);
  readonly loadingCategory = signal(false);

  readonly suggestedArticles = signal<{ id: string; title: string; url: string }[]>([]);
  readonly articlesError = signal<string | null>(null);
  readonly loadingArticles = signal(false);

  toggleCollapsed(): void {
    this.collapsed.update(v => !v);
  }

  summarize(): void {
    this.loadingSummary.set(true);
    this.summaryError.set(null);
    this.aiService.summarize(this.ticketId).subscribe({
      next: r => { this.summary.set(r.summary); this.loadingSummary.set(false); },
      error: () => { this.summaryError.set(AI_UNAVAILABLE_MSG); this.loadingSummary.set(false); },
    });
  }

  suggestReply(): void {
    this.loadingReply.set(true);
    this.replyError.set(null);
    this.aiService.suggestReply(this.ticketId).subscribe({
      next: r => { this.suggestedReply.set(r.reply); this.loadingReply.set(false); },
      error: () => { this.replyError.set(AI_UNAVAILABLE_MSG); this.loadingReply.set(false); },
    });
  }

  suggestCategory(): void {
    this.loadingCategory.set(true);
    this.categoryError.set(null);
    this.aiService.suggestCategory(this.ticketId).subscribe({
      next: r => { this.suggestedCategory.set(r); this.loadingCategory.set(false); },
      error: () => { this.categoryError.set(AI_UNAVAILABLE_MSG); this.loadingCategory.set(false); },
    });
  }

  suggestArticles(): void {
    this.loadingArticles.set(true);
    this.articlesError.set(null);
    this.aiService.suggestArticles(this.ticketId).subscribe({
      next: r => { this.suggestedArticles.set(r.articles); this.loadingArticles.set(false); },
      error: () => { this.articlesError.set(AI_UNAVAILABLE_MSG); this.loadingArticles.set(false); },
    });
  }
}
```

```html
<!-- src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.html -->

<div class="border-l bg-white h-full flex flex-col" [style.width]="collapsed() ? '48px' : '320px'" style="transition: width 0.2s ease;">

  <!-- Toggle button -->
  <div class="flex items-center justify-between px-3 py-3 border-b">
    @if (!collapsed()) {
      <span class="text-sm font-semibold text-purple-700 flex items-center gap-1">
        <mat-icon class="text-sm">auto_awesome</mat-icon> AI Assistant
      </span>
    }
    <button mat-icon-button (click)="toggleCollapsed()" [matTooltip]="collapsed() ? 'Expand AI panel' : 'Collapse AI panel'">
      <mat-icon>{{ collapsed() ? 'chevron_left' : 'chevron_right' }}</mat-icon>
    </button>
  </div>

  @if (!collapsed()) {
    <div class="flex-1 overflow-y-auto p-3 space-y-4">

      <!-- Summarize -->
      <section>
        <div class="flex items-center justify-between mb-2">
          <p class="text-xs font-semibold text-gray-600 uppercase tracking-wide">Summary</p>
          <button mat-stroked-button class="text-xs" (click)="summarize()" [disabled]="loadingSummary()">
            @if (loadingSummary()) { <mat-spinner diameter="14"></mat-spinner> } @else { Generate }
          </button>
        </div>
        @if (summaryError()) {
          <p class="text-xs text-red-500">{{ summaryError() }}</p>
        } @else if (summary()) {
          <p class="text-xs text-gray-700 bg-gray-50 rounded p-2">{{ summary() }}</p>
        }
      </section>

      <mat-divider></mat-divider>

      <!-- Suggest Reply -->
      <section>
        <div class="flex items-center justify-between mb-2">
          <p class="text-xs font-semibold text-gray-600 uppercase tracking-wide">Suggested Reply</p>
          <button mat-stroked-button class="text-xs" (click)="suggestReply()" [disabled]="loadingReply()">
            @if (loadingReply()) { <mat-spinner diameter="14"></mat-spinner> } @else { Generate }
          </button>
        </div>
        @if (replyError()) {
          <p class="text-xs text-red-500">{{ replyError() }}</p>
        } @else if (suggestedReply()) {
          <p class="text-xs text-gray-700 bg-gray-50 rounded p-2 whitespace-pre-wrap">{{ suggestedReply() }}</p>
        }
      </section>

      <mat-divider></mat-divider>

      <!-- Suggest Category -->
      <section>
        <div class="flex items-center justify-between mb-2">
          <p class="text-xs font-semibold text-gray-600 uppercase tracking-wide">Suggested Category</p>
          <button mat-stroked-button class="text-xs" (click)="suggestCategory()" [disabled]="loadingCategory()">
            @if (loadingCategory()) { <mat-spinner diameter="14"></mat-spinner> } @else { Detect }
          </button>
        </div>
        @if (categoryError()) {
          <p class="text-xs text-red-500">{{ categoryError() }}</p>
        } @else if (suggestedCategory()) {
          <div class="text-xs text-gray-700 bg-gray-50 rounded p-2">
            <p>Dept: <span class="font-medium">{{ suggestedCategory()!.departmentId }}</span></p>
            <p>Category: <span class="font-medium">{{ suggestedCategory()!.categoryId }}</span></p>
            <p>Confidence: <span class="font-medium">{{ (suggestedCategory()!.confidence * 100).toFixed(0) }}%</span></p>
          </div>
        }
      </section>

      <mat-divider></mat-divider>

      <!-- Suggest Articles -->
      <section>
        <div class="flex items-center justify-between mb-2">
          <p class="text-xs font-semibold text-gray-600 uppercase tracking-wide">Related Articles</p>
          <button mat-stroked-button class="text-xs" (click)="suggestArticles()" [disabled]="loadingArticles()">
            @if (loadingArticles()) { <mat-spinner diameter="14"></mat-spinner> } @else { Find }
          </button>
        </div>
        @if (articlesError()) {
          <p class="text-xs text-red-500">{{ articlesError() }}</p>
        } @else if (suggestedArticles().length > 0) {
          <ul class="space-y-1">
            @for (article of suggestedArticles(); track article.id) {
              <li>
                <a [href]="article.url" target="_blank" class="text-xs text-blue-600 hover:underline">
                  {{ article.title }}
                </a>
              </li>
            }
          </ul>
        }
      </section>

    </div>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ai-ticket-panel/ai-ticket-panel.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ai-ticket-panel/
git commit -m "feat(tickets): implement AiTicketPanelComponent with four AI tools and 503 handling (US-FE-040)"
```
