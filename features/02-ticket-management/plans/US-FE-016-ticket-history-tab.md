# Ticket History Tab — Implementation Plan

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

**Story:** US-FE-016  
**Goal:** Implement the History tab on the ticket detail page showing a chronological, oldest-first audit trail of all ticket lifecycle events with action-type icons, actor details, and timestamps — hidden from customers.

**Architecture:** `TicketHistoryComponent` is a standalone component rendered inside the ticket detail shell's "History" tab. It receives the `ticketId` via `@Input` and fetches data from `TicketService.getHistory(ticketId)` on init. Entries are displayed in a scrollable list sorted oldest-first (guaranteed by the API); each action type maps to a distinct `MatIcon` and colour class via a pure mapping function. The component is conditionally rendered by the parent shell when `currentUser.role !== 'Customer'`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/ticket-history/ticket-history.component.ts` |
| Create | `src/app/tickets/ticket-history/ticket-history.component.spec.ts` |

---

## Task 1: TicketService — getHistory method

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket.service.spec.ts  (append to existing describe block)
import { TicketHistory } from './ticket.service';

describe('TicketService — getHistory', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  const TICKET_ID = 'ticket-42';

  const mockHistory: TicketHistory[] = [
    {
      id: 'h-1',
      action: 'StatusChanged',
      label: 'Status changed: InProgress → OnHold',
      actorName: 'Alice Agent',
      actorRole: 'Agent',
      timestamp: '2026-08-01T09:00:00Z',
    },
    {
      id: 'h-2',
      action: 'PriorityChanged',
      label: 'Priority changed: Medium → High',
      actorName: 'Bob Supervisor',
      actorRole: 'Supervisor',
      timestamp: '2026-08-01T10:30:00Z',
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should GET /api/tickets/{id}/history', () => {
    service.getHistory(TICKET_ID).subscribe(history => {
      expect(history.length).toBe(2);
      expect(history[0].action).toBe('StatusChanged');
      expect(history[1].action).toBe('PriorityChanged');
    });

    const req = httpMock.expectOne(`/api/tickets/${TICKET_ID}/history`);
    expect(req.request.method).toBe('GET');
    req.flush(mockHistory);
  });

  it('should return empty array when no history exists', () => {
    service.getHistory(TICKET_ID).subscribe(history => {
      expect(history).toEqual([]);
    });
    const req = httpMock.expectOne(`/api/tickets/${TICKET_ID}/history`);
    req.flush([]);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: FAIL (getHistory not yet implemented)

- [ ] **Step 3: Implement getHistory on TicketService**

```typescript
// src/app/tickets/ticket.service.ts  (add interface + method)

export interface TicketHistory {
  id: string;
  action:
    | 'StatusChanged'
    | 'PriorityChanged'
    | 'AssigneeChanged'
    | 'NoteAdded'
    | 'AttachmentAdded'
    | 'AttachmentDeleted'
    | 'TicketCreated'
    | 'TicketClosed'
    | 'TicketReopened'
    | 'TagAdded'
    | 'TagRemoved';
  label: string;
  actorName: string;
  actorRole: string;
  timestamp: string;
}

// Inside TicketService class:
  getHistory(ticketId: string): Observable<TicketHistory[]> {
    return this.http.get<TicketHistory[]>(`/api/tickets/${ticketId}/history`);
  }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket.service.ts src/app/tickets/ticket.service.spec.ts
git commit -m "feat(tickets): add getHistory service method (US-FE-016)"
```

---

## Task 2: TicketHistoryComponent — render audit trail

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket-history/ticket-history.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TicketHistoryComponent } from './ticket-history.component';
import { TicketService, TicketHistory } from '../ticket.service';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';

const MOCK_HISTORY: TicketHistory[] = [
  {
    id: 'h-1',
    action: 'TicketCreated',
    label: 'Ticket created',
    actorName: 'Customer Jane',
    actorRole: 'Customer',
    timestamp: '2026-07-01T08:00:00Z',
  },
  {
    id: 'h-2',
    action: 'StatusChanged',
    label: 'Status changed: New → InProgress',
    actorName: 'Alice Agent',
    actorRole: 'Agent',
    timestamp: '2026-07-01T09:15:00Z',
  },
  {
    id: 'h-3',
    action: 'NoteAdded',
    label: 'Note added',
    actorName: 'Alice Agent',
    actorRole: 'Agent',
    timestamp: '2026-07-01T10:00:00Z',
  },
];

describe('TicketHistoryComponent', () => {
  let fixture: ComponentFixture<TicketHistoryComponent>;
  let component: TicketHistoryComponent;
  let ticketSvc: jasmine.SpyObj<TicketService>;

  beforeEach(async () => {
    ticketSvc = jasmine.createSpyObj('TicketService', ['getHistory']);
    ticketSvc.getHistory.and.returnValue(of(MOCK_HISTORY));

    await TestBed.configureTestingModule({
      imports: [TicketHistoryComponent, NoopAnimationsModule],
      providers: [{ provide: TicketService, useValue: ticketSvc }],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketHistoryComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-42';
    fixture.detectChanges();
  });

  it('should create and call getHistory on init', () => {
    expect(component).toBeTruthy();
    expect(ticketSvc.getHistory).toHaveBeenCalledWith('ticket-42');
  });

  it('should render all history entries', () => {
    const entries = fixture.debugElement.queryAll(By.css('[data-testid="history-entry"]'));
    expect(entries.length).toBe(3);
  });

  it('should display label and actor name in each entry', () => {
    const entries = fixture.debugElement.queryAll(By.css('[data-testid="history-entry"]'));
    expect(entries[0].nativeElement.textContent).toContain('Ticket created');
    expect(entries[0].nativeElement.textContent).toContain('Customer Jane');
    expect(entries[1].nativeElement.textContent).toContain('Status changed: New → InProgress');
    expect(entries[1].nativeElement.textContent).toContain('Alice Agent');
  });

  it('should display entries in DOM order (oldest first as returned by API)', () => {
    const labels = fixture.debugElement
      .queryAll(By.css('[data-testid="history-label"]'))
      .map(el => el.nativeElement.textContent.trim());
    expect(labels[0]).toContain('Ticket created');
    expect(labels[2]).toContain('Note added');
  });

  it('should apply distinct CSS class per action type', () => {
    const icons = fixture.debugElement.queryAll(By.css('[data-testid="action-icon"]'));
    expect(icons[0].classes['action-TicketCreated']).toBeTrue();
    expect(icons[1].classes['action-StatusChanged']).toBeTrue();
    expect(icons[2].classes['action-NoteAdded']).toBeTrue();
  });

  it('should show correct icon name per action', () => {
    expect(component.iconFor('StatusChanged')).toBe('swap_horiz');
    expect(component.iconFor('TicketCreated')).toBe('add_circle');
    expect(component.iconFor('NoteAdded')).toBe('note_add');
    expect(component.iconFor('AttachmentAdded')).toBe('attach_file');
    expect(component.iconFor('AssigneeChanged')).toBe('person');
    expect(component.iconFor('TicketClosed')).toBe('check_circle');
  });

  it('should show empty state when history is empty', async () => {
    ticketSvc.getHistory.and.returnValue(of([]));
    fixture = TestBed.createComponent(TicketHistoryComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-42';
    fixture.detectChanges();
    const empty = fixture.debugElement.query(By.css('[data-testid="empty-state"]'));
    expect(empty).not.toBeNull();
    expect(empty.nativeElement.textContent).toContain('No history');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket-history/ticket-history.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implement TicketHistoryComponent**

```typescript
// src/app/tickets/ticket-history/ticket-history.component.ts
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TicketService, TicketHistory } from '../ticket.service';

type ActionType = TicketHistory['action'];

const ACTION_ICONS: Record<ActionType, string> = {
  StatusChanged:     'swap_horiz',
  PriorityChanged:   'low_priority',
  AssigneeChanged:   'person',
  NoteAdded:         'note_add',
  AttachmentAdded:   'attach_file',
  AttachmentDeleted: 'attachment_off',
  TicketCreated:     'add_circle',
  TicketClosed:      'check_circle',
  TicketReopened:    'restart_alt',
  TagAdded:          'label',
  TagRemoved:        'label_off',
};

@Component({
  selector: 'app-ticket-history',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule,
    MatTooltipModule,
  ],
  template: `
    <div class="history-panel">
      @if (history().length === 0) {
        <p class="empty-state" data-testid="empty-state">No history available yet.</p>
      } @else {
        <ul class="history-list">
          @for (entry of history(); track entry.id) {
            <li class="history-entry" data-testid="history-entry">
              <mat-icon
                [class]="'action-icon action-' + entry.action"
                data-testid="action-icon"
                [matTooltip]="entry.action"
              >
                {{ iconFor(entry.action) }}
              </mat-icon>

              <div class="entry-body">
                <span class="history-label" data-testid="history-label">
                  {{ entry.label }}
                </span>
                <span class="entry-meta">
                  {{ entry.actorName }}
                  <span class="role-badge">{{ entry.actorRole }}</span>
                  &bull;
                  <time [attr.datetime]="entry.timestamp">
                    {{ entry.timestamp | date:'medium' }}
                  </time>
                </span>
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .history-panel { padding: 8px 0; }
    .history-list { list-style: none; margin: 0; padding: 0; }
    .history-entry {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 10px 0;
      border-bottom: 1px solid #f0f0f0;
    }
    .action-icon { flex-shrink: 0; margin-top: 2px; }
    .action-StatusChanged   { color: #1565c0; }
    .action-PriorityChanged { color: #e65100; }
    .action-AssigneeChanged { color: #6a1b9a; }
    .action-NoteAdded       { color: #2e7d32; }
    .action-AttachmentAdded { color: #00838f; }
    .action-AttachmentDeleted { color: #b71c1c; }
    .action-TicketCreated   { color: #1b5e20; }
    .action-TicketClosed    { color: #37474f; }
    .action-TicketReopened  { color: #f57f17; }
    .action-TagAdded        { color: #4527a0; }
    .action-TagRemoved      { color: #880e4f; }
    .entry-body { display: flex; flex-direction: column; }
    .history-label { font-weight: 500; font-size: 14px; }
    .entry-meta { font-size: 12px; color: #757575; margin-top: 2px; }
    .role-badge {
      background: #eeeeee;
      border-radius: 4px;
      padding: 1px 4px;
      font-size: 11px;
      margin: 0 4px;
    }
    .empty-state { color: #9e9e9e; text-align: center; margin-top: 24px; }
  `],
})
export class TicketHistoryComponent implements OnInit {
  @Input() ticketId!: string;

  private readonly ticketSvc = inject(TicketService);

  history = signal<TicketHistory[]>([]);

  ngOnInit(): void {
    this.ticketSvc.getHistory(this.ticketId).subscribe(entries => {
      this.history.set(entries);
    });
  }

  iconFor(action: ActionType): string {
    return ACTION_ICONS[action] ?? 'history';
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket-history/ticket-history.component.spec.ts --watch=false
```

Expected: 8 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket-history/
git commit -m "feat(tickets): implement TicketHistoryComponent audit trail (US-FE-016)"
```
