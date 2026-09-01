import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatListModule } from '@angular/material/list';
import { Subscription } from 'rxjs';
import { AuthStore } from '../../auth/auth.store';
import { I18nService } from '../../shared/services/i18n.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { NotificationBellComponent } from '../../notifications/notification-bell/notification-bell.component';
import { SignalRService, HubName } from '../../core/signalr.service';
import { NotificationService } from '../../notifications/notification.service';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatButtonModule, MatIconModule, MatTooltipModule, MatListModule,
    TranslatePipe, NotificationBellComponent,
  ],
  template: `
    <mat-toolbar color="primary" class="portal-toolbar">
      <span class="brand">{{ 'shell.customerPortal' | translate }}</span>
      <span class="spacer"></span>
      <button mat-button (click)="i18n.toggleLang()" [matTooltip]="'shell.switchLang' | translate" class="lang-toggle">
        {{ i18n.lang() === 'en' ? 'ع' : 'EN' }}
      </button>
      <app-notification-bell />
      <span class="user-name">{{ user()?.fullName }}</span>
      <button mat-icon-button (click)="logout()" [matTooltip]="'shell.signOut' | translate">
        <mat-icon>logout</mat-icon>
      </button>
    </mat-toolbar>

    <div class="portal-layout">
      <nav class="portal-sidenav">
        <mat-nav-list>
          <a mat-list-item routerLink="/portal/dashboard" routerLinkActive="nav-active">
            <mat-icon matListItemIcon>dashboard</mat-icon>
            <span matListItemTitle i18n="@@portal.nav.dashboard">Dashboard</span>
          </a>
          <a mat-list-item routerLink="/portal/tickets" routerLinkActive="nav-active" [routerLinkActiveOptions]="{ exact: false }">
            <mat-icon matListItemIcon>confirmation_number</mat-icon>
            <span matListItemTitle i18n="@@portal.nav.tickets">My Tickets</span>
          </a>
          <a mat-list-item routerLink="/portal/kb" routerLinkActive="nav-active" [routerLinkActiveOptions]="{ exact: false }">
            <mat-icon matListItemIcon>menu_book</mat-icon>
            <span matListItemTitle i18n="@@portal.nav.kb">Knowledge Base</span>
          </a>
          <a mat-list-item routerLink="/portal/profile" routerLinkActive="nav-active">
            <mat-icon matListItemIcon>person</mat-icon>
            <span matListItemTitle i18n="@@portal.nav.profile">My Profile</span>
          </a>
        </mat-nav-list>
      </nav>

      <main class="portal-content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .portal-toolbar { position: sticky; top: 0; z-index: 100; }
    .brand { font-size: 18px; font-weight: 600; }
    .spacer { flex: 1; }
    .lang-toggle { font-size: 15px; font-weight: 700; min-width: 40px; }
    .user-name { font-size: 14px; margin-inline-end: 8px; opacity: 0.9; }

    .portal-layout {
      display: flex;
      min-height: calc(100vh - 64px);
    }

    .portal-sidenav {
      width: 220px;
      flex-shrink: 0;
      border-right: 1px solid var(--mat-sys-outline-variant);
      background: var(--mat-sys-surface);
      padding-top: 8px;
    }

    .portal-content {
      flex: 1;
      min-width: 0;
      padding: 24px;
      max-width: 960px;
    }

    ::ng-deep .nav-active {
      background: var(--mat-sys-secondary-container) !important;
      color: var(--mat-sys-on-secondary-container) !important;
      border-radius: 0 24px 24px 0;
    }
  `],
})
export class PortalShellComponent implements OnInit, OnDestroy {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly signalR = inject(SignalRService);
  private readonly notifService = inject(NotificationService);
  readonly i18n = inject(I18nService);

  readonly user = this.authStore.user;

  private signalRSubs = new Subscription();

  ngOnInit(): void {
    this.signalR.connect(HubName.Notification);
    this.signalRSubs.add(
      this.signalR.notification$.subscribe(n => this.notifService.pushNotification(n))
    );
    this.signalRSubs.add(
      this.signalR.unreadCountUpdated$.subscribe(count => this.notifService.setUnreadCount(count))
    );
  }

  ngOnDestroy(): void {
    this.signalRSubs.unsubscribe();
    this.signalR.disconnect(HubName.Notification);
  }

  logout(): void {
    this.authStore.clearToken();
    this.router.navigate(['/portal/login']);
  }
}
