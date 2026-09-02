import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AgentService, MyTicketDto, AvailabilityStatus } from '../agent.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatToolbarModule,
    MatTooltipModule,
    FormsModule,
    DatePipe,
    TranslatePipe,
  ],
  template: `
    <div class="dashboard-page">

      <!-- Toolbar with availability -->
      <mat-toolbar color="primary" class="dashboard-toolbar">
        <span class="toolbar-title">{{ 'nav.dashboard' | translate }}</span>
        <span class="spacer"></span>
        <mat-form-field appearance="outline" class="availability-field" subscriptSizing="dynamic">
          <mat-label>{{ 'dashboard.availability' | translate }}</mat-label>
          <mat-select [(ngModel)]="selectedAvailability" (ngModelChange)="onAvailabilityChange($event)">
            @for (opt of availabilityOptions; track opt.value) {
              <mat-option [value]="opt.value">{{ opt.labelKey | translate }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <button mat-icon-button (click)="loadTickets()" [matTooltip]="'dashboard.refresh' | translate">
          <mat-icon>refresh</mat-icon>
        </button>
      </mat-toolbar>

      <!-- Stat cards -->
      <div class="stat-cards">
        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-value">{{ openCount() }}</div>
            <div class="stat-label">{{ 'dashboard.openTickets' | translate }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card stat-card--warn">
          <mat-card-content>
            <div class="stat-value">{{ slaWarnCount() }}</div>
            <div class="stat-label">{{ 'dashboard.slaWarningBreached' | translate }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-content>
            <div class="stat-value">{{ onHoldCount() }}</div>
            <div class="stat-label">{{ 'dashboard.onHold' | translate }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card stat-card--success">
          <mat-card-content>
            <div class="stat-value">{{ resolvedTodayCount() }}</div>
            <div class="stat-label">{{ 'dashboard.resolvedToday' | translate }}</div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- My Tickets table -->
      <mat-card class="tickets-card">
        <mat-card-header>
          <mat-card-title>{{ 'dashboard.myTickets' | translate }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (loading()) {
            <div class="loading-container">
              <mat-spinner diameter="40"></mat-spinner>
            </div>
          } @else if (tickets().length === 0) {
            <p class="empty-message">{{ 'dashboard.noTickets' | translate }}</p>
          } @else {
            <table mat-table [dataSource]="tickets()" class="tickets-table">

              <ng-container matColumnDef="ticketNumber">
                <th mat-header-cell *matHeaderCellDef>{{ 'ticket.ticketNum' | translate }}</th>
                <td mat-cell *matCellDef="let row">{{ row.ticketNumber }}</td>
              </ng-container>

              <ng-container matColumnDef="subject">
                <th mat-header-cell *matHeaderCellDef>{{ 'common.subject' | translate }}</th>
                <td mat-cell *matCellDef="let row">{{ row.subject }}</td>
              </ng-container>

              <ng-container matColumnDef="priority">
                <th mat-header-cell *matHeaderCellDef>{{ 'common.priority' | translate }}</th>
                <td mat-cell *matCellDef="let row">
                  <span [class]="'priority-badge priority-' + row.priority.toLowerCase()">
                    {{ row.priority }}
                  </span>
                </td>
              </ng-container>

              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>{{ 'common.status' | translate }}</th>
                <td mat-cell *matCellDef="let row">{{ row.status }}</td>
              </ng-container>

              <ng-container matColumnDef="slaStatus">
                <th mat-header-cell *matHeaderCellDef>{{ 'ticket.sla' | translate }}</th>
                <td mat-cell *matCellDef="let row">
                  <mat-chip [style.background-color]="getSlaColor(row.slaStatus)"
                            style="color: white; font-size: 12px;">
                    {{ row.slaStatus }}
                  </mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="resolutionDue">
                <th mat-header-cell *matHeaderCellDef>{{ 'dashboard.resolutionDue' | translate }}</th>
                <td mat-cell *matCellDef="let row">
                  {{ row.resolutionDue ? (row.resolutionDue | date:'medium') : '—' }}
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;"
                  class="clickable-row"
                  (click)="navigateToTicket(row.id)">
              </tr>
            </table>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .dashboard-page {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .dashboard-toolbar {
      flex-shrink: 0;
    }

    .toolbar-title {
      font-size: 1.2rem;
      font-weight: 500;
    }

    .spacer {
      flex: 1;
    }

    .availability-field {
      width: 180px;
      margin-right: 8px;
    }

    .stat-cards {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      padding: 16px;
    }

    .stat-card {
      text-align: center;
    }

    .stat-card--warn {
      border-left: 4px solid #f44336;
    }

    .stat-card--success {
      border-left: 4px solid #4caf50;
    }

    .stat-value {
      font-size: 2.5rem;
      font-weight: 700;
      line-height: 1;
      margin-bottom: 4px;
    }

    .stat-label {
      font-size: 0.85rem;
      color: rgba(0,0,0,0.6);
    }

    .tickets-card {
      margin: 0 16px 16px;
      flex: 1;
    }

    .tickets-table {
      width: 100%;
    }

    .clickable-row {
      cursor: pointer;
    }

    .clickable-row:hover {
      background-color: rgba(0, 0, 0, 0.04);
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 32px;
    }

    .empty-message {
      text-align: center;
      padding: 32px;
      color: rgba(0,0,0,0.5);
    }

    .priority-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
    }

    .priority-low { background: #e3f2fd; color: #1565c0; }
    .priority-medium { background: #fff3e0; color: #e65100; }
    .priority-high { background: #fce4ec; color: #b71c1c; }
    .priority-critical { background: #f44336; color: white; }
  `],
})
export class AgentDashboardComponent implements OnInit, OnDestroy {
  private readonly agentService = inject(AgentService);
  private readonly router = inject(Router);

  readonly tickets = signal<MyTicketDto[]>([]);
  readonly loading = signal(false);
  selectedAvailability: AvailabilityStatus = 'Available';

  readonly displayedColumns = ['ticketNumber', 'subject', 'priority', 'status', 'slaStatus', 'resolutionDue'];

  readonly availabilityOptions: { value: AvailabilityStatus; labelKey: string }[] = [
    { value: 'Available', labelKey: 'avail.available' },
    { value: 'Busy', labelKey: 'avail.busy' },
    { value: 'Away', labelKey: 'avail.away' },
    { value: 'Offline', labelKey: 'avail.offline' },
  ];

  // Computed stats
  readonly openCount = computed(() =>
    this.tickets().filter(t => ['New', 'Assigned', 'InProgress', 'Escalated', 'Reopened'].includes(t.status)).length
  );

  readonly slaWarnCount = computed(() =>
    this.tickets().filter(t => t.slaStatus === 'warning' || t.slaStatus === 'breach' || t.slaStatus === 'criticalBreach').length
  );

  readonly onHoldCount = computed(() =>
    this.tickets().filter(t => t.status === 'OnHold').length
  );

  readonly resolvedTodayCount = computed(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.tickets().filter(t => {
      if (t.status !== 'Resolved') return false;
      // We don't have resolvedAt on MyTicketDto — count resolved tickets as proxy
      return true;
    }).length;
  });

  private intervalId: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.loadTickets();
    this.intervalId = setInterval(() => this.loadTickets(), 60_000);
  }

  ngOnDestroy(): void {
    if (this.intervalId !== null) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  loadTickets(): void {
    this.loading.set(true);
    this.agentService.getMyTickets({ pageSize: 200 }).subscribe({
      next: page => {
        this.tickets.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onAvailabilityChange(status: AvailabilityStatus): void {
    this.agentService.updateAvailability(status).subscribe();
  }

  navigateToTicket(id: string): void {
    this.router.navigate(['/app/tickets', id]);
  }

  getSlaColor(slaStatus: string): string {
    switch (slaStatus) {
      case 'ok': return '#4caf50';
      case 'warning': return '#ff9800';
      case 'breach':
      case 'criticalBreach': return '#f44336';
      default: return '#9e9e9e';
    }
  }
}
