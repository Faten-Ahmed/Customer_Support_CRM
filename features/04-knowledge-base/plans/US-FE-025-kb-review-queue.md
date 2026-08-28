# KB Review Queue (Manager) — Implementation Plan

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

**Story:** US-FE-025
**Goal:** Add Approve and Reject actions to the KB article detail view, visible only to Manager/Admin, with a rejection note dialog.

**Architecture:** `KbArticleDetailComponent` (new standalone) renders the full article and conditionally shows Approve/Reject buttons based on `AuthStore` role and article status being `PendingReview`. Approve is a single-click confirmation inline; Reject opens a `MatDialog` requiring a note of ≥ 10 chars. Both call `KbService.approve/reject()` then navigate back to `/kb`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/kb/article-detail/kb-article-detail.component.ts` |
| Create | `src/app/kb/article-detail/kb-article-detail.component.html` |
| Create | `src/app/kb/article-detail/kb-article-detail.component.spec.ts` |
| Create | `src/app/kb/article-detail/reject-dialog.component.ts` |
| Create | `src/app/kb/article-detail/reject-dialog.component.spec.ts` |
| Modify | `src/app/kb/services/kb.service.ts` |
| Modify | `src/app/kb/services/kb.service.spec.ts` |

---

## Task 1: Add approve() and reject() to KbService

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/kb/services/kb.service.spec.ts

describe('KbService — approve/reject', () => {
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

  it('approve() should POST /api/v1/kb/articles/{id}/approve', () => {
    service.approve('art-1').subscribe();
    const req = httpMock.expectOne('/api/v1/kb/articles/art-1/approve');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'art-1', status: 'Published' });
  });

  it('reject() should POST /api/v1/kb/articles/{id}/reject with note', () => {
    service.reject('art-1', 'Needs more detail').subscribe();
    const req = httpMock.expectOne('/api/v1/kb/articles/art-1/reject');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ note: 'Needs more detail' });
    req.flush({ id: 'art-1', status: 'Draft' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/kb/services/kb.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/kb/services/kb.service.ts

approve(id: string): Observable<KbArticle> {
  return this.http.post<KbArticle>(`/api/v1/kb/articles/${id}/approve`, {});
}

reject(id: string, note: string): Observable<KbArticle> {
  return this.http.post<KbArticle>(`/api/v1/kb/articles/${id}/reject`, { note });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/kb/services/kb.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/kb/services/kb.service.ts src/app/kb/services/kb.service.spec.ts
git commit -m "feat(kb): add approve() and reject() to KbService (US-FE-025)"
```

---

## Task 2: KbArticleDetailComponent with Approve/Reject

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/kb/article-detail/kb-article-detail.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { KbArticleDetailComponent } from './kb-article-detail.component';
import { KbService, KbArticle } from '../services/kb.service';
import { AuthStore } from '../../auth/auth.store';

const mockArticle: KbArticle = {
  id: 'art-1', title: 'How to reset password', content: '# Reset\n\nFollow these steps.',
  visibility: 'Public', status: 'PendingReview', createdAt: '2025-01-01',
};

describe('KbArticleDetailComponent', () => {
  let fixture: ComponentFixture<KbArticleDetailComponent>;
  let component: KbArticleDetailComponent;
  let kbService: jasmine.SpyObj<KbService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let router: Router;

  beforeEach(async () => {
    kbService = jasmine.createSpyObj('KbService', ['getById', 'approve', 'reject']);
    dialog = jasmine.createSpyObj('MatDialog', ['open']);
    kbService.getById.and.returnValue(of(mockArticle));
    kbService.approve.and.returnValue(of({ ...mockArticle, status: 'Published' }));

    await TestBed.configureTestingModule({
      imports: [KbArticleDetailComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: KbService, useValue: kbService },
        { provide: MatDialog, useValue: dialog },
        { provide: ActivatedRoute, useValue: { params: of({ id: 'art-1' }) } },
        { provide: AuthStore, useValue: { user: () => ({ role: 'Manager' }) } },
        { provide: MatSnackBar, useValue: jasmine.createSpyObj('MatSnackBar', ['open']) },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(KbArticleDetailComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  it('should load article', () => {
    expect(component.article()?.title).toBe('How to reset password');
  });

  it('should show Approve and Reject buttons for Manager on PendingReview article', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Approve');
    expect(el.textContent).toContain('Reject');
  });

  it('should call approve() and navigate back', () => {
    component.approve();
    expect(kbService.approve).toHaveBeenCalledWith('art-1');
    expect(router.navigate).toHaveBeenCalledWith(['/kb']);
  });

  it('should not show approve/reject for Agent role', () => {
    (component as any).authStore = { user: () => ({ role: 'Agent' }) };
    fixture.detectChanges();
    expect(component.canReview).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/kb/article-detail/kb-article-detail.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/kb/article-detail/kb-article-detail.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { KbArticle, KbService } from '../services/kb.service';
import { AuthStore } from '../../auth/auth.store';
import { RejectDialogComponent } from './reject-dialog.component';

@Component({
  selector: 'app-kb-article-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatDialogModule, MatSnackBarModule, MatIconModule],
  templateUrl: './kb-article-detail.component.html',
})
export class KbArticleDetailComponent implements OnInit {
  private readonly kbService = inject(KbService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly authStore = inject(AuthStore);

  readonly article = signal<KbArticle | null>(null);

  get canReview(): boolean {
    const role = this.authStore.user()?.role;
    return (role === 'Manager' || role === 'Admin') && this.article()?.status === 'PendingReview';
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.kbService.getById(params['id']).subscribe(art => this.article.set(art));
    });
  }

  approve(): void {
    const id = this.article()?.id;
    if (!id) return;
    this.kbService.approve(id).subscribe(() => {
      this.snackBar.open('Article published', 'OK', { duration: 3000 });
      this.router.navigate(['/kb']);
    });
  }

  openRejectDialog(): void {
    const id = this.article()?.id;
    if (!id) return;
    const ref = this.dialog.open(RejectDialogComponent, { width: '400px', data: { articleId: id } });
    ref.afterClosed().subscribe(rejected => {
      if (rejected) this.router.navigate(['/kb']);
    });
  }
}
```

```html
<!-- src/app/kb/article-detail/kb-article-detail.component.html -->

@if (article()) {
  <div class="p-6 max-w-3xl mx-auto">
    <div class="flex items-start justify-between mb-6">
      <div>
        <h1 class="text-2xl font-semibold">{{ article()!.title }}</h1>
        <span class="text-sm text-gray-500">{{ article()!.visibility }} · {{ article()!.status }}</span>
      </div>
      @if (canReview) {
        <div class="flex gap-2">
          <button mat-raised-button color="primary" (click)="approve()">Approve</button>
          <button mat-stroked-button color="warn" (click)="openRejectDialog()">Reject</button>
        </div>
      }
    </div>
    <div class="prose max-w-none">
      <pre>{{ article()!.content }}</pre>
    </div>
  </div>
}
```

Implement `RejectDialogComponent`:
```typescript
// src/app/kb/article-detail/reject-dialog.component.ts

import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { KbService } from '../services/kb.service';

@Component({
  selector: 'app-reject-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSnackBarModule],
  template: `
    <h2 mat-dialog-title>Reject Article</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Rejection note (required, min 10 chars)</mat-label>
        <textarea matInput [formControl]="noteControl" rows="4"></textarea>
        @if (noteControl.hasError('minlength')) {
          <mat-error>Minimum 10 characters required</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="warn" [disabled]="noteControl.invalid" (click)="onReject()">Reject</button>
    </mat-dialog-actions>
  `,
})
export class RejectDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly kbService = inject(KbService);
  private readonly snackBar = inject(MatSnackBar);

  noteControl = this.fb.control('', [Validators.required, Validators.minLength(10)]);

  constructor(
    public dialogRef: MatDialogRef<RejectDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { articleId: string }
  ) {}

  onReject(): void {
    if (this.noteControl.invalid) return;
    this.kbService.reject(this.data.articleId, this.noteControl.value!).subscribe(() => {
      this.snackBar.open('Article rejected and returned to draft', 'OK', { duration: 3000 });
      this.dialogRef.close(true);
    });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/kb/article-detail/kb-article-detail.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/kb/article-detail/
git commit -m "feat(kb): implement KbArticleDetailComponent with Manager approve/reject flow (US-FE-025)"
```
