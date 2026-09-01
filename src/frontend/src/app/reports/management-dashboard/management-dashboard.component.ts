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
import { Department, DepartmentService } from '../../admin/departments/department.service';
import { SignalRService } from '../../shared/services/signalr.service';
import { AuthStore } from '../../auth/auth.store';
import * as signalR from '@microsoft/signalr';

interface KpiCard { label: string; key: keyof KpiData; suffix?: string; }

const KPI_CARDS: KpiCard[] = [
  { label: 'Open Tickets', key: 'openTickets' },
  { label: 'SLA Breach Rate', key: 'slaBreachRate', suffix: '%' },
  { label: 'Avg First Response (min)', key: 'avgFirstResponseMinutes7Day' },
  { label: 'CSAT Score', key: 'csatScore30Day', suffix: '%' },
  { label: 'Agent Utilization', key: 'agentUtilization', suffix: '%' },
  { label: 'Unassigned', key: 'unassignedTickets' },
  { label: 'Escalation Rate', key: 'escalationRate', suffix: '%' },
  { label: 'Resolved Today', key: 'ticketsTodayResolved' },
];

@Component({
  selector: 'app-management-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
  ],
  templateUrl: './management-dashboard.component.html',
})
export class ManagementDashboardComponent implements OnInit, OnDestroy {
  private readonly dashboardService = inject(DashboardService);
  private readonly departmentService = inject(DepartmentService);
  private readonly signalRService = inject(SignalRService);
  readonly authStore = inject(AuthStore);

  readonly kpis = signal<KpiData | null>(null);
  readonly agentWorkload = signal<AgentWorkload[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly departmentFilter = new FormControl('');

  readonly kpiCards = KPI_CARDS;
  private connection!: signalR.HubConnection;

  workloadColumns = ['agentName', 'openTickets', 'availabilityStatus'];

  get isAdmin(): boolean { return this.authStore.user()?.role === 'Admin'; }

  ngOnInit(): void {
    if (this.isAdmin) {
      this.departmentService.list().subscribe(r => this.departments.set(r.data));
    }
    this.loadKpis();
    this.connectSignalR();
    this.departmentFilter.valueChanges.subscribe(() => this.loadKpis());
  }

  ngOnDestroy(): void {
    this.connection?.stop();
  }

  loadKpis(): void {
    const deptId = this.departmentFilter.value || undefined;
    this.dashboardService.getKpis(deptId).subscribe(k => {
      this.kpis.set(k);
      this.agentWorkload.set(k.agentWorkload ?? []);
    });
  }

  refresh(): void { this.loadKpis(); }

  private connectSignalR(): void {
    this.connection = this.signalRService.getConnection('/hubs/dashboard');
    this.connection.start().then(() => {
      this.connection.on('KpiUpdated', (data: KpiData) => {
        this.kpis.set(data);
        this.agentWorkload.set(data.agentWorkload ?? []);
      });
    });
  }
}
