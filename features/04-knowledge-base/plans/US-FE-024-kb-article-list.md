# KB Article List & Editor (Agent) — Implementation Plan

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

**Story:** US-FE-024
**Goal:** Implement the Knowledge Base agent interface — `/kb` article list with status/category/search filtering, and `/kb/articles/new` and `/kb/articles/{id}/edit` with a bilingual Markdown editor, Save Draft, and Submit for Review actions.

**Architecture:** `KbArticleListComponent` is a standalone table page. `KbArticleEditorComponent` is a standalone form page with two tab panels (EN/AR) for bilingual content. Both lazy-loaded under `/kb`. `KbService` wraps all API calls. The Markdown editor uses Angular Material textarea for MVP (Monaco integration is flagged as enhancement).

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/kb/services/kb.service.ts` |
| Create | `src/app/kb/services/kb.service.spec.ts` |
| Create | `src/app/kb/article-list/kb-article-list.component.ts` |
| Create | `src/app/kb/article-list/kb-article-list.component.html` |
| Create | `src/app/kb/article-list/kb-article-list.component.spec.ts` |
| Create | `src/app/kb/article-editor/kb-article-editor.component.ts` |
| Create | `src/app/kb/article-editor/kb-article-editor.component.html` |
| Create | `src/app/kb/article-editor/kb-article-editor.component.spec.ts` |
| Create | `src/app/kb/kb.routes.ts` |

---

## Task 1: KbService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/kb/services/kb.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { KbService } from './kb.service';

describe('KbService', () => {
  let service: KbService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [KbService],
    });
    service = TestBed.inject(KbService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/kb/articles with params', () => {
    service.list({ page: 1, pageSize: 20, status: 'Draft' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/kb/articles');
    expect(req.request.params.get('status')).toBe('Draft');
    req.flush({ data: [], total: 0 });
  });

  it('create() should POST /api/v1/kb/articles', () => {
    service.create({ title: 'How to reset', content: '# Step 1', categoryId: 'c1', visibility: 'Public' }).subscribe();
    const req = httpMock.expectOne('/api/v1/kb/articles');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'art-1', status: 'Draft' });
  });

  it('update() should PATCH /api/v1/kb/articles/{id}', () => {
    service.update('art-1', { title: 'Updated' }).subscribe();
    const req = httpMock.expectOne('/api/v1/kb/articles/art-1');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: 'art-1' });
  });

  it('submitForReview() should POST /api/v1/kb/articles/{id}/submit', () => {
    service.submitForReview('art-1').subscribe();
    const req = httpMock.expectOne('/api/v1/kb/articles/art-1/submit');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'art-1', status: 'PendingReview' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/kb/services/kb.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/kb/services/kb.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type KbStatus = 'Draft' | 'PendingReview' | 'Published' | 'Archived';
export type KbVisibility = 'Public' | 'Internal' | 'Private';

export interface KbArticle {
  id: string;
  title: string;
  titleAr?: string;
  content: string;
  contentAr?: string;
  categoryId?: string;
  categoryName?: string;
  visibility: KbVisibility;
  status: KbStatus;
  authorName?: string;
  publishedAt?: string;
  createdAt: string;
}

export interface KbListQuery {
  page: number;
  pageSize: number;
  status?: KbStatus;
  categoryId?: string;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class KbService {
  private readonly http = inject(HttpClient);

  list(query: KbListQuery): Observable<{ data: KbArticle[]; total: number }> {
    let params = new HttpParams().set('page', String(query.page)).set('pageSize', String(query.pageSize));
    if (query.status) params = params.set('status', query.status);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.search) params = params.set('search', query.search);
    return this.http.get<{ data: KbArticle[]; total: number }>('/api/v1/kb/articles', { params });
  }

  getById(id: string): Observable<KbArticle> {
    return this.http.get<KbArticle>(`/api/v1/kb/articles/${id}`);
  }

  create(payload: Partial<KbArticle>): Observable<KbArticle> {
    return this.http.post<KbArticle>('/api/v1/kb/articles', payload);
  }

  update(id: string, changes: Partial<KbArticle>): Observable<KbArticle> {
    return this.http.patch<KbArticle>(`/api/v1/kb/articles/${id}`, changes);
  }

  submitForReview(id: string): Observable<KbArticle> {
    return this.http.post<KbArticle>(`/api/v1/kb/articles/${id}/submit`, {});
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/kb/services/kb.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/kb/services/
git commit -m "feat(kb): add KbService with list/create/update/submitForReview (US-FE-024)"
```

---

## Task 2: KbArticleListComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/kb/article-list/kb-article-list.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { KbArticleListComponent } from './kb-article-list.component';
import { KbService, KbArticle } from '../services/kb.service';

const mockArticles: KbArticle[] = [
  { id: 'a1', title: 'Reset Password', content: '...', visibility: 'Public', status: 'Published', createdAt: '2025-01-01' },
  { id: 'a2', title: 'Billing FAQ', content: '...', visibility: 'Internal', status: 'Draft', createdAt: '2025-01-02' },
];

describe('KbArticleListComponent', () => {
  let fixture: ComponentFixture<KbArticleListComponent>;
  let component: KbArticleListComponent;
  let kbService: jasmine.SpyObj<KbService>;

  beforeEach(async () => {
    kbService = jasmine.createSpyObj('KbService', ['list']);
    kbService.list.and.returnValue(of({ data: mockArticles, total: 2 }));

    await TestBed.configureTestingModule({
      imports: [KbArticleListComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: KbService, useValue: kbService }],
    }).compileComponents();

    fixture = TestBed.createComponent(KbArticleListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load articles', () => {
    expect(component).toBeTruthy();
    expect(component.articles().length).toBe(2);
  });

  it('should filter by status', () => {
    component.statusFilter.setValue('Draft');
    expect(kbService.list).toHaveBeenCalledWith(jasmine.objectContaining({ status: 'Draft' }));
  });

  it('should show colour-coded status badges', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Published');
    expect(el.textContent).toContain('Draft');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/kb/article-list/kb-article-list.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/kb/article-list/kb-article-list.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { KbArticle, KbService, KbStatus } from '../services/kb.service';

const STATUS_CLASSES: Record<KbStatus, string> = {
  Draft: 'badge-grey',
  PendingReview: 'badge-orange',
  Published: 'badge-green',
  Archived: 'badge-dark',
};

@Component({
  selector: 'app-kb-article-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule],
  templateUrl: './kb-article-list.component.html',
})
export class KbArticleListComponent implements OnInit {
  private readonly kbService = inject(KbService);

  readonly articles = signal<KbArticle[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);

  readonly searchControl = new FormControl('');
  readonly statusFilter = new FormControl<KbStatus | ''>('');

  displayedColumns = ['title', 'category', 'status', 'visibility', 'author', 'publishedAt'];

  ngOnInit(): void {
    this.loadArticles();
    this.searchControl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => this.loadArticles());
    this.statusFilter.valueChanges.subscribe(() => this.loadArticles());
  }

  loadArticles(): void {
    this.loading.set(true);
    this.kbService.list({
      page: 1,
      pageSize: 50,
      status: (this.statusFilter.value as KbStatus) || undefined,
      search: this.searchControl.value || undefined,
    }).subscribe({
      next: res => {
        this.articles.set(res.data);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  badgeClass(status: KbStatus): string {
    return STATUS_CLASSES[status] ?? 'badge-grey';
  }
}
```

```html
<!-- src/app/kb/article-list/kb-article-list.component.html -->

<div class="p-6">
  <div class="flex items-center justify-between mb-4">
    <h1 class="text-2xl font-semibold">Knowledge Base</h1>
    <button mat-raised-button color="primary" routerLink="/kb/articles/new">
      <mat-icon>add</mat-icon> New Article
    </button>
  </div>

  <div class="flex gap-3 mb-4">
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>Search</mat-label>
      <input matInput [formControl]="searchControl" />
    </mat-form-field>
    <mat-form-field appearance="outline" class="w-48">
      <mat-label>Status</mat-label>
      <mat-select [formControl]="statusFilter">
        <mat-option value="">All</mat-option>
        <mat-option value="Draft">Draft</mat-option>
        <mat-option value="PendingReview">Pending Review</mat-option>
        <mat-option value="Published">Published</mat-option>
        <mat-option value="Archived">Archived</mat-option>
      </mat-select>
    </mat-form-field>
  </div>

  <mat-table [dataSource]="articles()" class="w-full">
    <ng-container matColumnDef="title">
      <mat-header-cell *matHeaderCellDef>Title</mat-header-cell>
      <mat-cell *matCellDef="let a">
        <a [routerLink]="['/kb/articles', a.id, 'edit']" class="text-blue-600 hover:underline">{{ a.title }}</a>
      </mat-cell>
    </ng-container>
    <ng-container matColumnDef="category">
      <mat-header-cell *matHeaderCellDef>Category</mat-header-cell>
      <mat-cell *matCellDef="let a">{{ a.categoryName || '—' }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="status">
      <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
      <mat-cell *matCellDef="let a"><span [class]="badgeClass(a.status)">{{ a.status }}</span></mat-cell>
    </ng-container>
    <ng-container matColumnDef="visibility">
      <mat-header-cell *matHeaderCellDef>Visibility</mat-header-cell>
      <mat-cell *matCellDef="let a">{{ a.visibility }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="author">
      <mat-header-cell *matHeaderCellDef>Author</mat-header-cell>
      <mat-cell *matCellDef="let a">{{ a.authorName || '—' }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="publishedAt">
      <mat-header-cell *matHeaderCellDef>Published</mat-header-cell>
      <mat-cell *matCellDef="let a">{{ a.publishedAt ? (a.publishedAt | date:'mediumDate') : '—' }}</mat-cell>
    </ng-container>
    <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
    <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
  </mat-table>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/kb/article-list/kb-article-list.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/kb/article-list/
git commit -m "feat(kb): implement KbArticleListComponent (US-FE-024)"
```

---

## Task 3: KbArticleEditorComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/kb/article-editor/kb-article-editor.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { KbArticleEditorComponent } from './kb-article-editor.component';
import { KbService, KbArticle } from '../services/kb.service';
import { MatSnackBar } from '@angular/material/snack-bar';

describe('KbArticleEditorComponent', () => {
  let fixture: ComponentFixture<KbArticleEditorComponent>;
  let component: KbArticleEditorComponent;
  let kbService: jasmine.SpyObj<KbService>;
  let router: Router;

  beforeEach(async () => {
    kbService = jasmine.createSpyObj('KbService', ['create', 'update', 'submitForReview', 'getById']);
    kbService.create.and.returnValue(of({ id: 'art-new', title: 'T', content: 'C', visibility: 'Public', status: 'Draft', createdAt: '' }));
    kbService.update.and.returnValue(of({ id: 'art-1', title: 'T', content: 'C', visibility: 'Public', status: 'Draft', createdAt: '' }));
    kbService.submitForReview.and.returnValue(of({ id: 'art-1', status: 'PendingReview' } as KbArticle));

    await TestBed.configureTestingModule({
      imports: [KbArticleEditorComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: KbService, useValue: kbService },
        { provide: ActivatedRoute, useValue: { params: of({}) } },
        { provide: MatSnackBar, useValue: jasmine.createSpyObj('MatSnackBar', ['open']) },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(KbArticleEditorComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create in new mode', () => {
    expect(component).toBeTruthy();
    expect(component.isEditMode).toBeFalse();
  });

  it('should call create() on save draft', () => {
    component.form.patchValue({ title: 'My Article', content: 'Content here', visibility: 'Public' });
    component.saveDraft();
    expect(kbService.create).toHaveBeenCalled();
  });

  it('should call submitForReview() after creating draft', () => {
    component.articleId = 'art-1';
    component.form.patchValue({ title: 'My Article', content: 'Content here', visibility: 'Public' });
    component.submitForReview();
    expect(kbService.update).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/kb/article-editor/kb-article-editor.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/kb/article-editor/kb-article-editor.component.ts

import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';
import { KbService } from '../services/kb.service';

@Component({
  selector: 'app-kb-article-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatTabsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatSnackBarModule],
  templateUrl: './kb-article-editor.component.html',
})
export class KbArticleEditorComponent implements OnInit {
  private readonly kbService = inject(KbService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  articleId: string | null = null;
  isEditMode = false;

  form = this.fb.group({
    title: ['', Validators.required],
    titleAr: [''],
    content: ['', Validators.required],
    contentAr: [''],
    categoryId: [''],
    visibility: ['Public', Validators.required],
  });

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.articleId = params['id'];
        this.isEditMode = true;
        this.kbService.getById(this.articleId!).subscribe(art => this.form.patchValue(art));
      }
    });
  }

  saveDraft(): void {
    const val = this.form.value as any;
    if (this.articleId) {
      this.kbService.update(this.articleId, val).subscribe(() => {
        this.snackBar.open('Draft saved', 'OK', { duration: 2000 });
      });
    } else {
      this.kbService.create(val).subscribe(art => {
        this.articleId = art.id;
        this.isEditMode = true;
        this.snackBar.open('Draft saved', 'OK', { duration: 2000 });
        this.router.navigate(['/kb/articles', art.id, 'edit']);
      });
    }
  }

  submitForReview(): void {
    const val = this.form.value as any;
    const save$ = this.articleId
      ? this.kbService.update(this.articleId, val)
      : this.kbService.create(val);

    save$.subscribe(art => {
      this.articleId = art.id;
      this.kbService.submitForReview(this.articleId!).subscribe(() => {
        this.snackBar.open('Submitted for review', 'OK', { duration: 3000 });
        this.router.navigate(['/kb']);
      });
    });
  }
}
```

```html
<!-- src/app/kb/article-editor/kb-article-editor.component.html -->

<div class="p-6 max-w-4xl mx-auto">
  <h1 class="text-2xl font-semibold mb-6">{{ isEditMode ? 'Edit Article' : 'New Article' }}</h1>

  <form [formGroup]="form" class="flex flex-col gap-4">
    <div class="grid grid-cols-2 gap-4">
      <mat-form-field appearance="outline">
        <mat-label>Title (EN)</mat-label>
        <input matInput formControlName="title" />
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Title (AR)</mat-label>
        <input matInput formControlName="titleAr" dir="rtl" />
      </mat-form-field>
    </div>

    <mat-form-field appearance="outline">
      <mat-label>Visibility</mat-label>
      <mat-select formControlName="visibility">
        <mat-option value="Public">Public</mat-option>
        <mat-option value="Internal">Internal (Staff Only)</mat-option>
        <mat-option value="Private">Private</mat-option>
      </mat-select>
    </mat-form-field>

    <mat-tab-group>
      <mat-tab label="English Content">
        <div class="pt-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Content (Markdown)</mat-label>
            <textarea matInput formControlName="content" rows="15" class="font-mono"></textarea>
          </mat-form-field>
        </div>
      </mat-tab>
      <mat-tab label="Arabic Content (اللغة العربية)">
        <div class="pt-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Content AR (Markdown)</mat-label>
            <textarea matInput formControlName="contentAr" rows="15" dir="rtl" class="font-mono"></textarea>
          </mat-form-field>
        </div>
      </mat-tab>
    </mat-tab-group>

    <div class="flex gap-3">
      <button mat-stroked-button type="button" (click)="saveDraft()" [disabled]="form.invalid">Save Draft</button>
      <button mat-raised-button color="primary" type="button" (click)="submitForReview()" [disabled]="form.invalid">Submit for Review</button>
      <button mat-button type="button" routerLink="/kb">Cancel</button>
    </div>
  </form>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/kb/article-editor/kb-article-editor.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/kb/article-editor/ src/app/kb/kb.routes.ts
git commit -m "feat(kb): implement KbArticleEditorComponent with bilingual tabs (US-FE-024)"
```
