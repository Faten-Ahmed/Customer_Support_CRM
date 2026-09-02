import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthStore } from '../../auth/auth.store';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="welcome-wrap">
      <h1 class="welcome-title">{{ 'portal.welcome' | translate }}, {{ user()?.fullName ?? ('portal.customerFallback' | translate) }}</h1>
      <p class="welcome-sub">{{ 'portal.helpToday' | translate }}</p>

      <div class="quick-actions">
        <a class="action-card" routerLink="/portal/tickets/new">
          <mat-icon class="action-icon">add_circle_outline</mat-icon>
          <span class="action-label">{{ 'portal.submitTicket' | translate }}</span>
          <span class="action-desc">{{ 'portal.submitTicketDesc' | translate }}</span>
        </a>
        <a class="action-card" routerLink="/portal/tickets">
          <mat-icon class="action-icon">confirmation_number</mat-icon>
          <span class="action-label">{{ 'portal.myTickets' | translate }}</span>
          <span class="action-desc">{{ 'portal.myTicketsDesc' | translate }}</span>
        </a>
        <a class="action-card" routerLink="/portal/kb">
          <mat-icon class="action-icon">menu_book</mat-icon>
          <span class="action-label">{{ 'portal.knowledgeBase' | translate }}</span>
          <span class="action-desc">{{ 'portal.kbDesc' | translate }}</span>
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
