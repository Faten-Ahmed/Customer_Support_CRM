# Management Dashboard (Live KPIs) — Implementation Plan

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

**Story:** US-FE-030
**Goal:** Implement `/reports/dashboard` — live KPI cards, agent workload table, and today's summary bar, all updated in real time via SignalR `KpiUpdated` and `AgentWorkloadUpdated` events.

**Architecture:** `ManagementDashboardComponent` is standalone, lazy-loaded. On init it calls `DashboardService.getKpis()` and then subscribes to the `DashboardHub` SignalR connection. `KpiUpdated` events update the KPI signal; `AgentWorkloadUpdated` events update the agent table signal. Department filter (Admin only) re-fetches KPIs with a query param. A "Refresh" button calls `loadKpis()` imperatively.

**Tech Stack:** Angular 21, TypeScript, Angular Material, `@microsoft/signalr`, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/reports/dashboard.service.ts` |
| Create | `src/app/reports/dashboard.service.spec.ts` |
| Create | `src/app/reports/management-dashboard/management-dashboard.component.ts` |
| Create | `src/app/reports/management-dashboard/management-dashboard.component.html` |
| Create | `src/app/reports/management-dashboard/management-dashboard.component.spec.ts` |

---

## Task 1: DashboardService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/reports/dashboard.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [DashboardService],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getKpis() should GET /api/v1/reports/dashboard', () => {
    service.getKpis().subscribe(data => {
      expect(data.openTickets).toBeDefined();
    });
    const req = httpMock.expectOne('/api/v1/reports/dashboard');
    expect(req.request.method).toBe('GET');
    req.flush({ openTickets: 12, slaBreachRate: 5, avgFirstResponse: 30, escalationRate: 2 });
  });

  it('getKpis() should include departmentId param when provided', () => {
    service.getKpis('d1').subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/dashboard');
    expect(req.request.params.get('departmentId')).toBe('d1');
    req.flush({});
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/reports/dashboard.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/reports/dashboard.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface KpiData {
  openTickets: number;
  slaBreachRate: number;
  avgFirstResponse: number;
  avgResolution?: number;
  csatScore?: number;
  agentUtilization?: number;
  unassignedTickets?: number;
  escalationRate?: number;
  createdToday?: number;
  resolvedToday?: number;
}

export interface AgentWorkload {
  agentId: string;
  agentName: string;
  openTickets: number;
  availabilityStatus: string;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getKpis(departmentId?: string): Observable<KpiData> {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<KpiData>('/api/v1/reports/dashboard', { params });
  }

  getAgentWorkload(departmentId?: string): Observable<AgentWorkload[]> {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<AgentWorkload[]>('/api/v1/reports/dashboard/agents', { params });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/reports/dashboard.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/reports/dashboard.service.ts src/app/reports/dashboard.service.spec.ts
git commit -m "feat(reports): add DashboardService (US-FE-030)"
```

---

## Task 2: ManagementDashboardComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/reports/management-dashboard/management-dashboard.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ManagementDashboardComponent } from './management-dashboard.component';
import { DashboardService, KpiData, AgentWorkload } from '../dashboard.service';
import { SignalRService } from '../../shared/services/signalr.service';
import { AuthStore } from '../../auth/auth.store';

const mockKpi: KpiData = {
  openTickets: 12, slaBreachRate: 5, avgFirstResponse: 30, escalationRate: 2,
  unassignedTickets: 4, csatScore: 87, agentUtilization: 72, createdToday: 8, resolvedToday: 6,
};

const mockWorkload: AgentWorkload[] = [
  { agentId: 'a1', agentName: 'Omar', openTickets: 5, availabilityStatus: 'Available' },
];

describe('ManagementDashboardComponent', () => {
  let fixture: ComponentFixture<ManagementDashboardComponent>;
  let component: ManagementDashboardComponent;
  let dashboardService: jasmine.SpyObj<DashboardService>;
  let signalRService: jasmine.SpyObj<SignalRService>;
  let mockConnection: jasmine.SpyObj<any>;

  beforeEach(async () => {
    dashboardService = jasmine.createSpyObj('DashboardService', ['getKpis', 'getAgentWorkload']);
    dashboardService.getKpis.and.returnValue(of(mockKpi));
    dashboardService.getAgentWorkload.and.returnValue(of(mockWorkload));

    mockConnection = jasmine.createSpyObj('HubConnection', ['start', 'on', 'stop']);
    mockConnection.start.and.returnValue(Promise.resolve());
    signalRService = jasmine.createSpyObj('SignalRService', ['getConnection']);
    signalRService.getConnection.and.returnValue(mockConnection);

    await TestBed.configureTestingModule({
      imports: [ManagementDashboardComponent, NoopAnimationsModule],
      providers: [
        { provide: DashboardService, useValue: dashboardService },
        { provide: SignalRService, useValue: signalRService },
        { provide: AuthStore, useValue: { user: () => ({ role: 'Admin' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ManagementDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => component.ngOnDestroy());

  it('should create and display KPI cards', () => {
    expect(component).toBeTruthy();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('12');
    expect(el.textContent).toContain('Open Tickets');
  });

  it('should connect to DashboardHub via SignalR', () => {
    expect(signalRService.getConnection).toHaveBeenCalledWith('/hubs/dashboard');
    expect(mockConnection.start).toHaveBeenCalled();
  });

  it('should show department filter for Admin role', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Department');
  });

  it('should reload KPIs on refresh button click', () => {
    component.refresh();
    expect(dashboardService.getKpis).toHaveBeenCalledTimes(2);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/reports/management-dashboard/management-dashboard.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/reports/management-dashboard/management-dashboard.component.ts

import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AgentWorkload, DashboardService, KpiData } from '../dashboard.service';
import { SignalRService } from '../../shared/services/signalr.service';
import { AuthStore } from '../../auth/auth.store';
import * as signalR from '@microsoft/signalr';

interface KpiCard { label: string; key: keyof KpiData; color: string; suffix?: string; }

const KPI_CARDS: KpiCard[] = [
  { label: 'Open Tickets', key: 'openTickets', color: 'text-blue-600' },
  { label: 'SLA Breach Rate', key: 'slaBreachRate', color: 'text-red-600', suffix: '%' },
  { label: 'Avg First Response (min)', key: 'avgFirstResponse', color: 'text-yellow-600' },
  { label: 'CSAT Score', key: 'csatScore', color: 'text-green-600', suffix: '%' },
  { label: 'Agent Utilization', key: 'agentUtilization', color: 'text-purple-600', suffix: '%' },
  { label: 'Unassigned', key: 'unassignedTickets', color: 'text-orange-600' },
  { label: 'Escalation Rate', key: 'escalationRate', color: 'text-red-400', suffix: '%' },
  { label: 'Resolved Today', key: 'resolvedToday', color: 'text-teal-600' },
];

@Component({
  selector: 'app-management-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatSelectModule, MatFormFieldModule],
  templateUrl: './management-dashboard.component.html',
})
export class ManagementDashboardComponent implements OnInit, OnDestroy {
  private readonly dashboardService = inject(DashboardService);
  private readonly signalRService = inject(SignalRService);
  readonly authStore = inject(AuthStore);

  readonly kpis = signal<KpiData | null>(null);
  readonly agentWorkload = signal<AgentWorkload[]>([]);
  readonly departmentFilter = new FormControl('');

  readonly kpiCards = KPI_CARDS;
  private connection!: signalR.HubConnection;

  workloadColumns = ['agentName', 'openTickets', 'availabilityStatus'];

  get isAdmin(): boolean { return this.authStore.user()?.role === 'Admin'; }

  ngOnInit(): void {
    this.loadKpis();
    this.connectSignalR();
    this.departmentFilter.valueChanges.subscribe(() => this.loadKpis());
  }

  ngOnDestroy(): void {
    this.connection?.stop();
  }

  loadKpis(): void {
    const deptId = this.departmentFilter.value || undefined;
    this.dashboardService.getKpis(deptId).subscribe(k => this.kpis.set(k));
    this.dashboardService.getAgentWorkload(deptId).subscribe(w => this.agentWorkload.set(w));
  }

  refresh(): void { this.loadKpis(); }

  private connectSignalR(): void {
    this.connection = this.signalRService.getConnection('/hubs/dashboard');
    this.connection.start().then(() => {
      this.connection.on('KpiUpdated', (data: KpiData) => this.kpis.set(data));
      this.connection.on('AgentWorkloadUpdated', (data: AgentWorkload[]) => this.agentWorkload.set(data));
    });
  }
}
```

```html
<!-- src/app/reports/management-dashboard/management-dashboard.component.html -->

<div class="p-6">
  <div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-semibold">Operations Dashboard</h1>
    <div class="flex gap-3 items-center">
      @if (isAdmin) {
        <mat-form-field appearance="outline" class="w-40">
          <mat-label>Department</mat-label>
          <mat-select [formControl]="departmentFilter">
            <mat-option value="">All</mat-option>
            <mat-option value="d1">Support</mat-option>
          </mat-select>
        </mat-form-field>
      }
      <button mat-stroked-button (click)="refresh()"><mat-icon>refresh</mat-icon> Refresh</button>
    </div>
  </div>

  <!-- KPI Cards -->
  @if (kpis()) {
    <div class="grid grid-cols-4 gap-4 mb-8">
      @for (card of kpiCards; track card.key) {
        <mat-card class="p-4 text-center">
          <p class="text-3xl font-bold" [class]="card.color">
            {{ kpis()![card.key] ?? '—' }}{{ card.suffix ?? '' }}
          </p>
          <p class="text-sm text-gray-500 mt-1">{{ card.label }}</p>
        </mat-card>
      }
    </div>
  }

  <!-- Agent Workload -->
  <h2 class="text-lg font-semibold mb-3">Agent Workload</h2>
  <mat-table [dataSource]="agentWorkload()" class="w-full">
    <ng-container matColumnDef="agentName">
      <mat-header-cell *matHeaderCellDef>Agent</mat-header-cell>
      <mat-cell *matCellDef="let a">{{ a.agentName }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="openTickets">
      <mat-header-cell *matHeaderCellDef>Open Tickets</mat-header-cell>
      <mat-cell *matCellDef="let a" [class.text-red-600]="a.openTickets > 10">{{ a.openTickets }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="availabilityStatus">
      <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
      <mat-cell *matCellDef="let a">{{ a.availabilityStatus }}</mat-cell>
    </ng-container>
    <mat-header-row *matHeaderRowDef="workloadColumns"></mat-header-row>
    <mat-row *matRowDef="let row; columns: workloadColumns;"></mat-row>
  </mat-table>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/reports/management-dashboard/management-dashboard.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/reports/management-dashboard/
git commit -m "feat(reports): implement ManagementDashboardComponent with SignalR KPI updates (US-FE-030)"
```
