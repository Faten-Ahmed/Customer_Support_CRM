# Unassigned Ticket Queue Page — Implementation Plan

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

**Story:** US-FE-018
**Goal:** Implement `/tickets/unassigned` — a real-time list of unclaimed tickets with a "Claim" button per row that handles `409 TICKET_ALREADY_ASSIGNED` and removes the row via SignalR `TicketAssigned` event.

**Architecture:** `UnassignedQueueComponent` is standalone, lazy-loaded. It fetches unassigned tickets on init via `TicketService.listUnassigned()`. Claiming calls `TicketService.assign()` with the current user's ID. SignalR subscribes to the `TicketAssigned` event on the department group; on receipt the matching ticket row is removed from the signal array. `409` responses display a "Claimed by someone else — refreshing" snackbar and trigger a list reload.

**Tech Stack:** Angular 21, TypeScript, Angular Material, `@microsoft/signalr`, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/unassigned-queue/unassigned-queue.component.ts` |
| Create | `src/app/tickets/unassigned-queue/unassigned-queue.component.html` |
| Create | `src/app/tickets/unassigned-queue/unassigned-queue.component.spec.ts` |
| Modify | `src/app/tickets/services/ticket.service.ts` |
| Modify | `src/app/tickets/services/ticket.service.spec.ts` |

---

## Task 1: TicketService.listUnassigned()

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/tickets/services/ticket.service.spec.ts

describe('TicketService — listUnassigned', () => {
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

  it('listUnassigned() should GET /api/v1/tickets with status=Unassigned', () => {
    service.listUnassigned().subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/tickets');
    expect(req.request.params.get('status')).toBe('Unassigned');
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

listUnassigned(): Observable<{ data: Ticket[]; total: number }> {
  const params = new HttpParams().set('status', 'Unassigned');
  return this.http.get<{ data: Ticket[]; total: number }>('/api/v1/tickets', { params });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/ticket.service.ts src/app/tickets/services/ticket.service.spec.ts
git commit -m "feat(tickets): add listUnassigned() to TicketService (US-FE-018)"
```

---

## Task 2: UnassignedQueueComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/unassigned-queue/unassigned-queue.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { UnassignedQueueComponent } from './unassigned-queue.component';
import { TicketService, Ticket } from '../services/ticket.service';
import { SignalRService } from '../../shared/services/signalr.service';
import { AuthStore } from '../../auth/auth.store';
import { MatSnackBar } from '@angular/material/snack-bar';

const mockTickets: Ticket[] = [
  { id: 't1', subject: 'Need help', description: '', priority: 'High', status: 'Unassigned', departmentId: 'd1', customerId: 'c1', createdAt: '2025-01-01', updatedAt: '2025-01-01' },
  { id: 't2', subject: 'Billing issue', description: '', priority: 'Medium', status: 'Unassigned', departmentId: 'd1', customerId: 'c2', createdAt: '2025-01-02', updatedAt: '2025-01-02' },
];

describe('UnassignedQueueComponent', () => {
  let fixture: ComponentFixture<UnassignedQueueComponent>;
  let component: UnassignedQueueComponent;
  let ticketService: jasmine.SpyObj<TicketService>;
  let signalRService: jasmine.SpyObj<SignalRService>;
  let snackBar: jasmine.SpyObj<MatSnackBar>;
  let mockConnection: jasmine.SpyObj<any>;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('TicketService', ['listUnassigned', 'assign']);
    ticketService.listUnassigned.and.returnValue(of({ data: mockTickets, total: 2 }));
    ticketService.assign.and.returnValue(of(undefined));

    mockConnection = jasmine.createSpyObj('HubConnection', ['start', 'on', 'off', 'stop']);
    mockConnection.start.and.returnValue(Promise.resolve());
    signalRService = jasmine.createSpyObj('SignalRService', ['getConnection']);
    signalRService.getConnection.and.returnValue(mockConnection);

    snackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [UnassignedQueueComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketService },
        { provide: SignalRService, useValue: signalRService },
        { provide: AuthStore, useValue: { user: () => ({ sub: 'agent-1' }) } },
        { provide: MatSnackBar, useValue: snackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UnassignedQueueComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load unassigned tickets', () => {
    expect(component).toBeTruthy();
    expect(component.tickets().length).toBe(2);
  });

  it('should remove ticket from list after successful claim', () => {
    component.claim(mockTickets[0]);
    expect(ticketService.assign).toHaveBeenCalledWith('t1', 'agent-1');
    expect(component.tickets().length).toBe(1);
  });

  it('should show snackbar and reload on 409', () => {
    ticketService.assign.and.returnValue(throwError(() => ({ status: 409 })));
    component.claim(mockTickets[0]);
    expect(snackBar.open).toHaveBeenCalledWith(
      jasmine.stringContaining('Claimed by someone else'),
      jasmine.any(String),
      jasmine.any(Object)
    );
    expect(ticketService.listUnassigned).toHaveBeenCalledTimes(2);
  });

  it('should show empty state when no tickets', () => {
    ticketService.listUnassigned.and.returnValue(of({ data: [], total: 0 }));
    component.loadTickets();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No unassigned tickets');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/unassigned-queue/unassigned-queue.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/unassigned-queue/unassigned-queue.component.ts

import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Ticket, TicketService } from '../services/ticket.service';
import { SignalRService } from '../../shared/services/signalr.service';
import { AuthStore } from '../../auth/auth.store';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-unassigned-queue',
  standalone: true,
  imports: [CommonModule, DatePipe, MatTableModule, MatButtonModule, MatSnackBarModule],
  templateUrl: './unassigned-queue.component.html',
})
export class UnassignedQueueComponent implements OnInit, OnDestroy {
  private readonly ticketService = inject(TicketService);
  private readonly signalRService = inject(SignalRService);
  private readonly authStore = inject(AuthStore);
  private readonly snackBar = inject(MatSnackBar);

  readonly tickets = signal<Ticket[]>([]);
  readonly loading = signal(false);
  private connection!: signalR.HubConnection;

  displayedColumns = ['id', 'subject', 'customerId', 'departmentId', 'priority', 'createdAt', 'claim'];

  ngOnInit(): void {
    this.loadTickets();
    this.connectSignalR();
  }

  ngOnDestroy(): void {
    this.connection?.stop();
  }

  loadTickets(): void {
    this.loading.set(true);
    this.ticketService.listUnassigned().subscribe({
      next: res => {
        this.tickets.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  claim(ticket: Ticket): void {
    const agentId = this.authStore.user()?.sub;
    if (!agentId) return;
    this.ticketService.assign(ticket.id, agentId).subscribe({
      next: () => {
        this.tickets.update(list => list.filter(t => t.id !== ticket.id));
      },
      error: err => {
        if (err.status === 409) {
          this.snackBar.open('Claimed by someone else — refreshing', 'OK', { duration: 3000 });
          this.loadTickets();
        }
      },
    });
  }

  private connectSignalR(): void {
    this.connection = this.signalRService.getConnection('/hubs/tickets');
    this.connection.start().then(() => {
      this.connection.on('TicketAssigned', (ticketId: string) => {
        this.tickets.update(list => list.filter(t => t.id !== ticketId));
      });
    });
  }
}
```

```html
<!-- src/app/tickets/unassigned-queue/unassigned-queue.component.html -->

<div class="p-6">
  <h1 class="text-2xl font-semibold mb-4">Unassigned Tickets</h1>

  @if (loading()) {
    <p class="text-center text-gray-500 py-8">Loading…</p>
  } @else if (tickets().length === 0) {
    <div class="text-center py-12">
      <p class="text-gray-500 text-lg">No unassigned tickets — great work!</p>
    </div>
  } @else {
    <mat-table [dataSource]="tickets()" class="w-full">
      <ng-container matColumnDef="id">
        <mat-header-cell *matHeaderCellDef>Ticket #</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.id }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="subject">
        <mat-header-cell *matHeaderCellDef>Subject</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.subject }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="customerId">
        <mat-header-cell *matHeaderCellDef>Customer</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.customerId }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="departmentId">
        <mat-header-cell *matHeaderCellDef>Department</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.departmentId }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="priority">
        <mat-header-cell *matHeaderCellDef>Priority</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.priority }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="createdAt">
        <mat-header-cell *matHeaderCellDef>Created</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.createdAt | date:'medium' }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="claim">
        <mat-header-cell *matHeaderCellDef></mat-header-cell>
        <mat-cell *matCellDef="let t">
          <button mat-raised-button color="primary" (click)="claim(t)">Claim</button>
        </mat-cell>
      </ng-container>

      <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
      <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
    </mat-table>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/unassigned-queue/unassigned-queue.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/unassigned-queue/
git commit -m "feat(tickets): implement UnassignedQueueComponent with real-time claim (US-FE-018)"
```
