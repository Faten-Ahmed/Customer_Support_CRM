# Portal Shell (Layout & RTL Support) — Implementation Plan

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

**Story:** US-FE-032
**Goal:** Implement the customer portal layout shell — a header with bilingual nav links, EN/AR language toggle that switches RTL/LTR, responsive hamburger menu, footer, and unauthenticated routes without the header.

**Architecture:** `PortalShellComponent` is a standalone layout component wrapping all `/portal/**` routes. It uses `@angular/localize` for i18n and sets `document.documentElement.dir` to `rtl` or `ltr` on language toggle, along with applying Angular Material's `dir` attribute. Unauthenticated routes (`/portal/login`, `/portal/register`) use a separate `PortalAuthLayoutComponent` with no nav.

**Tech Stack:** Angular 21, TypeScript, Angular Material, `@angular/localize`, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/portal-shell/portal-shell.component.ts` |
| Create | `src/app/portal/portal-shell/portal-shell.component.html` |
| Create | `src/app/portal/portal-shell/portal-shell.component.spec.ts` |
| Create | `src/app/portal/services/i18n.service.ts` |
| Create | `src/app/portal/services/i18n.service.spec.ts` |
| Create | `src/app/portal/portal.routes.ts` |

---

## Task 1: I18nService (language/RTL toggle)

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/services/i18n.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { I18nService } from './i18n.service';

describe('I18nService', () => {
  let service: I18nService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [I18nService] });
    service = TestBed.inject(I18nService);
    document.documentElement.removeAttribute('dir');
    document.documentElement.removeAttribute('lang');
  });

  it('should default to English LTR', () => {
    expect(service.currentLang()).toBe('en');
    expect(service.isRtl()).toBeFalse();
  });

  it('setLanguage("ar") should set dir=rtl and lang=ar on <html>', () => {
    service.setLanguage('ar');
    expect(service.currentLang()).toBe('ar');
    expect(service.isRtl()).toBeTrue();
    expect(document.documentElement.getAttribute('dir')).toBe('rtl');
    expect(document.documentElement.getAttribute('lang')).toBe('ar');
  });

  it('setLanguage("en") should reset to ltr', () => {
    service.setLanguage('ar');
    service.setLanguage('en');
    expect(service.isRtl()).toBeFalse();
    expect(document.documentElement.getAttribute('dir')).toBe('ltr');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/i18n.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/services/i18n.service.ts

import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly _lang = signal<'en' | 'ar'>('en');

  readonly currentLang = this._lang.asReadonly();
  readonly isRtl = computed(() => this._lang() === 'ar');

  setLanguage(lang: 'en' | 'ar'): void {
    this._lang.set(lang);
    document.documentElement.setAttribute('lang', lang);
    document.documentElement.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');
    localStorage.setItem('portal_lang', lang);
  }

  loadSavedLanguage(): void {
    const saved = localStorage.getItem('portal_lang') as 'en' | 'ar' | null;
    if (saved) this.setLanguage(saved);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/i18n.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/i18n.service.ts src/app/portal/services/i18n.service.spec.ts
git commit -m "feat(portal): add I18nService for EN/AR RTL toggle (US-FE-032)"
```

---

## Task 2: PortalShellComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/portal-shell/portal-shell.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { PortalShellComponent } from './portal-shell.component';
import { I18nService } from '../services/i18n.service';
import { AuthStore } from '../../auth/auth.store';

describe('PortalShellComponent', () => {
  let fixture: ComponentFixture<PortalShellComponent>;
  let component: PortalShellComponent;
  let i18nService: jasmine.SpyObj<I18nService>;

  beforeEach(async () => {
    i18nService = jasmine.createSpyObj('I18nService', ['setLanguage', 'loadSavedLanguage'], {
      currentLang: jasmine.createSpy('currentLang').and.returnValue('en'),
      isRtl: jasmine.createSpy('isRtl').and.returnValue(false),
    });

    await TestBed.configureTestingModule({
      imports: [PortalShellComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: I18nService, useValue: i18nService },
        { provide: AuthStore, useValue: { user: () => ({ sub: 'cust-1', role: 'Customer' }), isAuthenticated: () => true, clearToken: jasmine.createSpy() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should render portal nav links', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('My Tickets');
    expect(el.textContent).toContain('Knowledge Base');
    expect(el.textContent).toContain('My Profile');
  });

  it('should toggle language via I18nService', () => {
    component.toggleLanguage();
    expect(i18nService.setLanguage).toHaveBeenCalledWith('ar');
  });

  it('should show hamburger menu toggle on mobile', () => {
    component.menuOpen.set(false);
    component.toggleMenu();
    expect(component.menuOpen()).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/portal-shell/portal-shell.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/portal-shell/portal-shell.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { I18nService } from '../services/i18n.service';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule],
  templateUrl: './portal-shell.component.html',
})
export class PortalShellComponent implements OnInit {
  readonly i18n = inject(I18nService);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);

  ngOnInit(): void {
    this.i18n.loadSavedLanguage();
  }

  toggleLanguage(): void {
    this.i18n.setLanguage(this.i18n.currentLang() === 'en' ? 'ar' : 'en');
  }

  toggleMenu(): void {
    this.menuOpen.update(v => !v);
  }

  logout(): void {
    this.authStore.clearToken();
    this.router.navigate(['/portal/login']);
  }
}
```

```html
<!-- src/app/portal/portal-shell/portal-shell.component.html -->

<div [attr.dir]="i18n.isRtl() ? 'rtl' : 'ltr'" class="min-h-screen flex flex-col">
  <!-- Header -->
  <mat-toolbar class="bg-white border-b shadow-sm px-6" color="primary">
    <a routerLink="/portal" class="text-xl font-bold text-blue-700 mr-8">Support Portal</a>

    <!-- Desktop Nav -->
    <nav class="hidden md:flex gap-6 flex-1">
      <a routerLink="/portal/tickets" routerLinkActive="text-blue-600 font-semibold" class="text-gray-700 hover:text-blue-600">
        My Tickets
      </a>
      <a routerLink="/portal/kb" routerLinkActive="text-blue-600 font-semibold" class="text-gray-700 hover:text-blue-600">
        Knowledge Base
      </a>
      <a routerLink="/portal/profile" routerLinkActive="text-blue-600 font-semibold" class="text-gray-700 hover:text-blue-600">
        My Profile
      </a>
    </nav>

    <div class="flex items-center gap-3 ml-auto">
      <button mat-stroked-button (click)="toggleLanguage()" class="text-sm">
        {{ i18n.currentLang() === 'en' ? 'عربي' : 'English' }}
      </button>
      <button mat-stroked-button (click)="logout()">Logout</button>
      <!-- Mobile hamburger -->
      <button mat-icon-button class="md:hidden" (click)="toggleMenu()">
        <mat-icon>{{ menuOpen() ? 'close' : 'menu' }}</mat-icon>
      </button>
    </div>
  </mat-toolbar>

  <!-- Mobile Nav -->
  @if (menuOpen()) {
    <div class="md:hidden bg-white border-b px-6 py-3 flex flex-col gap-3">
      <a routerLink="/portal/tickets" (click)="toggleMenu()">My Tickets</a>
      <a routerLink="/portal/kb" (click)="toggleMenu()">Knowledge Base</a>
      <a routerLink="/portal/profile" (click)="toggleMenu()">My Profile</a>
    </div>
  }

  <!-- Main Content -->
  <main class="flex-1 bg-gray-50">
    <router-outlet></router-outlet>
  </main>

  <!-- Footer -->
  <footer class="bg-white border-t px-6 py-4 text-sm text-gray-500 text-center">
    &copy; 2025 AZM Squad CRM Support Portal
  </footer>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/portal-shell/portal-shell.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/portal-shell/ src/app/portal/portal.routes.ts
git commit -m "feat(portal): implement PortalShellComponent with RTL language toggle (US-FE-032)"
```
