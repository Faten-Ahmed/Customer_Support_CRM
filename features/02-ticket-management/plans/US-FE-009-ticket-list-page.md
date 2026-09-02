# Ticket List Page (Agent) — Implementation Plan

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

**Story:** US-FE-009  
**Goal:** Implement the `/tickets` page with a filterable, searchable, paginated ticket table with SLA indicators and priority/status badges.

**Architecture:** `TicketListComponent` is a standalone Angular component backed by `TicketService.list(params)` which calls `GET /api/tickets`. Filter state is held in Angular Signals and fed into the HTTP call on change. The table uses `MatTable` with `MatPaginator` and `MatSort`. SLA colour is computed from urgency enum values returned by the API.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/ticket.service.ts` |
| Create | `src/app/tickets/ticket.service.spec.ts` |
| Create | `src/app/tickets/ticket-list/ticket-list.component.ts` |
| Create | `src/app/tickets/ticket-list/ticket-list.component.html` |
| Create | `src/app/tickets/ticket-list/ticket-list.component.spec.ts` |

---

## Task 1: TicketService — list()

> Note: No dependencies. Establish the service and its query parameter contract first.

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket.service.spec.ts
import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { TicketService, TicketListParams, TicketListResponse } from './ticket.service';

describe('TicketService', () => {
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

  describe('list()', () => {
    it('should GET /api/tickets with no params', () => {
      const mockResponse: TicketListResponse = { items: [], total: 0, page: 1, pageSize: 20 };
      service.list({}).subscribe(res => expect(res).toEqual(mockResponse));

      const req = httpMock.expectOne(r => r.url === '/api/tickets');
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should pass status filter as repeated query params', () => {
      const params: TicketListParams = { status: ['New', 'InProgress'], page: 1, pageSize: 20 };
      service.list(params).subscribe();

      const req = httpMock.expectOne(r => r.url === '/api/tickets');
      expect(req.request.params.getAll('status')).toEqual(['New', 'InProgress']);
      req.flush({ items: [], total: 0, page: 1, pageSize: 20 });
    });

    it('should pass search, priority, and department params', () => {
      const params: TicketListParams = {
        search: 'login issue',
        priority: 'High',
        departmentId: 'dept-1',
        page: 2,
        pageSize: 20,
      };
      service.list(params).subscribe();

      const req = httpMock.expectOne(r => r.url === '/api/tickets');
      expect(req.request.params.get('search')).toBe('login issue');
      expect(req.request.params.get('priority')).toBe('High');
      expect(req.request.params.get('departmentId')).toBe('dept-1');
      expect(req.request.params.get('page')).toBe('2');
      req.flush({ items: [], total: 0, page: 2, pageSize: 20 });
    });

    it('should pass date range params when provided', () => {
      const params: TicketListParams = {
        dateFrom: '2025-01-01',
        dateTo: '2025-01-31',
      };
      service.list(params).subscribe();

      const req = httpMock.expectOne(r => r.url === '/api/tickets');
      expect(req.request.params.get('dateFrom')).toBe('2025-01-01');
      expect(req.request.params.get('dateTo')).toBe('2025-01-31');
      req.flush({ items: [], total: 0, page: 1, pageSize: 20 });
    });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: FAIL — `TicketService` and `TicketListParams` do not exist yet.

- [ ] **Step 3: Implement TicketService**

```typescript
// src/app/tickets/ticket.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type TicketStatus = 'New' | 'Assigned' | 'InProgress' | 'OnHold' | 'Escalated' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type SlaUrgency = 'Normal' | 'Warning' | 'Breached';

export interface TicketSummary {
  id: string;
  ticketNumber: string;
  subject: string;
  customerName: string;
  customerId: string;
  status: TicketStatus;
  priority: TicketPriority;
  slaUrgency: SlaUrgency;
  assignedAgentName?: string;
  departmentName?: string;
  createdAt: string;
}

export interface TicketListParams {
  search?: string;
  status?: TicketStatus[];
  priority?: TicketPriority;
  departmentId?: string;
  categoryId?: string;
  assignedAgentId?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

export interface TicketListResponse {
  items: TicketSummary[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/tickets';

  list(params: TicketListParams): Observable<TicketListResponse> {
    let httpParams = new HttpParams();

    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.priority) httpParams = httpParams.set('priority', params.priority);
    if (params.departmentId) httpParams = httpParams.set('departmentId', params.departmentId);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.assignedAgentId) httpParams = httpParams.set('assignedAgentId', params.assignedAgentId);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);

    if (params.status?.length) {
      params.status.forEach(s => {
        httpParams = httpParams.append('status', s);
      });
    }

    return this.http.get<TicketListResponse>(this.baseUrl, { params: httpParams });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket.service.ts src/app/tickets/ticket.service.spec.ts
git commit -m "feat(tickets): add TicketService.list() with full query param support"
```

---

## Task 2: TicketListComponent

> Note: Depends on Task 1 (TicketService must exist).

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket-list/ticket-list.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { TicketListComponent } from './ticket-list.component';
import { TicketService, TicketListResponse, TicketSummary } from '../ticket.service';

const mockTickets: TicketSummary[] = [
  {
    id: 't-1',
    ticketNumber: 'TK-0001',
    subject: 'Cannot login',
    customerName: 'Alice',
    customerId: 'c-1',
    status: 'New',
    priority: 'High',
    slaUrgency: 'Warning',
    assignedAgentName: undefined,
    departmentName: 'Support',
    createdAt: '2025-01-01T10:00:00Z',
  },
  {
    id: 't-2',
    ticketNumber: 'TK-0002',
    subject: 'Billing question',
    customerName: 'Bob',
    customerId: 'c-2',
    status: 'InProgress',
    priority: 'Medium',
    slaUrgency: 'Normal',
    assignedAgentName: 'Carol',
    departmentName: 'Billing',
    createdAt: '2025-01-02T09:00:00Z',
  },
];

const mockResponse: TicketListResponse = { items: mockTickets, total: 2, page: 1, pageSize: 20 };

describe('TicketListComponent', () => {
  let fixture: ComponentFixture<TicketListComponent>;
  let component: TicketListComponent;
  let ticketServiceSpy: jasmine.SpyObj<TicketService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    ticketServiceSpy = jasmine.createSpyObj('TicketService', ['list']);
    ticketServiceSpy.list.and.returnValue(of(mockResponse));
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [TicketListComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call TicketService.list on init', () => {
    expect(ticketServiceSpy.list).toHaveBeenCalled();
  });

  it('should populate tickets signal with response items', () => {
    expect(component.tickets()).toHaveSize(2);
    expect(component.tickets()[0].ticketNumber).toBe('TK-0001');
  });

  it('should set totalCount signal from response total', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('should return "green" for Normal SLA urgency', () => {
    expect(component.slaColor('Normal')).toBe('green');
  });

  it('should return "yellow" for Warning SLA urgency', () => {
    expect(component.slaColor('Warning')).toBe('yellow');
  });

  it('should return "red" for Breached SLA urgency', () => {
    expect(component.slaColor('Breached')).toBe('red');
  });

  it('should navigate to /tickets/new when onNewTicket() is called', () => {
    component.onNewTicket();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/tickets', 'new']);
  });

  it('should navigate to ticket detail when onRowClick() is called', () => {
    component.onRowClick('t-1');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/tickets', 't-1']);
  });

  it('should reload tickets when search changes', fakeAsync(() => {
    ticketServiceSpy.list.calls.reset();
    component.onSearch('login');
    tick(300); // debounce
    expect(ticketServiceSpy.list).toHaveBeenCalledWith(jasmine.objectContaining({ search: 'login' }));
  }));

  it('should reload tickets when page changes', () => {
    ticketServiceSpy.list.calls.reset();
    component.onPageChange({ pageIndex: 1, pageSize: 20, length: 2 });
    expect(ticketServiceSpy.list).toHaveBeenCalledWith(jasmine.objectContaining({ page: 2, pageSize: 20 }));
  });

  it('should reload tickets when status filter changes', () => {
    ticketServiceSpy.list.calls.reset();
    component.onStatusFilterChange(['New', 'InProgress']);
    expect(ticketServiceSpy.list).toHaveBeenCalledWith(
      jasmine.objectContaining({ status: ['New', 'InProgress'] })
    );
  });

  it('should set loading signal to false after response', () => {
    expect(component.loading()).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket-list/ticket-list.component.spec.ts --watch=false
```

Expected: FAIL — `TicketListComponent` does not exist yet.

- [ ] **Step 3: Implement TicketListComponent**

```typescript
// src/app/tickets/ticket-list/ticket-list.component.ts
import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  TicketService,
  TicketSummary,
  TicketStatus,
  TicketPriority,
  SlaUrgency,
  TicketListParams,
} from '../ticket.service';

@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatChipsModule,
    MatBadgeModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatTooltipModule,
  ],
  template: `
    <div class="ticket-list-container">
      <div class="list-header">
        <h1>Tickets</h1>
        <button mat-flat-button color="primary" (click)="onNewTicket()">
          <mat-icon>add</mat-icon> New Ticket
        </button>
      </div>

      <!-- Search bar -->
      <mat-form-field appearance="outline" class="search-field">
        <mat-label>Search by ticket # or subject</mat-label>
        <mat-icon matPrefix>search</mat-icon>
        <input matInput [(ngModel)]="searchValue" (ngModelChange)="onSearch($event)" />
      </mat-form-field>

      <div class="list-body">
        <!-- Filter sidebar -->
        <aside class="filter-sidebar">
          <h3>Filters</h3>

          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select multiple (selectionChange)="onStatusFilterChange($event.value)">
              @for (s of statusOptions; track s) {
                <mat-option [value]="s">{{ s }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Priority</mat-label>
            <mat-select (selectionChange)="onPriorityFilterChange($event.value)">
              <mat-option value="">All</mat-option>
              @for (p of priorityOptions; track p) {
                <mat-option [value]="p">{{ p }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Date From</mat-label>
            <input matInput type="date" (change)="onDateFromChange($any($event.target).value)" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Date To</mat-label>
            <input matInput type="date" (change)="onDateToChange($any($event.target).value)" />
          </mat-form-field>
        </aside>

        <!-- Table -->
        <div class="table-wrapper">
          @if (loading()) {
            <div class="loading-overlay">
              <mat-spinner diameter="48" />
            </div>
          }

          <table mat-table [dataSource]="tickets()" class="mat-elevation-z1">
            <ng-container matColumnDef="ticketNumber">
              <th mat-header-cell *matHeaderCellDef>Ticket #</th>
              <td mat-cell *matCellDef="let row">{{ row.ticketNumber }}</td>
            </ng-container>

            <ng-container matColumnDef="subject">
              <th mat-header-cell *matHeaderCellDef>Subject</th>
              <td mat-cell *matCellDef="let row">{{ row.subject }}</td>
            </ng-container>

            <ng-container matColumnDef="customer">
              <th mat-header-cell *matHeaderCellDef>Customer</th>
              <td mat-cell *matCellDef="let row">{{ row.customerName }}</td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let row">
                <span class="status-badge status-{{ row.status.toLowerCase() }}">{{ row.status }}</span>
              </td>
            </ng-container>

            <ng-container matColumnDef="priority">
              <th mat-header-cell *matHeaderCellDef>Priority</th>
              <td mat-cell *matCellDef="let row">
                <span class="priority-badge priority-{{ row.priority.toLowerCase() }}">{{ row.priority }}</span>
              </td>
            </ng-container>

            <ng-container matColumnDef="sla">
              <th mat-header-cell *matHeaderCellDef>SLA</th>
              <td mat-cell *matCellDef="let row">
                <span
                  class="sla-dot"
                  [style.background-color]="slaColor(row.slaUrgency)"
                  [matTooltip]="row.slaUrgency"
                ></span>
              </td>
            </ng-container>

            <ng-container matColumnDef="assignedAgent">
              <th mat-header-cell *matHeaderCellDef>Agent</th>
              <td mat-cell *matCellDef="let row">{{ row.assignedAgentName ?? '—' }}</td>
            </ng-container>

            <ng-container matColumnDef="department">
              <th mat-header-cell *matHeaderCellDef>Department</th>
              <td mat-cell *matCellDef="let row">{{ row.departmentName ?? '—' }}</td>
            </ng-container>

            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef>Created</th>
              <td mat-cell *matCellDef="let row">{{ row.createdAt | date:'shortDate' }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr
              mat-row
              *matRowDef="let row; columns: displayedColumns"
              class="ticket-row"
              (click)="onRowClick(row.id)"
            ></tr>
          </table>

          <mat-paginator
            [length]="totalCount()"
            [pageSize]="pageSize"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onPageChange($event)"
          />
        </div>
      </div>
    </div>
  `,
})
export class TicketListComponent implements OnInit, OnDestroy {
  private readonly ticketService = inject(TicketService);
  protected readonly router = inject(Router);

  readonly tickets = signal<TicketSummary[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);

  readonly statusOptions: TicketStatus[] = ['New', 'Assigned', 'InProgress', 'OnHold', 'Escalated', 'Resolved', 'Closed'];
  readonly priorityOptions: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];

  readonly displayedColumns = ['ticketNumber', 'subject', 'customer', 'status', 'priority', 'sla', 'assignedAgent', 'department', 'createdAt'];

  pageSize = 20;
  private currentPage = 1;
  private currentParams: TicketListParams = { page: 1, pageSize: 20 };

  searchValue = '';
  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(search => {
      this.currentPage = 1;
      this.loadTickets({ ...this.currentParams, search, page: 1 });
    });

    this.loadTickets(this.currentParams);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadTickets(params: TicketListParams): void {
    this.currentParams = params;
    this.loading.set(true);
    this.ticketService.list(params).subscribe({
      next: res => {
        this.tickets.set(res.items);
        this.totalCount.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  slaColor(urgency: SlaUrgency): string {
    const map: Record<SlaUrgency, string> = { Normal: 'green', Warning: 'yellow', Breached: 'red' };
    return map[urgency] ?? 'grey';
  }

  onNewTicket(): void {
    this.router.navigate(['/tickets', 'new']);
  }

  onRowClick(id: string): void {
    this.router.navigate(['/tickets', id]);
  }

  onSearch(value: string): void {
    this.searchSubject.next(value);
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadTickets({ ...this.currentParams, page: this.currentPage, pageSize: this.pageSize });
  }

  onStatusFilterChange(statuses: TicketStatus[]): void {
    this.currentPage = 1;
    this.loadTickets({ ...this.currentParams, status: statuses, page: 1 });
  }

  onPriorityFilterChange(priority: TicketPriority | ''): void {
    this.currentPage = 1;
    this.loadTickets({ ...this.currentParams, priority: priority || undefined, page: 1 });
  }

  onDateFromChange(dateFrom: string): void {
    this.loadTickets({ ...this.currentParams, dateFrom, page: 1 });
  }

  onDateToChange(dateTo: string): void {
    this.loadTickets({ ...this.currentParams, dateTo, page: 1 });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket-list/ticket-list.component.spec.ts --watch=false
```

Expected: 13 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket-list/
git commit -m "feat(tickets): implement TicketListComponent with filters, search, SLA indicators, pagination"
```

---

## Task 3: Integration smoke test

- [ ] **Step 1: Run all ticket list tests**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --include=src/app/tickets/ticket-list/ticket-list.component.spec.ts --watch=false
```

Expected: All 17 tests PASS.

- [ ] **Step 2: Commit**

```bash
git commit -m "feat(tickets): US-FE-009 complete — ticket list page with sidebar filters and SLA"
```
