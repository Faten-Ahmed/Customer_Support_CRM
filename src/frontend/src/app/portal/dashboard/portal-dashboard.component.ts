import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

interface CustomerTicket {
  id: string;
  ticketNumber: string;
  subject: string;
  status: string;
  priority: string;
  createdAt: string;
  category: string | null;
}

interface TicketPage {
  items: CustomerTicket[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatButtonModule, MatIconModule, MatCardModule,
    MatTableModule, MatChipsModule, MatProgressSpinnerModule,
  ],
  template: `
    <div class="dashboard-header">
      <h1>My Support Tickets</h1>
      <a mat-flat-button color="primary" routerLink="/portal/tickets/new">
        <mat-icon>add</mat-icon> Submit New Ticket
      </a>
    </div>

    @if (loading()) {
      <div class="center"><mat-spinner diameter="40" /></div>
    } @else if (tickets().length === 0) {
      <mat-card class="empty-card">
        <mat-card-content>
          <mat-icon class="empty-icon">confirmation_number</mat-icon>
          <p>You have no tickets yet. Click <strong>Submit New Ticket</strong> to get started.</p>
        </mat-card-content>
      </mat-card>
    } @else {
      <mat-card>
        <table mat-table [dataSource]="tickets()" class="full-width">
          <ng-container matColumnDef="ticketNumber">
            <th mat-header-cell *matHeaderCellDef>Ticket #</th>
            <td mat-cell *matCellDef="let t">{{ t.ticketNumber }}</td>
          </ng-container>

          <ng-container matColumnDef="subject">
            <th mat-header-cell *matHeaderCellDef>Subject</th>
            <td mat-cell *matCellDef="let t">{{ t.subject }}</td>
          </ng-container>

          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let t">
              <mat-chip [class]="'status-' + t.status.toLowerCase()">{{ t.status }}</mat-chip>
            </td>
          </ng-container>

          <ng-container matColumnDef="priority">
            <th mat-header-cell *matHeaderCellDef>Priority</th>
            <td mat-cell *matCellDef="let t">{{ t.priority }}</td>
          </ng-container>

          <ng-container matColumnDef="createdAt">
            <th mat-header-cell *matHeaderCellDef>Submitted</th>
            <td mat-cell *matCellDef="let t">{{ t.createdAt | date:'mediumDate' }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"
              class="clickable-row"
              [routerLink]="['/portal/tickets', row.id]"></tr>
        </table>
      </mat-card>
    }
  `,
  styles: [`
    .dashboard-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 24px;
    }
    h1 { margin: 0; font-size: 22px; }
    .empty-card { text-align: center; padding: 40px; }
    .empty-icon { font-size: 48px; width: 48px; height: 48px; color: #bbb; display: block; margin: 0 auto 16px; }
    .center { display: flex; justify-content: center; padding: 48px; }
    .full-width { width: 100%; }
    .clickable-row { cursor: pointer; }
    .clickable-row:hover { background: #f5f5f5; }
    .status-new { background: #e3f2fd; color: #1565c0; }
    .status-assigned { background: #fff3e0; color: #e65100; }
    .status-inprogress { background: #f3e5f5; color: #6a1b9a; }
    .status-reopened { background: #fce4ec; color: #880e4f; }
    .status-resolved { background: #e8f5e9; color: #2e7d32; }
    .status-closed { background: #f5f5f5; color: #616161; }
  `],
})
export class PortalDashboardComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly loading = signal(true);
  readonly tickets = signal<CustomerTicket[]>([]);
  readonly columns = ['ticketNumber', 'subject', 'status', 'priority', 'createdAt'];

  ngOnInit(): void {
    this.http.get<TicketPage>('/api/v1/portal/tickets').subscribe({
      next: page => {
        this.tickets.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
