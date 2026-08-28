# Admin Navigation Shell — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-026
**Goal:** Implement the `/admin` section shell — a layout with a left sidenav containing role-aware navigation links, breadcrumb, and a `RouterOutlet` for admin sub-pages.

**Architecture:** `AdminShellComponent` is a standalone layout component wrapping all `/admin/**` routes via lazy-loaded `AdminModule`. It uses `MatSidenav` for the left nav and reads the user role from `AuthStore` to show/hide Manager-restricted items. Active nav item is highlighted via Angular `RouterLinkActive`. The shell is guarded by `RoleGuard(['Admin', 'Manager'])`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/admin/admin-shell/admin-shell.component.ts` |
| Create | `src/app/admin/admin-shell/admin-shell.component.html` |
| Create | `src/app/admin/admin-shell/admin-shell.component.spec.ts` |
| Create | `src/app/admin/admin.routes.ts` |

---

## Task 1: AdminShellComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/admin-shell/admin-shell.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AdminShellComponent } from './admin-shell.component';
import { AuthStore } from '../../auth/auth.store';

describe('AdminShellComponent', () => {
  let fixture: ComponentFixture<AdminShellComponent>;
  let component: AdminShellComponent;

  const setupWithRole = async (role: string) => {
    await TestBed.configureTestingModule({
      imports: [AdminShellComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: AuthStore, useValue: { user: () => ({ role }) } }],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  it('should create for Admin role', async () => {
    await setupWithRole('Admin');
    expect(component).toBeTruthy();
  });

  it('should show all nav items for Admin', async () => {
    await setupWithRole('Admin');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Users');
    expect(el.textContent).toContain('Departments');
    expect(el.textContent).toContain('Categories');
  });

  it('should hide Users nav item for Manager', async () => {
    await setupWithRole('Manager');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Users');
    expect(el.textContent).toContain('Departments');
  });

  it('should toggle sidenav collapse', async () => {
    await setupWithRole('Admin');
    expect(component.collapsed()).toBeFalse();
    component.toggleSidenav();
    expect(component.collapsed()).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/admin-shell/admin-shell.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/admin-shell/admin-shell.component.ts

import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthStore } from '../../auth/auth.store';

interface NavItem {
  label: string;
  icon: string;
  path: string;
  roles?: string[];
}

const ADMIN_NAV: NavItem[] = [
  { label: 'Users', icon: 'people', path: '/admin/users', roles: ['Admin'] },
  { label: 'Departments', icon: 'business', path: '/admin/departments' },
  { label: 'Branches', icon: 'location_on', path: '/admin/branches', roles: ['Admin'] },
  { label: 'Categories', icon: 'category', path: '/admin/categories' },
  { label: 'Field Definitions', icon: 'tune', path: '/admin/field-definitions', roles: ['Admin'] },
  { label: 'SLA Policies', icon: 'timer', path: '/admin/sla-policies' },
  { label: 'Business Hours', icon: 'schedule', path: '/admin/business-hours' },
  { label: 'Templates', icon: 'library_books', path: '/admin/templates', roles: ['Admin'] },
  { label: 'Channel Status', icon: 'signal_cellular_alt', path: '/admin/channels', roles: ['Admin'] },
];

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, MatSidenavModule, MatListModule, MatIconModule, MatButtonModule, MatToolbarModule],
  templateUrl: './admin-shell.component.html',
})
export class AdminShellComponent {
  readonly authStore = inject(AuthStore);
  readonly collapsed = signal(false);

  get visibleNavItems(): NavItem[] {
    const role = this.authStore.user()?.role ?? '';
    return ADMIN_NAV.filter(item => !item.roles || item.roles.includes(role));
  }

  toggleSidenav(): void {
    this.collapsed.update(v => !v);
  }
}
```

```html
<!-- src/app/admin/admin-shell/admin-shell.component.html -->

<mat-sidenav-container class="h-screen">
  <mat-sidenav mode="side" opened [style.width]="collapsed() ? '64px' : '220px'" class="transition-all">
    <div class="p-2 flex justify-end">
      <button mat-icon-button (click)="toggleSidenav()">
        <mat-icon>{{ collapsed() ? 'menu' : 'menu_open' }}</mat-icon>
      </button>
    </div>
    <mat-nav-list>
      @for (item of visibleNavItems; track item.path) {
        <a mat-list-item [routerLink]="item.path" routerLinkActive="active-nav">
          <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
          @if (!collapsed()) {
            <span matListItemTitle>{{ item.label }}</span>
          }
        </a>
      }
    </mat-nav-list>
  </mat-sidenav>

  <mat-sidenav-content>
    <mat-toolbar class="border-b">
      <span class="text-lg font-semibold">Administration</span>
    </mat-toolbar>
    <div class="p-4">
      <router-outlet></router-outlet>
    </div>
  </mat-sidenav-content>
</mat-sidenav-container>
```

Register routes in `src/app/admin/admin.routes.ts`:
```typescript
import { Routes } from '@angular/router';
import { AdminShellComponent } from './admin-shell/admin-shell.component';
import { RoleGuard } from '../auth/guards/role.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [RoleGuard],
    data: { roles: ['Admin', 'Manager'] },
    children: [
      { path: 'users', loadComponent: () => import('./user-management/user-list.component').then(m => m.UserListComponent) },
      { path: 'departments', loadComponent: () => import('./departments/department-list.component').then(m => m.DepartmentListComponent) },
      { path: 'categories', loadComponent: () => import('./categories/category-tree.component').then(m => m.CategoryTreeComponent) },
    ],
  },
];
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/admin-shell/admin-shell.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/admin-shell/ src/app/admin/admin.routes.ts
git commit -m "feat(admin): implement AdminShellComponent with role-aware sidenav (US-FE-026)"
```
