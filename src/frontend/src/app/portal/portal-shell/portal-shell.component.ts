import { Component, inject } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [RouterOutlet, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary">
      <span class="brand">Customer Portal</span>
      <span class="spacer"></span>
      <span class="user-name">{{ user()?.fullName }}</span>
      <button mat-icon-button (click)="logout()" matTooltip="Sign out">
        <mat-icon>logout</mat-icon>
      </button>
    </mat-toolbar>

    <main class="portal-content">
      <router-outlet />
    </main>
  `,
  styles: [`
    .brand { font-size: 18px; font-weight: 600; }
    .spacer { flex: 1; }
    .user-name { font-size: 14px; margin-right: 8px; opacity: 0.9; }
    .portal-content { max-width: 960px; margin: 24px auto; padding: 0 16px; }
  `],
})
export class PortalShellComponent {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  readonly user = this.authStore.user;

  logout(): void {
    this.authStore.clearToken();
    this.router.navigate(['/portal/login']);
  }
}
