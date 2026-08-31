import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { PortalTicketService, PortalTicketPage } from '../services/portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatFormFieldModule, MatProgressSpinnerModule,
    MatChipsModule, MatTableModule,
  ],
  template: `
    <div class="list-wrap">
      <div class="list-header">
        <h1>My Tickets</h1>
        <a mat-flat-button color="primary" routerLink="/portal/tickets/new">
          <mat-icon>add</mat-icon> Submit New Ticket
        </a>
      </div>

      <mat-form-field appearance="outline" class="filter-field">
        <mat-label>Filter by status</mat-label>
        <mat-select [formControl]="statusFilter">
          <mat-option value="">All</mat-option>
          <mat-option value="New">New</mat-option>
          <mat-option value="Assigned">Assigned</mat-option>
          <mat-option value="InProgress">In Progress</mat-option>
          <mat-option value="OnHold">On Hold</mat-option>
          <mat-option value="Resolved">Resolved</mat-option>
          <mat-option value="Reopened">Reopened</mat-option>
          <mat-option value="Closed">Closed</mat-option>
        </mat-select>
      </mat-form-field>

      @if (loading()) {
        <div class="center"><mat-spinner diameter="48" /></div>
      } @else if (tickets().length === 0) {
        <mat-card class="empty-card">
          <mat-card-content>
            <mat-icon class="empty-icon">confirmation_number</mat-icon>
            <p>No tickets found. Click <strong>Submit New Ticket</strong> to get started.</p>
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
                <mat-chip [class]="'chip-' + t.status.toLowerCase()">{{ t.status }}</mat-chip>
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
    </div>
  `,
  styles: [`
    .list-wrap { max-width: 900px; }
    .list-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 20px;
    }
    h1 { margin: 0; font-size: 22px; font-weight: 600; }
    .filter-field { width: 200px; margin-bottom: 16px; display: block; }
    .center { display: flex; justify-content: center; padding: 48px; }

    .empty-card { text-align: center; padding: 40px; }
    .empty-icon {
      font-size: 48px; width: 48px; height: 48px;
      color: #bbb; display: block; margin: 0 auto 16px;
    }

    .full-width { width: 100%; }
    .clickable-row { cursor: pointer; }
    .clickable-row:hover { background: #f5f5f5; }

    .chip-new        { background: #e3f2fd !important; color: #1565c0 !important; }
    .chip-assigned   { background: #fff3e0 !important; color: #e65100 !important; }
    .chip-inprogress { background: #f3e5f5 !important; color: #6a1b9a !important; }
    .chip-onhold     { background: #fafafa !important; color: #616161 !important; }
    .chip-resolved   { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .chip-reopened   { background: #fce4ec !important; color: #880e4f !important; }
    .chip-closed     { background: #f5f5f5 !important; color: #9e9e9e !important; }
    .chip-escalated  { background: #fff8e1 !important; color: #f57f17 !important; }
  `],
})
export class PortalTicketListComponent implements OnInit {
  private readonly ticketService = inject(PortalTicketService);

  readonly tickets = signal<PortalTicketPage['items']>([]);
  readonly loading = signal(true);
  readonly statusFilter = new FormControl('');
  readonly columns = ['ticketNumber', 'subject', 'status', 'priority', 'createdAt'];

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges.subscribe(s => this.load(s || undefined));
  }

  private load(status?: string): void {
    this.loading.set(true);
    this.ticketService.list(status).subscribe({
      next: page => {
        this.tickets.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
