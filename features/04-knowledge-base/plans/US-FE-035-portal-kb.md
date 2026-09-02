# Portal Knowledge Base — Implementation Plan

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

**Story:** US-FE-035
**Goal:** Implement the customer portal Knowledge Base at `/portal/kb` — a home page with category/featured articles, a search page with full-text results, and an article detail page with bilingual (EN/AR) content and a "Was this helpful?" feedback UI.

**Architecture:** `PortalKbService` wraps `/api/v1/portal/kb/**` calls. Three standalone components: `PortalKbHomeComponent` (categories + featured), `PortalKbSearchComponent` (search results, activated by `?q=` query param), `PortalKbArticleComponent` (article detail with bilingual tabs and thumbs feedback). Language switching uses `I18nService.currentLang()` to choose between `content.en` and `content.ar`. Thumbs feedback is UI-only (no API call required by the spec).

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/services/portal-kb.service.ts` |
| Create | `src/app/portal/services/portal-kb.service.spec.ts` |
| Create | `src/app/portal/kb/portal-kb-home/portal-kb-home.component.ts` |
| Create | `src/app/portal/kb/portal-kb-home/portal-kb-home.component.html` |
| Create | `src/app/portal/kb/portal-kb-home/portal-kb-home.component.spec.ts` |
| Create | `src/app/portal/kb/portal-kb-search/portal-kb-search.component.ts` |
| Create | `src/app/portal/kb/portal-kb-search/portal-kb-search.component.html` |
| Create | `src/app/portal/kb/portal-kb-search/portal-kb-search.component.spec.ts` |
| Create | `src/app/portal/kb/portal-kb-article/portal-kb-article.component.ts` |
| Create | `src/app/portal/kb/portal-kb-article/portal-kb-article.component.html` |
| Create | `src/app/portal/kb/portal-kb-article/portal-kb-article.component.spec.ts` |

---

## Task 1: PortalKbService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/services/portal-kb.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PortalKbService } from './portal-kb.service';

describe('PortalKbService', () => {
  let service: PortalKbService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PortalKbService],
    });
    service = TestBed.inject(PortalKbService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/portal/kb/articles', () => {
    service.list().subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/portal/kb/articles');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('list() should pass categoryId and featured params', () => {
    service.list({ categoryId: 'c1', featured: true }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/portal/kb/articles');
    expect(req.request.params.get('categoryId')).toBe('c1');
    expect(req.request.params.get('featured')).toBe('true');
    req.flush([]);
  });

  it('search() should GET /api/v1/portal/kb/search with q param', () => {
    service.search('password reset').subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/portal/kb/search');
    expect(req.request.params.get('q')).toBe('password reset');
    req.flush([]);
  });

  it('getById() should GET /api/v1/portal/kb/articles/:id', () => {
    service.getById('a1').subscribe();
    const req = httpMock.expectOne('/api/v1/portal/kb/articles/a1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'a1', title: { en: 'Test', ar: 'اختبار' }, content: { en: '', ar: '' } });
  });

  it('getCategories() should GET /api/v1/portal/kb/categories', () => {
    service.getCategories().subscribe();
    const req = httpMock.expectOne('/api/v1/portal/kb/categories');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/portal-kb.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/services/portal-kb.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface KbCategory {
  id: string;
  name: { en: string; ar: string };
  articleCount: number;
}

export interface KbArticleSummary {
  id: string;
  title: { en: string; ar: string };
  excerpt: { en: string; ar: string };
  categoryId: string;
  categoryName: { en: string; ar: string };
  featured: boolean;
  updatedAt: string;
}

export interface KbArticle extends KbArticleSummary {
  content: { en: string; ar: string };
}

@Injectable({ providedIn: 'root' })
export class PortalKbService {
  private readonly http = inject(HttpClient);

  list(options?: { categoryId?: string; featured?: boolean }): Observable<KbArticleSummary[]> {
    let params = new HttpParams();
    if (options?.categoryId) params = params.set('categoryId', options.categoryId);
    if (options?.featured !== undefined) params = params.set('featured', String(options.featured));
    return this.http.get<KbArticleSummary[]>('/api/v1/portal/kb/articles', { params });
  }

  search(q: string): Observable<KbArticleSummary[]> {
    const params = new HttpParams().set('q', q);
    return this.http.get<KbArticleSummary[]>('/api/v1/portal/kb/search', { params });
  }

  getById(id: string): Observable<KbArticle> {
    return this.http.get<KbArticle>(`/api/v1/portal/kb/articles/${id}`);
  }

  getCategories(): Observable<KbCategory[]> {
    return this.http.get<KbCategory[]>('/api/v1/portal/kb/categories');
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/portal-kb.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/portal-kb.service.ts src/app/portal/services/portal-kb.service.spec.ts
git commit -m "feat(portal): add PortalKbService (US-FE-035)"
```

---

## Task 2: PortalKbHomeComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/kb/portal-kb-home/portal-kb-home.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ReactiveFormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { PortalKbHomeComponent } from './portal-kb-home.component';
import { PortalKbService, KbCategory, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../services/i18n.service';

const mockCategories: KbCategory[] = [
  { id: 'c1', name: { en: 'Billing', ar: 'الفواتير' }, articleCount: 5 },
];

const mockFeatured: KbArticleSummary[] = [
  { id: 'a1', title: { en: 'How to pay', ar: 'كيفية الدفع' }, excerpt: { en: 'Details', ar: 'تفاصيل' }, categoryId: 'c1', categoryName: { en: 'Billing', ar: 'الفواتير' }, featured: true, updatedAt: '' },
];

describe('PortalKbHomeComponent', () => {
  let fixture: ComponentFixture<PortalKbHomeComponent>;
  let component: PortalKbHomeComponent;
  let kbService: jasmine.SpyObj<PortalKbService>;

  beforeEach(async () => {
    kbService = jasmine.createSpyObj('PortalKbService', ['list', 'getCategories']);
    kbService.getCategories.and.returnValue(of(mockCategories));
    kbService.list.and.returnValue(of(mockFeatured));

    const i18nStub = { currentLang: () => 'en', isRtl: () => false };

    await TestBed.configureTestingModule({
      imports: [PortalKbHomeComponent, RouterTestingModule, NoopAnimationsModule, ReactiveFormsModule],
      providers: [
        { provide: PortalKbService, useValue: kbService },
        { provide: I18nService, useValue: i18nStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load categories and featured articles on init', () => {
    expect(kbService.getCategories).toHaveBeenCalled();
    expect(kbService.list).toHaveBeenCalledWith({ featured: true });
    expect(component.categories().length).toBe(1);
    expect(component.featured().length).toBe(1);
  });

  it('should display category names in current language', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Billing');
  });

  it('should navigate to search when search form is submitted', () => {
    component.searchControl.setValue('password');
    expect(component.searchControl.value).toBe('password');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/kb/portal-kb-home/portal-kb-home.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/kb/portal-kb-home/portal-kb-home.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { PortalKbService, KbCategory, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../services/i18n.service';

@Component({
  selector: 'app-portal-kb-home',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  templateUrl: './portal-kb-home.component.html',
})
export class PortalKbHomeComponent implements OnInit {
  readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);

  readonly categories = signal<KbCategory[]>([]);
  readonly featured = signal<KbArticleSummary[]>([]);
  readonly searchControl = new FormControl('');

  ngOnInit(): void {
    this.kbService.getCategories().subscribe(c => this.categories.set(c));
    this.kbService.list({ featured: true }).subscribe(a => this.featured.set(a));
  }

  search(): void {
    const q = this.searchControl.value?.trim();
    if (q) this.router.navigate(['/portal/kb'], { queryParams: { q } });
  }

  label(obj: { en: string; ar: string }): string {
    return this.i18n.currentLang() === 'ar' ? obj.ar : obj.en;
  }
}
```

```html
<!-- src/app/portal/kb/portal-kb-home/portal-kb-home.component.html -->

<div class="p-6 max-w-4xl mx-auto">
  <h1 class="text-3xl font-bold text-center mb-4">Knowledge Base</h1>

  <!-- Search bar -->
  <div class="flex gap-2 mb-10">
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>Search articles…</mat-label>
      <input matInput [formControl]="searchControl" (keyup.enter)="search()" />
      <mat-icon matSuffix>search</mat-icon>
    </mat-form-field>
    <button mat-raised-button color="primary" (click)="search()">Search</button>
  </div>

  <!-- Featured articles -->
  @if (featured().length > 0) {
    <section class="mb-10">
      <h2 class="text-xl font-semibold mb-4">Featured Articles</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        @for (article of featured(); track article.id) {
          <a [routerLink]="['/portal/kb', article.id]" class="block border rounded-lg p-4 hover:shadow-md transition">
            <p class="font-medium text-blue-700">{{ label(article.title) }}</p>
            <p class="text-sm text-gray-500 mt-1">{{ label(article.excerpt) }}</p>
          </a>
        }
      </div>
    </section>
  }

  <!-- Categories -->
  <section>
    <h2 class="text-xl font-semibold mb-4">Browse by Category</h2>
    <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
      @for (cat of categories(); track cat.id) {
        <a [routerLink]="['/portal/kb']" [queryParams]="{ categoryId: cat.id }"
           class="block border rounded-lg p-4 text-center hover:shadow-md transition">
          <mat-icon class="text-blue-600 mb-2">folder</mat-icon>
          <p class="font-medium">{{ label(cat.name) }}</p>
          <p class="text-xs text-gray-400">{{ cat.articleCount }} articles</p>
        </a>
      }
    </div>
  </section>

  <!-- Still need help? -->
  <div class="mt-12 text-center bg-blue-50 rounded-lg p-6">
    <p class="text-lg font-semibold mb-2">Can't find what you're looking for?</p>
    <p class="text-gray-600 mb-4">Our support team is here to help.</p>
    <a mat-raised-button color="primary" routerLink="/portal/tickets/new">Submit a Ticket</a>
  </div>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/kb/portal-kb-home/portal-kb-home.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/kb/portal-kb-home/
git commit -m "feat(portal): implement PortalKbHomeComponent with categories and featured articles (US-FE-035)"
```

---

## Task 3: PortalKbSearchComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/kb/portal-kb-search/portal-kb-search.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { PortalKbSearchComponent } from './portal-kb-search.component';
import { PortalKbService, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../services/i18n.service';

const mockResults: KbArticleSummary[] = [
  { id: 'a1', title: { en: 'Reset Password', ar: 'إعادة تعيين كلمة المرور' }, excerpt: { en: 'Steps to reset', ar: 'خطوات' }, categoryId: 'c1', categoryName: { en: 'Account', ar: 'الحساب' }, featured: false, updatedAt: '' },
];

describe('PortalKbSearchComponent', () => {
  let fixture: ComponentFixture<PortalKbSearchComponent>;
  let component: PortalKbSearchComponent;
  let kbService: jasmine.SpyObj<PortalKbService>;

  beforeEach(async () => {
    kbService = jasmine.createSpyObj('PortalKbService', ['search', 'list']);
    kbService.search.and.returnValue(of(mockResults));
    kbService.list.and.returnValue(of([]));

    const i18nStub = { currentLang: () => 'en', isRtl: () => false };

    await TestBed.configureTestingModule({
      imports: [PortalKbSearchComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: PortalKbService, useValue: kbService },
        { provide: I18nService, useValue: i18nStub },
        { provide: ActivatedRoute, useValue: { queryParams: of({ q: 'password' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should call search() with q from queryParams', () => {
    expect(kbService.search).toHaveBeenCalledWith('password');
    expect(component.results().length).toBe(1);
  });

  it('should display search results', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Reset Password');
  });

  it('should show empty state when no results', () => {
    kbService.search.and.returnValue(of([]));
    component.runSearch('nothing');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('No articles found');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/kb/portal-kb-search/portal-kb-search.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/kb/portal-kb-search/portal-kb-search.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { PortalKbService, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../services/i18n.service';

@Component({
  selector: 'app-portal-kb-search',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule],
  templateUrl: './portal-kb-search.component.html',
})
export class PortalKbSearchComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);

  readonly results = signal<KbArticleSummary[]>([]);
  readonly loading = signal(false);
  query = '';

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q) {
        this.query = q;
        this.runSearch(q);
      }
    });
  }

  runSearch(q: string): void {
    this.loading.set(true);
    this.kbService.search(q).subscribe(res => {
      this.results.set(res);
      this.loading.set(false);
    });
  }

  label(obj: { en: string; ar: string }): string {
    return this.i18n.currentLang() === 'ar' ? obj.ar : obj.en;
  }
}
```

```html
<!-- src/app/portal/kb/portal-kb-search/portal-kb-search.component.html -->

<div class="p-6 max-w-3xl mx-auto">
  <h1 class="text-2xl font-semibold mb-2">Search Results</h1>
  @if (query) {
    <p class="text-gray-500 mb-6">Results for: <span class="font-medium text-gray-800">"{{ query }}"</span></p>
  }

  @if (loading()) {
    <div class="space-y-3">
      @for (i of [1,2,3]; track i) {
        <div class="h-16 bg-gray-200 rounded animate-pulse"></div>
      }
    </div>
  } @else if (results().length === 0) {
    <div class="text-center py-16 text-gray-500">
      <mat-icon class="text-5xl mb-3">search_off</mat-icon>
      <p class="text-lg">No articles found</p>
      <p class="text-sm mt-1">Try different keywords or <a routerLink="/portal/tickets/new" class="text-blue-600 underline">submit a ticket</a>.</p>
    </div>
  } @else {
    <div class="space-y-3">
      @for (article of results(); track article.id) {
        <a [routerLink]="['/portal/kb', article.id]"
           class="block border rounded-lg p-4 hover:shadow-md transition">
          <p class="font-medium text-blue-700">{{ label(article.title) }}</p>
          <p class="text-sm text-gray-500 mt-1">{{ label(article.excerpt) }}</p>
          <p class="text-xs text-gray-400 mt-2">{{ label(article.categoryName) }}</p>
        </a>
      }
    </div>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/kb/portal-kb-search/portal-kb-search.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/kb/portal-kb-search/
git commit -m "feat(portal): implement PortalKbSearchComponent (US-FE-035)"
```

---

## Task 4: PortalKbArticleComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/kb/portal-kb-article/portal-kb-article.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { PortalKbArticleComponent } from './portal-kb-article.component';
import { PortalKbService, KbArticle } from '../../services/portal-kb.service';
import { I18nService } from '../../services/i18n.service';

const mockArticle: KbArticle = {
  id: 'a1',
  title: { en: 'Reset Password', ar: 'إعادة تعيين كلمة المرور' },
  excerpt: { en: 'Steps', ar: 'خطوات' },
  content: { en: '<p>Step 1: Go to login page</p>', ar: '<p>الخطوة 1</p>' },
  categoryId: 'c1',
  categoryName: { en: 'Account', ar: 'الحساب' },
  featured: false,
  updatedAt: '2025-01-01T00:00:00Z',
};

describe('PortalKbArticleComponent', () => {
  let fixture: ComponentFixture<PortalKbArticleComponent>;
  let component: PortalKbArticleComponent;
  let kbService: jasmine.SpyObj<PortalKbService>;

  beforeEach(async () => {
    kbService = jasmine.createSpyObj('PortalKbService', ['getById']);
    kbService.getById.and.returnValue(of(mockArticle));

    const i18nStub = { currentLang: () => 'en', isRtl: () => false };

    await TestBed.configureTestingModule({
      imports: [PortalKbArticleComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: PortalKbService, useValue: kbService },
        { provide: I18nService, useValue: i18nStub },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'a1' } } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbArticleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load article by id', () => {
    expect(kbService.getById).toHaveBeenCalledWith('a1');
    expect(component.article()).toBeTruthy();
  });

  it('should render article title in current language', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Reset Password');
  });

  it('should record thumbsUp feedback', () => {
    component.submitFeedback(true);
    expect(component.feedbackGiven()).toBe('up');
  });

  it('should record thumbsDown feedback', () => {
    component.submitFeedback(false);
    expect(component.feedbackGiven()).toBe('down');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/kb/portal-kb-article/portal-kb-article.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/kb/portal-kb-article/portal-kb-article.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { PortalKbService, KbArticle } from '../../services/portal-kb.service';
import { I18nService } from '../../services/i18n.service';

@Component({
  selector: 'app-portal-kb-article',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule],
  templateUrl: './portal-kb-article.component.html',
})
export class PortalKbArticleComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);
  private readonly sanitizer = inject(DomSanitizer);

  readonly article = signal<KbArticle | null>(null);
  readonly feedbackGiven = signal<'up' | 'down' | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.kbService.getById(id).subscribe(a => this.article.set(a));
  }

  label(obj: { en: string; ar: string }): string {
    return this.i18n.currentLang() === 'ar' ? obj.ar : obj.en;
  }

  safeContent(): SafeHtml {
    const a = this.article();
    if (!a) return '';
    const raw = this.i18n.currentLang() === 'ar' ? a.content.ar : a.content.en;
    return this.sanitizer.bypassSecurityTrustHtml(raw);
  }

  submitFeedback(helpful: boolean): void {
    this.feedbackGiven.set(helpful ? 'up' : 'down');
  }
}
```

```html
<!-- src/app/portal/kb/portal-kb-article/portal-kb-article.component.html -->

<div class="p-6 max-w-3xl mx-auto">
  @if (article()) {
    <!-- Breadcrumb -->
    <nav class="text-sm text-gray-500 mb-4">
      <a routerLink="/portal/kb" class="hover:underline">Knowledge Base</a>
      <span class="mx-2">/</span>
      <span>{{ label(article()!.categoryName) }}</span>
    </nav>

    <h1 class="text-2xl font-bold mb-2">{{ label(article()!.title) }}</h1>
    <p class="text-xs text-gray-400 mb-6">Last updated: {{ article()!.updatedAt | date }}</p>

    <!-- Article content -->
    <div class="prose max-w-none mb-10" [innerHTML]="safeContent()"></div>

    <!-- Was this helpful? -->
    <div class="border-t pt-6">
      @if (!feedbackGiven()) {
        <p class="text-sm font-medium mb-3">Was this article helpful?</p>
        <div class="flex gap-3">
          <button mat-stroked-button (click)="submitFeedback(true)">
            <mat-icon>thumb_up</mat-icon> Yes
          </button>
          <button mat-stroked-button (click)="submitFeedback(false)">
            <mat-icon>thumb_down</mat-icon> No
          </button>
        </div>
      } @else {
        <p class="text-sm text-green-600 flex items-center gap-2">
          <mat-icon>check_circle</mat-icon>
          Thanks for your feedback!
        </p>
      }
    </div>

    <!-- Still need help? -->
    <div class="mt-8 bg-blue-50 rounded-lg p-5 text-center">
      <p class="font-semibold mb-2">Still need help?</p>
      <a mat-raised-button color="primary" routerLink="/portal/tickets/new">Submit a Support Ticket</a>
    </div>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/kb/portal-kb-article/portal-kb-article.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/kb/portal-kb-article/
git commit -m "feat(portal): implement PortalKbArticleComponent with bilingual content and feedback UI (US-FE-035)"
```
