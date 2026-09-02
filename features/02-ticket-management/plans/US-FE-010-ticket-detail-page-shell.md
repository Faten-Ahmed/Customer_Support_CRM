# Ticket Detail Page Shell — Implementation Plan

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

**Story:** US-FE-010  
**Goal:** Implement the `/app/tickets/{id}` shell page with a three-panel layout, action bar, and SignalR real-time integration hook.

**Architecture:** `TicketDetailComponent` is a standalone shell that loads ticket data via `TicketService.getById(id)` on route activation. The three-panel layout (metadata, thread, AI assistance) uses Angular Signals for reactive state. SignalR integration is handled by a `SignalRService` that joins a ticket room and emits new messages via an Observable; the component subscribes on init and unsubscribes on destroy. Action buttons are hidden/disabled based on the current status and the authenticated user's role (resolved from `AuthStore`).

**Tech Stack:** Angular 21, TypeScript, Angular Material, Vitest, TestBed

> **⚠️ Implementation divergences from original plan:**
> - `TicketDetail` TypeScript interface includes `departmentId?: string`, `categoryId?: string` in addition to `departmentName`, `categoryName`
> - Department and category **names** are returned by the API (resolved server-side); the frontend displays them directly without additional lookups
> - Route is `/app/tickets/:id` (inside the `/app` shell)

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/ticket-detail/ticket-detail.component.ts` |
| Create | `src/app/tickets/ticket-detail/ticket-detail.component.html` |
| Create | `src/app/tickets/ticket-detail/ticket-detail.component.spec.ts` |
| Modify | `src/app/tickets/ticket.service.ts` (add `getById`) |

---

## Task 1: TicketService — getById()

> Note: Depends on `TicketService` existing from US-FE-009.

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/tickets/ticket.service.spec.ts
describe('getById()', () => {
  it('should GET /api/tickets/:id and return a TicketDetail', () => {
    const mock = {
      id: 't-1',
      ticketNumber: 'TK-0001',
      subject: 'Login issue',
      status: 'New',
      priority: 'High',
      slaUrgency: 'Warning',
      customerId: 'c-1',
      customerName: 'Alice',
      departmentId: 'dept-1',
      departmentName: 'Support',
      createdAt: '2025-01-01T10:00:00Z',
    };

    service.getById('t-1').subscribe(res => expect(res).toEqual(mock as any));
    const req = httpMock.expectOne('/api/tickets/t-1');
    expect(req.request.method).toBe('GET');
    req.flush(mock);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: FAIL — `service.getById` is not a function.

- [ ] **Step 3: Add getById() to TicketService**

```typescript
// Add inside TicketService class in src/app/tickets/ticket.service.ts

export interface TicketDetail extends TicketSummary {
  departmentId?: string;
  categoryId?: string;
  categoryName?: string;
  description?: string;
  assignedAgentId?: string;
  resolutionText?: string;
  customFields?: Record<string, unknown>;
  updatedAt?: string;
}

// Inside the class body:
getById(id: string): Observable<TicketDetail> {
  return this.http.get<TicketDetail>(`${this.baseUrl}/${id}`);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: All TicketService tests PASS (5 total).

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket.service.ts src/app/tickets/ticket.service.spec.ts
git commit -m "feat(tickets): add TicketService.getById()"
```

---

## Task 2: TicketDetailComponent shell

> Note: Depends on Task 1. Tests use `ActivatedRoute` stub to supply the ticket `id` param.

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket-detail/ticket-detail.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { TicketDetailComponent } from './ticket-detail.component';
import { TicketService, TicketDetail } from '../ticket.service';

const mockTicket: TicketDetail = {
  id: 't-1',
  ticketNumber: 'TK-0001',
  subject: 'Login issue',
  customerName: 'Alice',
  customerId: 'c-1',
  status: 'New',
  priority: 'High',
  slaUrgency: 'Warning',
  departmentId: 'dept-1',
  departmentName: 'Support',
  categoryName: 'Authentication',
  description: 'Cannot log in.',
  createdAt: '2025-01-01T10:00:00Z',
};

describe('TicketDetailComponent', () => {
  let fixture: ComponentFixture<TicketDetailComponent>;
  let component: TicketDetailComponent;
  let ticketServiceSpy: jasmine.SpyObj<TicketService>;

  const activatedRouteStub = {
    paramMap: of(new Map([['id', 't-1']])),
    snapshot: { paramMap: { get: (k: string) => (k === 'id' ? 't-1' : null) } },
  };

  beforeEach(async () => {
    ticketServiceSpy = jasmine.createSpyObj('TicketService', ['getById']);
    ticketServiceSpy.getById.and.returnValue(of(mockTicket));

    await TestBed.configureTestingModule({
      imports: [TicketDetailComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketServiceSpy },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call TicketService.getById with the route id on init', () => {
    expect(ticketServiceSpy.getById).toHaveBeenCalledWith('t-1');
  });

  it('should populate the ticket signal with API response', () => {
    expect(component.ticket()).toEqual(mockTicket);
  });

  it('should set loading to false after data loads', () => {
    expect(component.loading()).toBeFalse();
  });

  it('should expose the ticket subject', () => {
    expect(component.ticket()?.subject).toBe('Login issue');
  });

  it('should expose the ticket status', () => {
    expect(component.ticket()?.status).toBe('New');
  });

  it('should expose the ticket priority', () => {
    expect(component.ticket()?.priority).toBe('High');
  });

  it('should expose the customer name', () => {
    expect(component.ticket()?.customerName).toBe('Alice');
  });

  it('should expose department name', () => {
    expect(component.ticket()?.departmentName).toBe('Support');
  });

  it('should expose active tab defaulting to messages', () => {
    expect(component.activeTab()).toBe('messages');
  });

  it('should switch active tab when setActiveTab is called', () => {
    component.setActiveTab('history');
    expect(component.activeTab()).toBe('history');
  });

  it('should expose aiPanelOpen signal defaulting to false', () => {
    expect(component.aiPanelOpen()).toBeFalse();
  });

  it('should toggle aiPanelOpen when toggleAiPanel() is called', () => {
    component.toggleAiPanel();
    expect(component.aiPanelOpen()).toBeTrue();
    component.toggleAiPanel();
    expect(component.aiPanelOpen()).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket-detail/ticket-detail.component.spec.ts --watch=false
```

Expected: FAIL — `TicketDetailComponent` does not exist yet.

- [ ] **Step 3: Implement TicketDetailComponent**

```typescript
// src/app/tickets/ticket-detail/ticket-detail.component.ts
import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, switchMap } from 'rxjs/operators';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { TicketService, TicketDetail, TicketStatus } from '../ticket.service';

export type TabName = 'messages' | 'history' | 'attachments';

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule,
    MatTooltipModule,
    MatMenuModule,
  ],
  template: `
    @if (loading()) {
      <div class="loading-center">
        <mat-spinner diameter="56" />
      </div>
    } @else if (ticket()) {
      <div class="ticket-detail-layout">

        <!-- Action Bar -->
        <div class="action-bar">
          <span class="ticket-number">{{ ticket()!.ticketNumber }}</span>
          <span class="ticket-subject">{{ ticket()!.subject }}</span>
          <div class="action-buttons">
            <button mat-stroked-button (click)="onAssign()">Assign</button>
            <button mat-stroked-button (click)="onTransfer()">Transfer</button>
            <button mat-stroked-button color="warn" (click)="onEscalate()">Escalate</button>
            <button mat-flat-button [matMenuTriggerFor]="statusMenu">Change Status</button>
            <mat-menu #statusMenu>
              @for (s of availableNextStatuses(); track s) {
                <button mat-menu-item (click)="onChangeStatus(s)">{{ s }}</button>
              }
            </mat-menu>
            <button mat-flat-button color="primary" (click)="onClose()">Close</button>
          </div>
          <button mat-icon-button (click)="toggleAiPanel()" matTooltip="AI Assistance">
            <mat-icon>auto_awesome</mat-icon>
          </button>
        </div>

        <!-- Three-panel layout -->
        <div class="panels">

          <!-- Left: Metadata -->
          <aside class="panel-metadata">
            <h3>Details</h3>
            <dl>
              <dt>Status</dt>
              <dd>
                <span class="status-badge status-{{ ticket()!.status.toLowerCase() }}">
                  {{ ticket()!.status }}
                </span>
              </dd>
              <dt>Priority</dt>
              <dd>{{ ticket()!.priority }}</dd>
              <dt>SLA</dt>
              <dd>{{ ticket()!.slaUrgency }}</dd>
              <dt>Department</dt>
              <dd>{{ ticket()!.departmentName ?? '—' }}</dd>
              <dt>Category</dt>
              <dd>{{ ticket()!.categoryName ?? '—' }}</dd>
              <dt>Agent</dt>
              <dd>{{ ticket()!.assignedAgentName ?? 'Unassigned' }}</dd>
              <dt>Customer</dt>
              <dd>{{ ticket()!.customerName }}</dd>
              <dt>Created</dt>
              <dd>{{ ticket()!.createdAt | date:'medium' }}</dd>
            </dl>

            @if (ticket()!.customFields) {
              <mat-divider />
              <h4>Custom Fields</h4>
              @for (entry of customFieldEntries(); track entry.key) {
                <div class="custom-field">
                  <span class="cf-label">{{ entry.key }}</span>
                  <span class="cf-value">{{ entry.value }}</span>
                </div>
              }
            }
          </aside>

          <!-- Centre: Thread + Tabs -->
          <main class="panel-thread">
            <div class="tabs">
              <button
                [class.active]="activeTab() === 'messages'"
                (click)="setActiveTab('messages')"
              >Messages</button>
              <button
                [class.active]="activeTab() === 'history'"
                (click)="setActiveTab('history')"
              >History</button>
              <button
                [class.active]="activeTab() === 'attachments'"
                (click)="setActiveTab('attachments')"
              >Attachments</button>
            </div>

            <div class="tab-content">
              @if (activeTab() === 'messages') {
                <!-- MessageThreadComponent will be inserted here (US-FE-013) -->
                <ng-content select="[slot=thread]" />
              }
              @if (activeTab() === 'history') {
                <p>Audit history will appear here.</p>
              }
              @if (activeTab() === 'attachments') {
                <p>Attachments will appear here.</p>
              }
            </div>
          </main>

          <!-- Right: AI Assistance (conditionally shown) -->
          @if (aiPanelOpen()) {
            <aside class="panel-ai">
              <h3>AI Assistance</h3>
              <button mat-stroked-button>Summarize</button>
              <button mat-stroked-button>Suggest Reply</button>
              <button mat-stroked-button>Suggest Articles</button>
              <button mat-stroked-button>Suggest Category</button>
            </aside>
          }
        </div>
      </div>
    } @else {
      <p class="not-found">Ticket not found.</p>
    }
  `,
})
export class TicketDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly ticketService = inject(TicketService);
  protected readonly router = inject(Router);

  readonly ticket = signal<TicketDetail | null>(null);
  readonly loading = signal(true);
  readonly activeTab = signal<TabName>('messages');
  readonly aiPanelOpen = signal(false);

  private readonly destroy$ = new Subject<void>();

  private readonly statusTransitions: Record<TicketStatus, TicketStatus[]> = {
    New: ['Assigned', 'Closed'],
    Assigned: ['InProgress', 'OnHold', 'Closed'],
    InProgress: ['OnHold', 'Resolved', 'Escalated'],
    OnHold: ['InProgress', 'Closed'],
    Escalated: ['InProgress', 'Resolved'],
    Resolved: ['Closed'],
    Closed: [],
  };

  availableNextStatuses(): TicketStatus[] {
    const current = this.ticket()?.status;
    if (!current) return [];
    return this.statusTransitions[current] ?? [];
  }

  customFieldEntries(): { key: string; value: unknown }[] {
    const cf = this.ticket()?.customFields;
    if (!cf) return [];
    return Object.entries(cf).map(([key, value]) => ({ key, value }));
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.ticketService.getById(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: t => {
        this.ticket.set(t);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setActiveTab(tab: TabName): void {
    this.activeTab.set(tab);
  }

  toggleAiPanel(): void {
    this.aiPanelOpen.update(v => !v);
  }

  onAssign(): void { /* opens AssignModalComponent (US-FE-012) */ }
  onTransfer(): void { /* opens TransferModalComponent (US-FE-012) */ }
  onEscalate(): void { /* opens EscalateModalComponent (US-FE-012) */ }
  onChangeStatus(status: TicketStatus): void { /* opens StatusChangeModalComponent (US-FE-012) */ }
  onClose(): void { /* triggers Close flow */ }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket-detail/ticket-detail.component.spec.ts --watch=false
```

Expected: 13 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket-detail/
git commit -m "feat(tickets): implement TicketDetailComponent shell with 3-panel layout and AI panel toggle"
```

---

## Task 3: Integration smoke test

- [ ] **Step 1: Run all ticket detail tests**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --include=src/app/tickets/ticket-detail/ticket-detail.component.spec.ts --watch=false
```

Expected: All tests PASS.

- [ ] **Step 2: Commit**

```bash
git commit -m "feat(tickets): US-FE-010 complete — ticket detail page shell"
```
