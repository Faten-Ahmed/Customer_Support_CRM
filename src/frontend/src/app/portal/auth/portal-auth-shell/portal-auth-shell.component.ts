import { Component, signal, HostBinding } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { PortalLoginComponent } from '../portal-login/portal-login.component';
import { PortalRegisterComponent } from '../portal-register/portal-register.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-portal-auth-shell',
  standalone: true,
  imports: [MatTabsModule, PortalLoginComponent, PortalRegisterComponent, TranslatePipe],
  template: `
    <div class="portal-auth-container">
      <div class="brand">{{ 'portal.brand' | translate }}</div>
      <p class="subtitle">{{ 'portal.subtitle' | translate }}</p>
      <mat-tab-group animationDuration="200ms">
        <mat-tab [label]="'portal.tabSignIn' | translate">
          <div class="tab-content">
            <app-portal-login />
          </div>
        </mat-tab>
        <mat-tab [label]="'portal.tabCreateAccount' | translate">
          <div class="tab-content">
            <app-portal-register />
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: var(--mat-sys-surface-container-lowest);
    }
    .portal-auth-container {
      width: 100%;
      max-width: 480px;
      background: var(--mat-sys-surface);
      border-radius: 16px;
      box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }
    .brand {
      text-align: center;
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--mat-sys-primary);
      letter-spacing: 0.05em;
      padding: 1.5rem 2rem 0;
    }
    .subtitle {
      text-align: center;
      font-size: 0.875rem;
      color: var(--mat-sys-on-surface-variant);
      margin: 0.25rem 0 0;
      padding: 0 2rem;
    }
    .tab-content {
      padding: 1.5rem 2rem 2rem;
    }
  `],
})
export class PortalAuthShellComponent {
  isRtl = signal(false);

  @HostBinding('attr.dir')
  get dir(): string | null {
    return this.isRtl() ? 'rtl' : null;
  }
}
