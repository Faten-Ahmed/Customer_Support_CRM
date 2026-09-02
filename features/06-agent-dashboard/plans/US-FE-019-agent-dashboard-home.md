# Agent Dashboard Home (My Tickets) — Implementation Plan

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

**Story:** US-FE-019
**Goal:** Implement `/dashboard` — the agent landing page with summary stat cards, a personal ticket table sorted by SLA urgency, availability status toggle, and a 60-second SLA badge refresh interval.

**Architecture:** `AgentDashboardComponent` is standalone, lazy-loaded at `/dashboard`. Summary cards use `AgentService.getDashboardStats()`. My Tickets table reuses the same `TicketService.list()` with a `assignedAgentId=me` filter. Availability is toggled via `AgentService.updateAvailability()`. A `setInterval` every 60 seconds re-fetches the ticket list to refresh SLA badges.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/dashboard/agent-dashboard/agent-dashboard.component.ts` |
| Create | `src/app/dashboard/agent-dashboard/agent-dashboard.component.html` |
| Create | `src/app/dashboard/agent-dashboard/agent-dashboard.component.spec.ts` |
| Create | `src/app/dashboard/services/agent.service.ts` |
| Create | `src/app/dashboard/services/agent.service.spec.ts` |
| Modify | `src/app/tickets/services/ticket.service.ts` |
| Modify | `src/app/tickets/services/ticket.service.spec.ts` |

---

## Task 1: AgentService and TicketService.getMyTickets()

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/dashboard/services/agent.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AgentService } from './agent.service';

describe('AgentService', () => {
  let service: AgentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AgentService],
    });
    service = TestBed.inject(AgentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getDashboardStats() should GET /api/v1/agent/dashboard', () => {
    service.getDashboardStats().subscribe(stats => {
      expect(stats.openTickets).toBeDefined();
    });
    const req = httpMock.expectOne('/api/v1/agent/dashboard');
    expect(req.request.method).toBe('GET');
    req.flush({ openTickets: 5, slaBreached: 1, onHold: 2, resolvedToday: 3 });
  });

  it('updateAvailability() should PATCH /api/v1/agents/me/availability', () => {
    service.updateAvailability('Busy').subscribe();
    const req = httpMock.expectOne('/api/v1/agents/me/availability');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ status: 'Busy' });
    req.flush({ status: 'Busy' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/dashboard/services/agent.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/dashboard/services/agent.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type AvailabilityStatus = 'Available' | 'Busy' | 'Away' | 'Offline';

export interface DashboardStats {
  openTickets: number;
  slaBreached: number;
  onHold: number;
  resolvedToday: number;
}

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);

  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>('/api/v1/agent/dashboard');
  }

  updateAvailability(status: AvailabilityStatus): Observable<{ status: AvailabilityStatus }> {
    return this.http.patch<{ status: AvailabilityStatus }>('/api/v1/agents/me/availability', { status });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/dashboard/services/agent.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/dashboard/services/
git commit -m "feat(dashboard): add AgentService with stats and availability (US-FE-019)"
```

---

## Task 2: AgentDashboardComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/dashboard/agent-dashboard/agent-dashboard.component.spec.ts

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AgentDashboardComponent } from './agent-dashboard.component';
import { AgentService, DashboardStats } from '../services/agent.service';
import { TicketService } from '../../tickets/services/ticket.service';
import { AuthStore } from '../../auth/auth.store';

const mockStats: DashboardStats = { openTickets: 5, slaBreached: 1, onHold: 2, resolvedToday: 3 };

describe('AgentDashboardComponent', () => {
  let fixture: ComponentFixture<AgentDashboardComponent>;
  let component: AgentDashboardComponent;
  let agentService: jasmine.SpyObj<AgentService>;
  let ticketService: jasmine.SpyObj<TicketService>;

  beforeEach(async () => {
    agentService = jasmine.createSpyObj('AgentService', ['getDashboardStats', 'updateAvailability']);
    ticketService = jasmine.createSpyObj('TicketService', ['list']);
    agentService.getDashboardStats.and.returnValue(of(mockStats));
    agentService.updateAvailability.and.returnValue(of({ status: 'Busy' as const }));
    ticketService.list.and.returnValue(of({ data: [], total: 0, page: 1, pageSize: 20 }));

    await TestBed.configureTestingModule({
      imports: [AgentDashboardComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: TicketService, useValue: ticketService },
        { provide: AuthStore, useValue: { user: () => ({ sub: 'agent-1', role: 'Agent' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AgentDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => component.ngOnDestroy());

  it('should create and display stats', () => {
    expect(component).toBeTruthy();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('5');
  });

  it('should call updateAvailability on toggle', () => {
    component.setAvailability('Busy');
    expect(agentService.updateAvailability).toHaveBeenCalledWith('Busy');
    expect(component.availability()).toBe('Busy');
  });

  it('should refresh tickets every 60 seconds', fakeAsync(() => {
    expect(ticketService.list).toHaveBeenCalledTimes(1);
    tick(60000);
    expect(ticketService.list).toHaveBeenCalledTimes(2);
    tick(60000);
    expect(ticketService.list).toHaveBeenCalledTimes(3);
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/dashboard/agent-dashboard/agent-dashboard.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/dashboard/agent-dashboard/agent-dashboard.component.ts

import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { AgentService, AvailabilityStatus, DashboardStats } from '../services/agent.service';
import { TicketService, Ticket } from '../../tickets/services/ticket.service';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatMenuModule, MatTableModule, MatIconModule],
  templateUrl: './agent-dashboard.component.html',
})
export class AgentDashboardComponent implements OnInit, OnDestroy {
  private readonly agentService = inject(AgentService);
  private readonly ticketService = inject(TicketService);
  private readonly authStore = inject(AuthStore);

  readonly stats = signal<DashboardStats | null>(null);
  readonly myTickets = signal<Ticket[]>([]);
  readonly availability = signal<AvailabilityStatus>('Available');
  readonly loading = signal(false);

  private refreshInterval?: ReturnType<typeof setInterval>;
  displayedColumns = ['id', 'subject', 'priority', 'status', 'sla'];

  ngOnInit(): void {
    this.loadStats();
    this.loadMyTickets();
    this.refreshInterval = setInterval(() => this.loadMyTickets(), 60000);
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  private loadStats(): void {
    this.agentService.getDashboardStats().subscribe(stats => this.stats.set(stats));
  }

  loadMyTickets(): void {
    const agentId = this.authStore.user()?.sub;
    if (!agentId) return;
    this.ticketService.list({ page: 1, pageSize: 20, assignedAgentId: agentId }).subscribe({
      next: res => this.myTickets.set(res.data),
    });
  }

  setAvailability(status: AvailabilityStatus): void {
    this.agentService.updateAvailability(status).subscribe({
      next: () => this.availability.set(status),
    });
  }
}
```

```html
<!-- src/app/dashboard/agent-dashboard/agent-dashboard.component.html -->

<div class="p-6">
  <div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-semibold">My Dashboard</h1>
    <div class="flex items-center gap-3">
      <button mat-stroked-button [matMenuTriggerFor]="availMenu">
        <mat-icon>circle</mat-icon> {{ availability() }}
      </button>
      <mat-menu #availMenu="matMenu">
        @for (s of ['Available','Busy','Away','Offline']; track s) {
          <button mat-menu-item (click)="setAvailability(s)">{{ s }}</button>
        }
      </mat-menu>
      <button mat-raised-button routerLink="/tickets/new">New Ticket</button>
      <button mat-stroked-button routerLink="/tickets/unassigned">Unassigned Queue</button>
    </div>
  </div>

  <!-- Summary Cards -->
  @if (stats()) {
    <div class="grid grid-cols-4 gap-4 mb-6">
      <mat-card class="p-4 text-center">
        <p class="text-3xl font-bold text-blue-600">{{ stats()!.openTickets }}</p>
        <p class="text-sm text-gray-500">Open Tickets</p>
      </mat-card>
      <mat-card class="p-4 text-center">
        <p class="text-3xl font-bold text-red-600">{{ stats()!.slaBreached }}</p>
        <p class="text-sm text-gray-500">SLA Breached</p>
      </mat-card>
      <mat-card class="p-4 text-center">
        <p class="text-3xl font-bold text-yellow-600">{{ stats()!.onHold }}</p>
        <p class="text-sm text-gray-500">On Hold</p>
      </mat-card>
      <mat-card class="p-4 text-center">
        <p class="text-3xl font-bold text-green-600">{{ stats()!.resolvedToday }}</p>
        <p class="text-sm text-gray-500">Resolved Today</p>
      </mat-card>
    </div>
  }

  <!-- My Tickets Table -->
  <h2 class="text-lg font-semibold mb-3">My Tickets</h2>
  @if (myTickets().length === 0) {
    <p class="text-gray-500 text-center py-6">No tickets assigned to you.</p>
  } @else {
    <mat-table [dataSource]="myTickets()">
      <ng-container matColumnDef="id">
        <mat-header-cell *matHeaderCellDef>#</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.id }}</mat-cell>
      </ng-container>
      <ng-container matColumnDef="subject">
        <mat-header-cell *matHeaderCellDef>Subject</mat-header-cell>
        <mat-cell *matCellDef="let t">
          <a [routerLink]="['/tickets', t.id]" class="text-blue-600 hover:underline">{{ t.subject }}</a>
        </mat-cell>
      </ng-container>
      <ng-container matColumnDef="priority">
        <mat-header-cell *matHeaderCellDef>Priority</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.priority }}</mat-cell>
      </ng-container>
      <ng-container matColumnDef="status">
        <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.status }}</mat-cell>
      </ng-container>
      <ng-container matColumnDef="sla">
        <mat-header-cell *matHeaderCellDef>SLA</mat-header-cell>
        <mat-cell *matCellDef="let t">—</mat-cell>
      </ng-container>
      <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
      <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
    </mat-table>
  }
</div>
```

Extend `TicketService.list()` to accept `assignedAgentId` query param:
```typescript
// In TicketService.list() — add to existing method or create a new overload
list(query: { page: number; pageSize: number; assignedAgentId?: string; [key: string]: unknown }): Observable<{ data: Ticket[]; total: number; page: number; pageSize: number }> {
  let params = new HttpParams().set('page', String(query.page)).set('pageSize', String(query.pageSize));
  if (query.assignedAgentId) params = params.set('assignedAgentId', query.assignedAgentId);
  return this.http.get<{ data: Ticket[]; total: number; page: number; pageSize: number }>('/api/v1/tickets', { params });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/dashboard/agent-dashboard/agent-dashboard.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/dashboard/agent-dashboard/
git commit -m "feat(dashboard): implement AgentDashboardComponent with stats and auto-refresh (US-FE-019)"
```
