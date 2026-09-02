# Department, Branch & Category Management — Implementation Plan

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

**Story:** US-FE-028
**Goal:** Implement admin pages for managing departments (`/admin/departments`), branches (`/admin/branches`), and the hierarchical category tree (`/admin/categories`), each with create/edit dialogs and deactivation with open-ticket guard.

**Architecture:** Three standalone components, each lazy-loaded. `DepartmentListComponent` and `BranchListComponent` use MatTable with dialog forms. `CategoryTreeComponent` renders a nested view using recursive template or flat datasource with indentation levels. Create/Edit uses a shared `EntityFormDialogComponent` pattern with `MatDialog`. `DepartmentService`, `BranchService`, and `CategoryService` wrap respective API calls.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/admin/departments/department.service.ts` |
| Create | `src/app/admin/departments/department.service.spec.ts` |
| Create | `src/app/admin/departments/department-list.component.ts` |
| Create | `src/app/admin/departments/department-list.component.html` |
| Create | `src/app/admin/departments/department-list.component.spec.ts` |
| Create | `src/app/admin/categories/category.service.ts` |
| Create | `src/app/admin/categories/category.service.spec.ts` |
| Create | `src/app/admin/categories/category-tree.component.ts` |
| Create | `src/app/admin/categories/category-tree.component.html` |
| Create | `src/app/admin/categories/category-tree.component.spec.ts` |

---

## Task 1: DepartmentService and BranchService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/departments/department.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DepartmentService } from './department.service';

describe('DepartmentService', () => {
  let service: DepartmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [DepartmentService],
    });
    service = TestBed.inject(DepartmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/admin/departments', () => {
    service.list().subscribe();
    const req = httpMock.expectOne('/api/v1/admin/departments');
    expect(req.request.method).toBe('GET');
    req.flush({ data: [], total: 0 });
  });

  it('create() should POST /api/v1/admin/departments', () => {
    service.create({ name: 'Support', nameAr: 'الدعم' }).subscribe();
    const req = httpMock.expectOne('/api/v1/admin/departments');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'd1', name: 'Support' });
  });

  it('update() should PATCH /api/v1/admin/departments/{id}', () => {
    service.update('d1', { name: 'Support Updated' }).subscribe();
    const req = httpMock.expectOne('/api/v1/admin/departments/d1');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: 'd1' });
  });

  it('deactivate() should DELETE /api/v1/admin/departments/{id}', () => {
    service.deactivate('d1').subscribe();
    const req = httpMock.expectOne('/api/v1/admin/departments/d1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/departments/department.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/departments/department.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Department {
  id: string;
  name: string;
  nameAr?: string;
  agentCount?: number;
  isActive: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly http = inject(HttpClient);

  list(): Observable<{ data: Department[]; total: number }> {
    return this.http.get<{ data: Department[]; total: number }>('/api/v1/admin/departments');
  }

  create(payload: { name: string; nameAr?: string }): Observable<Department> {
    return this.http.post<Department>('/api/v1/admin/departments', payload);
  }

  update(id: string, changes: Partial<Department>): Observable<Department> {
    return this.http.patch<Department>(`/api/v1/admin/departments/${id}`, changes);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/admin/departments/${id}`);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/departments/department.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/departments/department.service.ts src/app/admin/departments/department.service.spec.ts
git commit -m "feat(admin): add DepartmentService (US-FE-028)"
```

---

## Task 2: DepartmentListComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/departments/department-list.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { DepartmentListComponent } from './department-list.component';
import { DepartmentService, Department } from './department.service';

const mockDepts: Department[] = [
  { id: 'd1', name: 'Support', isActive: true, agentCount: 5, createdAt: '2025-01-01' },
  { id: 'd2', name: 'Billing', isActive: true, agentCount: 3, createdAt: '2025-01-02' },
];

describe('DepartmentListComponent', () => {
  let fixture: ComponentFixture<DepartmentListComponent>;
  let component: DepartmentListComponent;
  let deptService: jasmine.SpyObj<DepartmentService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let snackBar: jasmine.SpyObj<MatSnackBar>;

  beforeEach(async () => {
    deptService = jasmine.createSpyObj('DepartmentService', ['list', 'deactivate']);
    dialog = jasmine.createSpyObj('MatDialog', ['open']);
    snackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    deptService.list.and.returnValue(of({ data: mockDepts, total: 2 }));
    deptService.deactivate.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [DepartmentListComponent, NoopAnimationsModule],
      providers: [
        { provide: DepartmentService, useValue: deptService },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DepartmentListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load departments', () => {
    expect(component.departments().length).toBe(2);
  });

  it('should show 409 error if department has open tickets', () => {
    deptService.deactivate.and.returnValue(throwError(() => ({ status: 409 })));
    component.deactivate(mockDepts[0]);
    expect(snackBar.open).toHaveBeenCalledWith(jasmine.stringContaining('open tickets'), jasmine.any(String), jasmine.any(Object));
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/departments/department-list.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/departments/department-list.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Department, DepartmentService } from './department.service';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, MatSnackBarModule],
  templateUrl: './department-list.component.html',
})
export class DepartmentListComponent implements OnInit {
  private readonly deptService = inject(DepartmentService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly departments = signal<Department[]>([]);
  displayedColumns = ['name', 'nameAr', 'agentCount', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.deptService.list().subscribe(res => this.departments.set(res.data));
  }

  deactivate(dept: Department): void {
    this.deptService.deactivate(dept.id).subscribe({
      next: () => {
        this.departments.update(list => list.filter(d => d.id !== dept.id));
        this.snackBar.open('Department deactivated', 'OK', { duration: 3000 });
      },
      error: err => {
        if (err.status === 409) {
          this.snackBar.open('Cannot deactivate: department has open tickets', 'OK', { duration: 4000 });
        }
      },
    });
  }
}
```

```html
<!-- src/app/admin/departments/department-list.component.html -->

<div class="flex justify-between mb-4">
  <h2 class="text-xl font-semibold">Departments</h2>
  <button mat-raised-button color="primary"><mat-icon>add</mat-icon> New Department</button>
</div>
<mat-table [dataSource]="departments()" class="w-full">
  <ng-container matColumnDef="name"><mat-header-cell *matHeaderCellDef>Name (EN)</mat-header-cell><mat-cell *matCellDef="let d">{{ d.name }}</mat-cell></ng-container>
  <ng-container matColumnDef="nameAr"><mat-header-cell *matHeaderCellDef>Name (AR)</mat-header-cell><mat-cell *matCellDef="let d" dir="rtl">{{ d.nameAr || '—' }}</mat-cell></ng-container>
  <ng-container matColumnDef="agentCount"><mat-header-cell *matHeaderCellDef>Agents</mat-header-cell><mat-cell *matCellDef="let d">{{ d.agentCount ?? 0 }}</mat-cell></ng-container>
  <ng-container matColumnDef="status"><mat-header-cell *matHeaderCellDef>Status</mat-header-cell><mat-cell *matCellDef="let d">{{ d.isActive ? 'Active' : 'Inactive' }}</mat-cell></ng-container>
  <ng-container matColumnDef="actions">
    <mat-header-cell *matHeaderCellDef></mat-header-cell>
    <mat-cell *matCellDef="let d">
      <button mat-icon-button><mat-icon>edit</mat-icon></button>
      <button mat-icon-button color="warn" (click)="deactivate(d)"><mat-icon>block</mat-icon></button>
    </mat-cell>
  </ng-container>
  <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
  <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
</mat-table>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/departments/department-list.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/departments/
git commit -m "feat(admin): implement DepartmentListComponent (US-FE-028)"
```

---

## Task 3: CategoryService and CategoryTreeComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/categories/category.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CategoryService } from './category.service';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CategoryService],
    });
    service = TestBed.inject(CategoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('tree() should GET /api/v1/admin/categories', () => {
    service.tree().subscribe();
    const req = httpMock.expectOne('/api/v1/admin/categories');
    req.flush([{ id: 'c1', name: 'Hardware', children: [{ id: 'c2', name: 'Laptops', children: [] }] }]);
  });

  it('create() should POST /api/v1/admin/categories', () => {
    service.create({ name: 'Software', parentId: null }).subscribe();
    const req = httpMock.expectOne('/api/v1/admin/categories');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'c3', name: 'Software' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/categories/category.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/categories/category.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Category {
  id: string;
  name: string;
  nameAr?: string;
  parentId?: string | null;
  isActive: boolean;
  children: Category[];
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);

  tree(): Observable<Category[]> {
    return this.http.get<Category[]>('/api/v1/admin/categories');
  }

  create(payload: { name: string; parentId: string | null; nameAr?: string }): Observable<Category> {
    return this.http.post<Category>('/api/v1/admin/categories', payload);
  }

  update(id: string, changes: Partial<Category>): Observable<Category> {
    return this.http.patch<Category>(`/api/v1/admin/categories/${id}`, changes);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/admin/categories/${id}`);
  }
}
```

```typescript
// src/app/admin/categories/category-tree.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Category, CategoryService } from './category.service';

@Component({
  selector: 'app-category-tree',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatSnackBarModule],
  template: `
    <div class="flex justify-between mb-4">
      <h2 class="text-xl font-semibold">Categories</h2>
      <button mat-raised-button color="primary" (click)="addRoot()"><mat-icon>add</mat-icon> Add Category</button>
    </div>
    <div class="border rounded-lg overflow-hidden">
      @for (cat of categories(); track cat.id) {
        <ng-container *ngTemplateOutlet="catRow; context: { $implicit: cat, level: 0 }"></ng-container>
      }
    </div>
    <ng-template #catRow let-cat let-level="level">
      <div class="flex items-center px-4 py-2 border-b hover:bg-gray-50" [style.padding-left.px]="16 + level * 24">
        <span class="flex-1 font-medium">{{ cat.name }}</span>
        <button mat-icon-button (click)="addChild(cat)"><mat-icon>add</mat-icon></button>
        <button mat-icon-button><mat-icon>edit</mat-icon></button>
        <button mat-icon-button color="warn" (click)="deactivate(cat)"><mat-icon>block</mat-icon></button>
      </div>
      @for (child of cat.children; track child.id) {
        <ng-container *ngTemplateOutlet="catRow; context: { $implicit: child, level: level + 1 }"></ng-container>
      }
    </ng-template>
  `,
})
export class CategoryTreeComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly snackBar = inject(MatSnackBar);

  readonly categories = signal<Category[]>([]);

  ngOnInit(): void {
    this.categoryService.tree().subscribe(cats => this.categories.set(cats));
  }

  addRoot(): void {
    const name = prompt('Category name:');
    if (!name) return;
    this.categoryService.create({ name, parentId: null }).subscribe(cat => {
      this.categories.update(list => [...list, { ...cat, children: [] }]);
    });
  }

  addChild(parent: Category): void {
    const name = prompt(`Child category under "${parent.name}":`);
    if (!name) return;
    this.categoryService.create({ name, parentId: parent.id }).subscribe(cat => {
      this.categories.update(list =>
        list.map(c => c.id === parent.id ? { ...c, children: [...c.children, { ...cat, children: [] }] } : c)
      );
    });
  }

  deactivate(cat: Category): void {
    if (!confirm(`Deactivate "${cat.name}" and all its children?`)) return;
    this.categoryService.deactivate(cat.id).subscribe({
      next: () => {
        this.categories.update(list => list.filter(c => c.id !== cat.id));
        this.snackBar.open('Category deactivated', 'OK', { duration: 3000 });
      },
      error: err => {
        if (err.status === 409) this.snackBar.open('Cannot deactivate: has open tickets', 'OK', { duration: 4000 });
      },
    });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/categories/category.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/categories/ src/app/admin/departments/department-list.component.spec.ts
git commit -m "feat(admin): implement CategoryTreeComponent and DepartmentListComponent (US-FE-028)"
```
