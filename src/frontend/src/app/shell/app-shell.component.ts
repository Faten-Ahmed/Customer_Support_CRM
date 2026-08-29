import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import {
  RouterModule, Router,
  NavigationStart, NavigationEnd, NavigationCancel, NavigationError,
} from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { Subscription } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { AgentAiAssistantComponent } from '../shared/agent-ai-assistant/agent-ai-assistant.component';
import { I18nService } from '../shared/services/i18n.service';
import { TranslatePipe } from '../shared/pipes/translate.pipe';

interface NavItem {
  labelKey: string;
  icon: string;
  route: string;
  roles?: string[];
}

const NAV_ITEMS: NavItem[] = [
  { labelKey: 'nav.dashboard', icon: 'dashboard', route: '/app/dashboard' },
  { labelKey: 'nav.tickets', icon: 'confirmation_number', route: '/app/tickets' },
  { labelKey: 'nav.customers', icon: 'people', route: '/app/customers', roles: ['Admin', 'Manager', 'Agent'] },
  { labelKey: 'nav.knowledgeBase', icon: 'menu_book', route: '/app/kb' },
  { labelKey: 'nav.reports', icon: 'bar_chart', route: '/app/reports', roles: ['Admin', 'Manager'] },
  { labelKey: 'nav.admin', icon: 'admin_panel_settings', route: '/app/admin', roles: ['Admin'] },
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatSidenavModule, MatToolbarModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatTooltipModule, MatProgressBarModule, MatBadgeModule, MatDividerModule,
    AgentAiAssistantComponent, TranslatePipe,
  ],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent implements OnInit, OnDestroy {
  readonly authStore = inject(AuthStore);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);

  readonly collapsed = signal(localStorage.getItem('sidenav_collapsed') === 'true');
  readonly aiOpen = signal(false);
  readonly loading = signal(false);

  readonly navItems = NAV_ITEMS;

  private routerSub!: Subscription;

  get user() { return this.authStore.user(); }

  visibleNavItems(): NavItem[] {
    const role = this.user?.role;
    return this.navItems.filter(item => !item.roles || !role || item.roles.includes(role));
  }

  ngOnInit(): void {
    this.routerSub = this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) this.loading.set(true);
      if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError
      ) {
        this.loading.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
  }

  toggleSidenav(): void {
    this.collapsed.update(v => {
      const next = !v;
      localStorage.setItem('sidenav_collapsed', String(next));
      return next;
    });
  }

  toggleAi(): void {
    this.aiOpen.update(v => !v);
  }

  logout(): void {
    this.authStore.clearToken();
    this.router.navigate(['/login']);
  }
}
