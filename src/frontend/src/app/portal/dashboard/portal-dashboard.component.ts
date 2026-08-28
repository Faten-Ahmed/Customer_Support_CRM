import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <div class="dashboard-header">
      <h1>My Support Tickets</h1>
      <a mat-flat-button color="primary" routerLink="/portal/tickets/new">
        <mat-icon>add</mat-icon> Submit New Ticket
      </a>
    </div>

    <mat-card class="empty-card">
      <mat-card-content>
        <mat-icon class="empty-icon">confirmation_number</mat-icon>
        <p>You have no tickets yet. Click <strong>Submit New Ticket</strong> to get started.</p>
      </mat-card-content>
    </mat-card>
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
  `],
})
export class PortalDashboardComponent {}
