import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthStore } from '../auth/auth.store';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  template: `
    <div style="padding: 24px; max-width: 600px; margin: 0 auto;">
      <h1 style="margin: 0 0 24px; font-size: 22px; font-weight: 600;">My Profile</h1>

      <mat-card appearance="outlined">
        <mat-card-content style="padding: 24px;">
          <div style="display: flex; align-items: center; gap: 20px; margin-bottom: 24px;">
            <div style="width: 72px; height: 72px; border-radius: 50%; background: var(--mat-sys-primary); display: flex; align-items: center; justify-content: center;">
              <mat-icon style="font-size: 36px; width: 36px; height: 36px; color: white;">person</mat-icon>
            </div>
            <div>
              <div style="font-size: 20px; font-weight: 600;">{{ user()?.fullName }}</div>
              <div style="font-size: 13px; color: var(--mat-sys-on-surface-variant); margin-top: 2px;">
                {{ user()?.role }}
              </div>
            </div>
          </div>

          <div style="display: flex; flex-direction: column; gap: 16px;">
            <div style="display: flex; gap: 12px; align-items: center; padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);">
              <mat-icon style="color: var(--mat-sys-on-surface-variant);">email</mat-icon>
              <div>
                <div style="font-size: 11px; color: var(--mat-sys-on-surface-variant); text-transform: uppercase; letter-spacing: 0.05em;">Email</div>
                <div style="margin-top: 2px;">{{ user()?.email }}</div>
              </div>
            </div>

            <div style="display: flex; gap: 12px; align-items: center; padding: 12px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);">
              <mat-icon style="color: var(--mat-sys-on-surface-variant);">badge</mat-icon>
              <div>
                <div style="font-size: 11px; color: var(--mat-sys-on-surface-variant); text-transform: uppercase; letter-spacing: 0.05em;">Role</div>
                <div style="margin-top: 2px;">{{ user()?.role }}</div>
              </div>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class ProfileComponent {
  readonly authStore = inject(AuthStore);
  readonly user = this.authStore.user;
}
