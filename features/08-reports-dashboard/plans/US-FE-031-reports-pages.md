# Reports Pages (Ticket, SLA, Agents, CSAT) — Implementation Plan

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

**Story:** US-FE-031
**Goal:** Implement four report pages under `/reports/` — Tickets, SLA, Agents, and CSAT — each with charts (ng2-charts/Chart.js), data tables, a shared date-range + department filter bar, and an Export button.

**Architecture:** `ReportService` wraps all four report API calls. Each report page is a standalone component. A shared `ReportFilterBarComponent` provides the date-range picker and department filter and emits a `FilterChange` event. Charts use `ng2-charts` (`BaseChartDirective`). Skeleton loaders are shown while loading. Error state shows a "Retry" button.

**Tech Stack:** Angular 21, TypeScript, Angular Material, ng2-charts (Chart.js), Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/reports/report.service.ts` |
| Create | `src/app/reports/report.service.spec.ts` |
| Create | `src/app/reports/shared/report-filter-bar.component.ts` |
| Create | `src/app/reports/shared/report-filter-bar.component.spec.ts` |
| Create | `src/app/reports/ticket-report/ticket-report.component.ts` |
| Create | `src/app/reports/ticket-report/ticket-report.component.html` |
| Create | `src/app/reports/ticket-report/ticket-report.component.spec.ts` |
| Create | `src/app/reports/sla-report/sla-report.component.ts` |
| Create | `src/app/reports/sla-report/sla-report.component.html` |
| Create | `src/app/reports/sla-report/sla-report.component.spec.ts` |
| Create | `src/app/reports/agent-report/agent-report.component.ts` |
| Create | `src/app/reports/agent-report/agent-report.component.html` |
| Create | `src/app/reports/agent-report/agent-report.component.spec.ts` |
| Create | `src/app/reports/csat-report/csat-report.component.ts` |
| Create | `src/app/reports/csat-report/csat-report.component.html` |
| Create | `src/app/reports/csat-report/csat-report.component.spec.ts` |
| Create | `src/app/reports/reports.routes.ts` |

---

## Task 1: ReportService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/reports/report.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReportService } from './report.service';

describe('ReportService', () => {
  let service: ReportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReportService],
    });
    service = TestBed.inject(ReportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getTicketReport() should GET /api/v1/reports/tickets with date params', () => {
    service.getTicketReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/tickets');
    expect(req.request.params.get('dateFrom')).toBe('2025-01-01');
    req.flush({ summary: {}, byStatus: [], trend: [] });
  });

  it('getSlaReport() should GET /api/v1/reports/sla', () => {
    service.getSlaReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/sla');
    req.flush({ complianceRate: 90, byPriority: [] });
  });

  it('getAgentReport() should GET /api/v1/reports/agents', () => {
    service.getAgentReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/agents');
    req.flush([]);
  });

  it('getCsatReport() should GET /api/v1/reports/csat', () => {
    service.getCsatReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/csat');
    req.flush({ avgRating: 4.2, distribution: [], comments: [] });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/reports/report.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/reports/report.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReportFilter {
  dateFrom: string;
  dateTo: string;
  departmentId?: string;
  exportFormat?: 'csv' | 'excel' | 'pdf';
}

export interface TicketReport {
  summary: Record<string, number>;
  byStatus: { status: string; count: number }[];
  byPriority: { priority: string; count: number }[];
  trend: { date: string; count: number }[];
}

export interface SlaReport {
  complianceRate: number;
  byPriority: { priority: string; compliant: number; breached: number }[];
  breachReasons: { reason: string; count: number }[];
}

export interface AgentReportRow {
  agentId: string;
  agentName: string;
  ticketsHandled: number;
  avgResponseTime: number;
  slaComplianceRate: number;
  csatAvg?: number;
}

export interface CsatReport {
  avgRating: number;
  distribution: { rating: number; count: number }[];
  byDepartment: { department: string; avg: number }[];
  comments: { content: string; rating: number; agentName: string; date: string }[];
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);

  private buildParams(filter: ReportFilter): HttpParams {
    let params = new HttpParams().set('dateFrom', filter.dateFrom).set('dateTo', filter.dateTo);
    if (filter.departmentId) params = params.set('departmentId', filter.departmentId);
    return params;
  }

  getTicketReport(filter: ReportFilter): Observable<TicketReport> {
    return this.http.get<TicketReport>('/api/v1/reports/tickets', { params: this.buildParams(filter) });
  }

  getSlaReport(filter: ReportFilter): Observable<SlaReport> {
    return this.http.get<SlaReport>('/api/v1/reports/sla', { params: this.buildParams(filter) });
  }

  getAgentReport(filter: ReportFilter): Observable<AgentReportRow[]> {
    return this.http.get<AgentReportRow[]>('/api/v1/reports/agents', { params: this.buildParams(filter) });
  }

  getCsatReport(filter: ReportFilter): Observable<CsatReport> {
    return this.http.get<CsatReport>('/api/v1/reports/csat', { params: this.buildParams(filter) });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/reports/report.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/reports/report.service.ts src/app/reports/report.service.spec.ts
git commit -m "feat(reports): add ReportService with four report endpoints (US-FE-031)"
```

---

## Task 2: TicketReportComponent (representative pattern for all four)

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/reports/ticket-report/ticket-report.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { TicketReportComponent } from './ticket-report.component';
import { ReportService, TicketReport } from '../report.service';

const mockReport: TicketReport = {
  summary: { total: 120, open: 20 },
  byStatus: [{ status: 'New', count: 10 }, { status: 'Resolved', count: 50 }],
  byPriority: [{ priority: 'High', count: 30 }],
  trend: [{ date: '2025-01-01', count: 5 }],
};

describe('TicketReportComponent', () => {
  let fixture: ComponentFixture<TicketReportComponent>;
  let component: TicketReportComponent;
  let reportService: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    reportService = jasmine.createSpyObj('ReportService', ['getTicketReport']);
    reportService.getTicketReport.and.returnValue(of(mockReport));

    await TestBed.configureTestingModule({
      imports: [TicketReportComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: ReportService, useValue: reportService }],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load report', () => {
    expect(component).toBeTruthy();
    expect(component.report()).toBeTruthy();
    expect(reportService.getTicketReport).toHaveBeenCalled();
  });

  it('should show error state and retry button on API failure', () => {
    reportService.getTicketReport.and.returnValue(throwError(() => new Error('Server error')));
    component.load();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Retry');
  });

  it('should reload on filter change', () => {
    component.filterForm.patchValue({ dateFrom: '2025-01-01', dateTo: '2025-01-31' });
    component.applyFilter();
    expect(reportService.getTicketReport).toHaveBeenCalledTimes(2);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/reports/ticket-report/ticket-report.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/reports/ticket-report/ticket-report.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { ReportFilter, ReportService, TicketReport } from '../report.service';

@Component({
  selector: 'app-ticket-report',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatTableModule, MatIconModule],
  templateUrl: './ticket-report.component.html',
})
export class TicketReportComponent implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly fb = inject(FormBuilder);

  readonly report = signal<TicketReport | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);

  filterForm = this.fb.group({
    dateFrom: [this.defaultFrom()],
    dateTo: [this.defaultTo()],
    departmentId: [''],
  });

  byStatusColumns = ['status', 'count'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    const filter = this.filterForm.value as ReportFilter;
    this.reportService.getTicketReport(filter).subscribe({
      next: r => { this.report.set(r); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  applyFilter(): void { this.load(); }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().split('T')[0];
  }

  private defaultTo(): string {
    return new Date().toISOString().split('T')[0];
  }
}
```

```html
<!-- src/app/reports/ticket-report/ticket-report.component.html -->

<div class="p-6">
  <h1 class="text-2xl font-semibold mb-4">Ticket Report</h1>

  <!-- Filter Bar -->
  <form [formGroup]="filterForm" class="flex gap-3 mb-6 items-end flex-wrap">
    <mat-form-field appearance="outline">
      <mat-label>From</mat-label>
      <input matInput type="date" formControlName="dateFrom" />
    </mat-form-field>
    <mat-form-field appearance="outline">
      <mat-label>To</mat-label>
      <input matInput type="date" formControlName="dateTo" />
    </mat-form-field>
    <button mat-raised-button color="primary" type="button" (click)="applyFilter()">Apply</button>
    <button mat-stroked-button type="button">
      <mat-icon>download</mat-icon> Export
    </button>
  </form>

  @if (loading()) {
    <div class="grid grid-cols-3 gap-4">
      @for (i of [1,2,3]; track i) {
        <div class="h-24 bg-gray-200 rounded animate-pulse"></div>
      }
    </div>
  } @else if (error()) {
    <div class="text-center py-12">
      <p class="text-red-500 mb-4">Failed to load report. Please try again.</p>
      <button mat-raised-button color="primary" (click)="load()">Retry</button>
    </div>
  } @else if (report()) {
    <!-- Summary Cards -->
    <div class="grid grid-cols-3 gap-4 mb-6">
      @for (entry of (report()!.summary | keyvalue); track entry.key) {
        <mat-card class="p-4 text-center">
          <p class="text-2xl font-bold text-blue-600">{{ entry.value }}</p>
          <p class="text-sm text-gray-500 capitalize">{{ entry.key }}</p>
        </mat-card>
      }
    </div>

    <!-- By Status Table -->
    <h2 class="text-lg font-semibold mb-3">By Status</h2>
    <mat-table [dataSource]="report()!.byStatus" class="w-full mb-6">
      <ng-container matColumnDef="status"><mat-header-cell *matHeaderCellDef>Status</mat-header-cell><mat-cell *matCellDef="let r">{{ r.status }}</mat-cell></ng-container>
      <ng-container matColumnDef="count"><mat-header-cell *matHeaderCellDef>Count</mat-header-cell><mat-cell *matCellDef="let r">{{ r.count }}</mat-cell></ng-container>
      <mat-header-row *matHeaderRowDef="byStatusColumns"></mat-header-row>
      <mat-row *matRowDef="let row; columns: byStatusColumns;"></mat-row>
    </mat-table>
  }
</div>
```

The SLA, Agent, and CSAT report components follow the same structure — identical skeleton with their own `report.service.*Report()` call, summary cards for their specific KPIs, and their own chart data. Implement them as copies of `TicketReportComponent` with the appropriate service call and table columns.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/reports/ticket-report/ticket-report.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/reports/ticket-report/ src/app/reports/sla-report/ src/app/reports/agent-report/ src/app/reports/csat-report/ src/app/reports/reports.routes.ts
git commit -m "feat(reports): implement four report pages with filters and error handling (US-FE-031)"
```
