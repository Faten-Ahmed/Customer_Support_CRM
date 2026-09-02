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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { AgentAiAssistantComponent } from '../shared/agent-ai-assistant/agent-ai-assistant.component';
import { I18nService } from '../shared/services/i18n.service';
import { TranslatePipe } from '../shared/pipes/translate.pipe';
import { TRANSLATIONS } from '../shared/i18n/translations';
import { NotificationBellComponent } from '../notifications/notification-bell/notification-bell.component';
import { SignalRService, HubName } from '../core/signalr.service';
import { NotificationService } from '../notifications/notification.service';

interface NavItem {
  labelKey: string;
  icon: string;
  route: string;
  roles?: string[];
  children?: NavItem[];
}

const NAV_ITEMS: NavItem[] = [
  { labelKey: 'nav.dashboard', icon: 'dashboard', route: '/app/dashboard' },
  { labelKey: 'nav.tickets', icon: 'confirmation_number', route: '/app/tickets' },
  { labelKey: 'nav.customers', icon: 'people', route: '/app/customers', roles: ['Admin', 'Manager', 'Agent'] },
  { labelKey: 'nav.knowledgeBase', icon: 'menu_book', route: '/app/kb' },
  {
    labelKey: 'nav.reports', icon: 'bar_chart', route: '/app/reports', roles: ['Admin', 'Manager'],
    children: [
      { labelKey: 'nav.reports.dashboard', icon: 'space_dashboard',     route: '/app/reports/dashboard', roles: ['Admin', 'Manager'] },
      { labelKey: 'nav.reports.tickets',   icon: 'confirmation_number', route: '/app/reports/tickets',   roles: ['Admin', 'Manager'] },
      { labelKey: 'nav.reports.sla',       icon: 'verified_user',       route: '/app/reports/sla',       roles: ['Admin', 'Manager'] },
      { labelKey: 'nav.reports.agents',    icon: 'support_agent',       route: '/app/reports/agents',    roles: ['Admin', 'Manager'] },
      { labelKey: 'nav.reports.csat',      icon: 'star_rate',           route: '/app/reports/csat',      roles: ['Admin', 'Manager'] },
    ],
  },
  { labelKey: 'nav.templates', icon: 'library_books', route: '/app/settings/templates', roles: ['Admin', 'Manager', 'Agent'] },
  { labelKey: 'nav.tasks', icon: 'checklist', route: '/app/settings/tasks', roles: ['Agent', 'Manager'] },
  { labelKey: 'nav.liveChat', icon: 'support_agent', route: '/app/live-chat', roles: ['Agent', 'Manager'] },
  { labelKey: 'nav.admin', icon: 'admin_panel_settings', route: '/app/admin', roles: ['Admin'] },
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatSidenavModule, MatToolbarModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatTooltipModule, MatProgressBarModule, MatBadgeModule, MatDividerModule,
    MatSnackBarModule,
    AgentAiAssistantComponent, TranslatePipe, NotificationBellComponent,
  ],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent implements OnInit, OnDestroy {
  readonly authStore = inject(AuthStore);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);
  private readonly signalR = inject(SignalRService);
  private readonly notifService = inject(NotificationService);
  private readonly snackBar = inject(MatSnackBar);

  readonly collapsed = signal(localStorage.getItem('sidenav_collapsed') === 'true');
  readonly aiOpen = signal(false);
  readonly loading = signal(false);
  readonly expandedGroups = signal<Set<string>>(new Set());
  readonly pendingChatCount = signal(0);

  readonly navItems = NAV_ITEMS;

  private routerSub!: Subscription;
  private signalRSubs = new Subscription();

  get user() { return this.authStore.user(); }

  visibleNavItems(): NavItem[] {
    const role = this.user?.role;
    return this.navItems.filter(item => !item.roles || !role || item.roles.includes(role));
  }

  isExpanded(route: string): boolean {
    return this.expandedGroups().has(route);
  }

  hasActiveChild(children: NavItem[]): boolean {
    return children.some(c => this.router.url.startsWith(c.route));
  }

  toggleGroup(route: string): void {
    this.expandedGroups.update(set => {
      const next = new Set(set);
      next.has(route) ? next.delete(route) : next.add(route);
      return next;
    });
  }

  ngOnInit(): void {
    // Auto-expand any group whose child matches the current URL
    this.expandedGroups.update(set => {
      const next = new Set(set);
      for (const item of NAV_ITEMS) {
        if (item.children?.some(c => this.router.url.startsWith(c.route))) {
          next.add(item.route);
        }
      }
      return next;
    });

    this.routerSub = this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) this.loading.set(true);
      if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError
      ) {
        this.loading.set(false);
        if (event instanceof NavigationEnd && event.urlAfterRedirects.startsWith('/app/live-chat')) {
          this.pendingChatCount.set(0);
        }
        // Auto-expand the group containing the newly active child
        this.expandedGroups.update(set => {
          const next = new Set(set);
          for (const item of NAV_ITEMS) {
            if (item.children?.some(c => this.router.url.startsWith(c.route))) {
              next.add(item.route);
            }
          }
          return next;
        });
      }
    });

    this.signalR.connect(HubName.Notification);
    this.signalRSubs.add(
      this.signalR.notification$.subscribe(n => this.notifService.pushNotification(n))
    );
    this.signalRSubs.add(
      this.signalR.unreadCountUpdated$.subscribe(count => this.notifService.setUnreadCount(count))
    );
    this.signalRSubs.add(
      this.signalR.liveChatHandoff$.subscribe(evt => {
        const role = this.user?.role;
        if (role !== 'Agent' && role !== 'Manager') return;
        if (!this.router.url.startsWith('/app/live-chat')) {
          this.pendingChatCount.update(n => n + 1);
          const ref = this.snackBar.open(
            `${TRANSLATIONS['notif.customerWaiting']?.[this.i18n.lang()] ?? 'Customer waiting'}: ${evt.customerName}`,
            TRANSLATIONS['notif.goToChat']?.[this.i18n.lang()] ?? 'Go to Live Chat',
            { duration: 8000, panelClass: 'live-chat-snack' },
          );
          ref.onAction().subscribe(() => this.router.navigate(['/app/live-chat']));
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
    this.signalRSubs.unsubscribe();
    this.signalR.disconnectAll();
  }

  toggleSidenav(): void {
    this.collapsed.update(v => {
      const next = !v;
      localStorage.setItem('sidenav_collapsed', String(next));
      return next;
    });
  }

  toggleAi(): void { this.aiOpen.update(v => !v); }

  logout(): void {
    this.authStore.clearToken();
    this.router.navigate(['/login']);
  }
}
