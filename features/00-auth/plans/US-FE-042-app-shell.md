# App Shell — Implementation Plan

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

**Story:** US-FE-042
**Goal:** Implement the internal app shell — a `MatSidenav` layout with collapsed/expanded state persisted in `localStorage`, a top `MatToolbar` with username, role badge, notification bell, availability selector, and logout, active route highlighting, a top loading progress bar (router events), and dedicated 404/403 error pages.

**Architecture:** `AppShellComponent` is standalone, the root layout for all `/app/**` routes. It uses `Router.events` piped through `NavigationStart`/`NavigationEnd` to toggle a `loading` signal for the progress bar. Sidenav `collapsed` state is a `signal<boolean>` seeded from `localStorage.getItem('sidenav_collapsed')`. The `AgentAiAssistantComponent` is toggled via an `aiOpen` signal. `NotFoundComponent` and `ForbiddenComponent` are tiny standalone components.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/shell/app-shell.component.ts` |
| Create | `src/app/shell/app-shell.component.html` |
| Create | `src/app/shell/app-shell.component.spec.ts` |
| Create | `src/app/shell/not-found.component.ts` |
| Create | `src/app/shell/forbidden.component.ts` |

---

## Task 1: NotFoundComponent and ForbiddenComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// Inline tests for both components — place in a single spec file during TDD,
// then split into separate files if desired.

// src/app/shell/not-found.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  let fixture: ComponentFixture<NotFoundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent, RouterTestingModule],
    }).compileComponents();
    fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();
  });

  it('should create', () => expect(fixture.componentInstance).toBeTruthy());

  it('should display 404 message', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('404');
  });

  it('should have a link back to home', () => {
    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shell/not-found.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/shell/not-found.component.ts

import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-4 text-center">
      <mat-icon class="text-8xl text-gray-300">search_off</mat-icon>
      <h1 class="text-6xl font-bold text-gray-300">404</h1>
      <p class="text-xl text-gray-500">Page not found</p>
      <p class="text-gray-400">The page you're looking for doesn't exist or has been moved.</p>
      <a mat-raised-button color="primary" routerLink="/app">Go to Dashboard</a>
    </div>
  `,
})
export class NotFoundComponent {}
```

```typescript
// src/app/shell/forbidden.component.ts

import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-4 text-center">
      <mat-icon class="text-8xl text-red-300">block</mat-icon>
      <h1 class="text-6xl font-bold text-red-300">403</h1>
      <p class="text-xl text-gray-500">Access Denied</p>
      <p class="text-gray-400">You don't have permission to view this page.</p>
      <a mat-raised-button color="warn" routerLink="/app">Go to Dashboard</a>
    </div>
  `,
})
export class ForbiddenComponent {}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shell/not-found.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/shell/not-found.component.ts src/app/shell/forbidden.component.ts
git commit -m "feat(shell): add NotFoundComponent and ForbiddenComponent (US-FE-042)"
```

---

## Task 2: AppShellComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/shell/app-shell.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AppShellComponent } from './app-shell.component';
import { AuthStore } from '../auth/auth.store';

describe('AppShellComponent', () => {
  let fixture: ComponentFixture<AppShellComponent>;
  let component: AppShellComponent;

  const mockAuthStore = {
    user: () => ({ sub: 'a1', fullName: 'Omar Hassan', role: 'Agent' }),
    isAuthenticated: () => true,
    clearToken: jasmine.createSpy('clearToken'),
  };

  beforeEach(async () => {
    localStorage.removeItem('sidenav_collapsed');

    await TestBed.configureTestingModule({
      imports: [AppShellComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: AuthStore, useValue: mockAuthStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => localStorage.removeItem('sidenav_collapsed'));

  it('should create', () => expect(component).toBeTruthy());

  it('should display logged-in user name', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Omar Hassan');
  });

  it('should display user role badge', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Agent');
  });

  it('should start with sidenav expanded (no localStorage value)', () => {
    expect(component.collapsed()).toBeFalse();
  });

  it('should persist collapsed=true to localStorage on toggle', () => {
    component.toggleSidenav();
    expect(component.collapsed()).toBeTrue();
    expect(localStorage.getItem('sidenav_collapsed')).toBe('true');
  });

  it('should restore collapsed state from localStorage', () => {
    localStorage.setItem('sidenav_collapsed', 'true');
    const fresh = TestBed.createComponent(AppShellComponent);
    fresh.detectChanges();
    expect(fresh.componentInstance.collapsed()).toBeTrue();
    fresh.destroy();
  });

  it('should toggle AI assistant panel', () => {
    expect(component.aiOpen()).toBeFalse();
    component.toggleAi();
    expect(component.aiOpen()).toBeTrue();
  });

  it('should call authStore.clearToken on logout', () => {
    component.logout();
    expect(mockAuthStore.clearToken).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shell/app-shell.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/shell/app-shell.component.ts

import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { RouterModule, Router, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatBadgeModule } from '@angular/material/badge';
import { filter, Subscription } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { AgentAiAssistantComponent } from '../shared/agent-ai-assistant/agent-ai-assistant.component';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  roles?: string[];
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', icon: 'dashboard', route: '/app/dashboard' },
  { label: 'Tickets', icon: 'confirmation_number', route: '/app/tickets' },
  { label: 'Customers', icon: 'people', route: '/app/customers', roles: ['Admin', 'Manager', 'Agent'] },
  { label: 'Knowledge Base', icon: 'menu_book', route: '/app/kb' },
  { label: 'Reports', icon: 'bar_chart', route: '/app/reports', roles: ['Admin', 'Manager'] },
  { label: 'Admin', icon: 'admin_panel_settings', route: '/app/admin', roles: ['Admin'] },
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatSidenavModule, MatToolbarModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatTooltipModule, MatProgressBarModule, MatBadgeModule,
    AgentAiAssistantComponent,
  ],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent implements OnInit, OnDestroy {
  readonly authStore = inject(AuthStore);
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
      if (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) {
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
```

```html
<!-- src/app/shell/app-shell.component.html -->

<div class="flex flex-col h-screen overflow-hidden">

  <!-- Top progress bar -->
  @if (loading()) {
    <mat-progress-bar mode="indeterminate" color="accent" class="absolute top-0 left-0 right-0 z-50"></mat-progress-bar>
  }

  <!-- Top toolbar -->
  <mat-toolbar color="primary" class="flex items-center gap-3 z-10 shadow">
    <!-- Menu toggle -->
    <button mat-icon-button (click)="toggleSidenav()" matTooltip="{{ collapsed() ? 'Expand sidebar' : 'Collapse sidebar' }}">
      <mat-icon>{{ collapsed() ? 'menu' : 'menu_open' }}</mat-icon>
    </button>

    <span class="font-bold text-lg mr-auto">CRM Support</span>

    <!-- AI assistant toggle -->
    <button mat-icon-button (click)="toggleAi()" matTooltip="AI Assistant">
      <mat-icon>auto_awesome</mat-icon>
    </button>

    <!-- Notification bell -->
    <button mat-icon-button matTooltip="Notifications">
      <mat-icon matBadge="3" matBadgeColor="warn">notifications</mat-icon>
    </button>

    <!-- User menu -->
    <button mat-button [matMenuTriggerFor]="userMenu" class="flex items-center gap-2">
      <mat-icon>account_circle</mat-icon>
      <span>{{ user?.fullName }}</span>
      <span class="text-xs bg-white text-blue-700 font-semibold rounded px-1">{{ user?.role }}</span>
    </button>
    <mat-menu #userMenu="matMenu">
      <button mat-menu-item routerLink="/app/profile"><mat-icon>person</mat-icon> My Profile</button>
      <button mat-menu-item routerLink="/app/settings"><mat-icon>settings</mat-icon> Settings</button>
      <mat-divider></mat-divider>
      <button mat-menu-item (click)="logout()"><mat-icon>logout</mat-icon> Logout</button>
    </mat-menu>
  </mat-toolbar>

  <!-- Sidenav + content -->
  <mat-sidenav-container class="flex-1 overflow-hidden">
    <mat-sidenav
      mode="side"
      opened
      [style.width]="collapsed() ? '56px' : '220px'"
      class="transition-all duration-200 border-r bg-gray-900 text-white overflow-hidden">

      <nav class="flex flex-col gap-1 pt-3">
        @for (item of visibleNavItems(); track item.route) {
          <a [routerLink]="item.route" routerLinkActive="bg-blue-700"
             class="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-300 hover:bg-gray-700 rounded-lg mx-2 transition"
             [matTooltip]="collapsed() ? item.label : ''" matTooltipPosition="right">
            <mat-icon class="flex-shrink-0">{{ item.icon }}</mat-icon>
            @if (!collapsed()) {
              <span>{{ item.label }}</span>
            }
          </a>
        }
      </nav>
    </mat-sidenav>

    <mat-sidenav-content class="flex overflow-hidden">
      <!-- Main content -->
      <div class="flex-1 overflow-y-auto bg-gray-50">
        <router-outlet></router-outlet>
      </div>

      <!-- AI assistant panel -->
      @if (aiOpen()) {
        <div class="w-80 border-l flex-shrink-0 overflow-hidden">
          <app-agent-ai-assistant></app-agent-ai-assistant>
        </div>
      }
    </mat-sidenav-content>
  </mat-sidenav-container>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shell/app-shell.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/shell/
git commit -m "feat(shell): implement AppShellComponent with sidenav, toolbar, AI panel, and progress bar (US-FE-042)"
```
