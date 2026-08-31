import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="welcome-wrap">
      <h1 class="welcome-title">Welcome, {{ user()?.fullName ?? 'Customer' }}</h1>
      <p class="welcome-sub">How can we help you today?</p>

      <div class="quick-actions">
        <a class="action-card" routerLink="/portal/tickets/new">
          <mat-icon class="action-icon">add_circle_outline</mat-icon>
          <span class="action-label">Submit a Ticket</span>
          <span class="action-desc">Report an issue or request support</span>
        </a>
        <a class="action-card" routerLink="/portal/tickets">
          <mat-icon class="action-icon">confirmation_number</mat-icon>
          <span class="action-label">My Tickets</span>
          <span class="action-desc">View and manage your support tickets</span>
        </a>
        <a class="action-card" routerLink="/portal/kb">
          <mat-icon class="action-icon">menu_book</mat-icon>
          <span class="action-label">Knowledge Base</span>
          <span class="action-desc">Browse articles and find answers</span>
        </a>
      </div>
    </div>
  `,
  styles: [`
    .welcome-wrap {
      padding: 16px 0;
      max-width: 720px;
    }

    .welcome-title {
      font-size: 1.75rem;
      font-weight: 700;
      margin: 0 0 8px;
      color: var(--mat-sys-on-surface);
    }

    .welcome-sub {
      font-size: 1rem;
      color: var(--mat-sys-on-surface-variant);
      margin: 0 0 40px;
    }

    .quick-actions {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 16px;
    }

    .action-card {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 8px;
      padding: 24px;
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
      background: var(--mat-sys-surface);
      text-decoration: none;
      color: inherit;
      transition: box-shadow 0.15s, border-color 0.15s;

      &:hover {
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        border-color: var(--mat-sys-primary);
      }
    }

    .action-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      color: var(--mat-sys-primary);
    }

    .action-label {
      font-size: 1rem;
      font-weight: 600;
      color: var(--mat-sys-on-surface);
    }

    .action-desc {
      font-size: 0.8rem;
      color: var(--mat-sys-on-surface-variant);
      line-height: 1.4;
    }
  `],
})
export class PortalDashboardComponent {
  private readonly authStore = inject(AuthStore);
  readonly user = this.authStore.user;
}
