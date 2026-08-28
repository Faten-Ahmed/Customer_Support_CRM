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
import { PortalTicketService, PortalTicketPage } from '../services/portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatFormFieldModule, MatProgressSpinnerModule, MatChipsModule,
  ],
  template: `
    <div class="list-wrap">
      <div class="list-header">
        <h1>My Tickets</h1>
        <button mat-flat-button color="primary" routerLink="/portal/tickets/new">
          <mat-icon>add</mat-icon> New Ticket
        </button>
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
        <div class="empty">
          <mat-icon class="empty-icon">inbox</mat-icon>
          <p>No tickets found.</p>
          <a routerLink="/portal/tickets/new" mat-stroked-button>Submit a ticket</a>
        </div>
      } @else {
        <div class="card-list">
          @for (ticket of tickets(); track ticket.ticketNumber) {
            <mat-card class="ticket-card" [routerLink]="['/portal/tickets', ticket.id]">
              <mat-card-content>
                <div class="card-top">
                  <span class="ticket-num">{{ ticket.ticketNumber }}</span>
                  <mat-chip [class]="'chip-' + ticket.status.toLowerCase()">{{ ticket.status }}</mat-chip>
                </div>
                <p class="ticket-subject">{{ ticket.subject }}</p>
                <div class="card-bottom">
                  <span class="priority">{{ ticket.priority }}</span>
                  <span class="date">{{ ticket.createdAt | date:'mediumDate' }}</span>
                </div>
              </mat-card-content>
            </mat-card>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .list-wrap { max-width: 720px; margin: 0 auto; padding: 24px 16px; }
    .list-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
    h1 { margin: 0; font-size: 22px; font-weight: 600; }
    .filter-field { width: 200px; margin-bottom: 16px; display: block; }
    .center { display: flex; justify-content: center; padding: 48px; }
    .empty { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 48px; color: #888; }
    .empty-icon { font-size: 48px; height: 48px; width: 48px; }

    .card-list { display: flex; flex-direction: column; gap: 12px; }
    .ticket-card { cursor: pointer; transition: box-shadow 0.15s; }
    .ticket-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.12); }
    .card-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
    .ticket-num { font-size: 11px; color: #888; font-family: monospace; }
    .ticket-subject { margin: 0 0 10px; font-size: 15px; font-weight: 500; color: #212121; }
    .card-bottom { display: flex; justify-content: space-between; font-size: 12px; color: #666; }

    .chip-new { background: #e3f2fd !important; color: #1565c0 !important; }
    .chip-assigned { background: #fff3e0 !important; color: #e65100 !important; }
    .chip-inprogress { background: #f3e5f5 !important; color: #6a1b9a !important; }
    .chip-onhold { background: #fafafa !important; color: #616161 !important; }
    .chip-resolved { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .chip-reopened { background: #fce4ec !important; color: #880e4f !important; }
    .chip-closed { background: #f5f5f5 !important; color: #9e9e9e !important; }
    .chip-escalated { background: #fff8e1 !important; color: #f57f17 !important; }
  `],
})
export class PortalTicketListComponent implements OnInit {
  private readonly ticketService = inject(PortalTicketService);

  readonly tickets = signal<PortalTicketPage['items']>([]);
  readonly loading = signal(true);
  readonly statusFilter = new FormControl('');

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
