import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthStore } from '../../auth/auth.store';

interface AdminNavItem {
  label: string;
  icon: string;
  route: string;
  roles?: string[];
}

const ADMIN_NAV_ITEMS: AdminNavItem[] = [
  { label: 'Users', icon: 'people', route: 'users', roles: ['Admin'] },
  { label: 'Departments', icon: 'business', route: 'departments', roles: ['Admin', 'Manager'] },
  { label: 'Branches', icon: 'location_on', route: 'branches', roles: ['Admin', 'Manager'] },
  { label: 'Categories', icon: 'category', route: 'categories', roles: ['Admin', 'Manager'] },
  { label: 'Templates', icon: 'article', route: 'templates', roles: ['Admin'] },
];

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
  ],
  templateUrl: './admin-shell.component.html',
})
export class AdminShellComponent {
  readonly authStore = inject(AuthStore);

  readonly collapsed = signal(false);

  readonly navItems = ADMIN_NAV_ITEMS;

  get user() {
    return this.authStore.user();
  }

  visibleNavItems(): AdminNavItem[] {
    const role = this.user?.role;
    return this.navItems.filter(item => !item.roles || !role || item.roles.includes(role));
  }

  toggleSidenav(): void {
    this.collapsed.update(v => !v);
  }
}
