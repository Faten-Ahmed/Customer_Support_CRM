# User Management (Admin) — Implementation Plan

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

**Story:** US-FE-027
**Goal:** Implement `/admin/users` — a searchable, filterable staff user list with a "New User" dialog and a `/admin/users/{id}` detail page for department assignments, skill tags, and deactivation.

**Architecture:** `UserListComponent` is a standalone MatTable page. `UserFormComponent` is a `MatDialog`. `UserDetailComponent` is a standalone route page. `UserService` wraps all user API endpoints. Role/department/active filters are query-param based.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/admin/user-management/user.service.ts` |
| Create | `src/app/admin/user-management/user.service.spec.ts` |
| Create | `src/app/admin/user-management/user-list.component.ts` |
| Create | `src/app/admin/user-management/user-list.component.html` |
| Create | `src/app/admin/user-management/user-list.component.spec.ts` |
| Create | `src/app/admin/user-management/user-form-dialog.component.ts` |
| Create | `src/app/admin/user-management/user-form-dialog.component.spec.ts` |
| Create | `src/app/admin/user-management/user-detail.component.ts` |
| Create | `src/app/admin/user-management/user-detail.component.html` |
| Create | `src/app/admin/user-management/user-detail.component.spec.ts` |

---

## Task 1: UserService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/user-management/user.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UserService],
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/admin/users with filters', () => {
    service.list({ page: 1, pageSize: 20, role: 'Agent' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/admin/users');
    expect(req.request.params.get('role')).toBe('Agent');
    req.flush({ data: [], total: 0 });
  });

  it('create() should POST /api/v1/admin/users', () => {
    service.create({ fullName: 'Omar', email: 'omar@test.com', role: 'Agent', tempPassword: 'Temp1234!', primaryDepartmentId: 'd1' }).subscribe();
    const req = httpMock.expectOne('/api/v1/admin/users');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'u1' });
  });

  it('deactivate() should PATCH /api/v1/admin/users/{id}/deactivate', () => {
    service.deactivate('u1').subscribe();
    const req = httpMock.expectOne('/api/v1/admin/users/u1/deactivate');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: 'u1', isActive: false });
  });

  it('updateDepartments() should PUT /api/v1/admin/users/{id}/departments', () => {
    service.updateDepartments('u1', [{ departmentId: 'd1', isPrimary: true }]).subscribe();
    const req = httpMock.expectOne('/api/v1/admin/users/u1/departments');
    expect(req.request.method).toBe('PUT');
    req.flush([]);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/user-management/user.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/user-management/user.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StaffUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
  primaryDepartmentId?: string;
  primaryDepartmentName?: string;
  isActive: boolean;
  availabilityStatus?: string;
  lastLoginAt?: string;
}

export interface UserListQuery {
  page: number;
  pageSize: number;
  role?: string;
  departmentId?: string;
  isActive?: boolean;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  list(query: UserListQuery): Observable<{ data: StaffUser[]; total: number }> {
    let params = new HttpParams().set('page', String(query.page)).set('pageSize', String(query.pageSize));
    if (query.role) params = params.set('role', query.role);
    if (query.departmentId) params = params.set('departmentId', query.departmentId);
    if (query.isActive !== undefined) params = params.set('isActive', String(query.isActive));
    if (query.search) params = params.set('search', query.search);
    return this.http.get<{ data: StaffUser[]; total: number }>('/api/v1/admin/users', { params });
  }

  getById(id: string): Observable<StaffUser> {
    return this.http.get<StaffUser>(`/api/v1/admin/users/${id}`);
  }

  create(payload: { fullName: string; email: string; role: string; tempPassword: string; primaryDepartmentId: string }): Observable<StaffUser> {
    return this.http.post<StaffUser>('/api/v1/admin/users', payload);
  }

  update(id: string, changes: Partial<StaffUser>): Observable<StaffUser> {
    return this.http.patch<StaffUser>(`/api/v1/admin/users/${id}`, changes);
  }

  deactivate(id: string): Observable<StaffUser> {
    return this.http.patch<StaffUser>(`/api/v1/admin/users/${id}/deactivate`, {});
  }

  reactivate(id: string): Observable<StaffUser> {
    return this.http.patch<StaffUser>(`/api/v1/admin/users/${id}/reactivate`, {});
  }

  updateDepartments(id: string, assignments: { departmentId: string; isPrimary: boolean }[]): Observable<unknown> {
    return this.http.put(`/api/v1/admin/users/${id}/departments`, assignments);
  }

  updateSkills(id: string, categoryIds: string[]): Observable<unknown> {
    return this.http.put(`/api/v1/admin/users/${id}/skills`, { categoryIds });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/user-management/user.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/user-management/user.service.ts src/app/admin/user-management/user.service.spec.ts
git commit -m "feat(admin): add UserService with full CRUD and department/skill endpoints (US-FE-027)"
```

---

## Task 2: UserListComponent and UserFormDialogComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/user-management/user-list.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { UserListComponent } from './user-list.component';
import { UserService, StaffUser } from './user.service';

const mockUsers: StaffUser[] = [
  { id: 'u1', fullName: 'Omar Ali', email: 'omar@test.com', role: 'Agent', isActive: true, availabilityStatus: 'Available' },
  { id: 'u2', fullName: 'Sara Noor', email: 'sara@test.com', role: 'Manager', isActive: false },
];

describe('UserListComponent', () => {
  let fixture: ComponentFixture<UserListComponent>;
  let component: UserListComponent;
  let userService: jasmine.SpyObj<UserService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  beforeEach(async () => {
    userService = jasmine.createSpyObj('UserService', ['list']);
    dialog = jasmine.createSpyObj('MatDialog', ['open']);
    userService.list.and.returnValue(of({ data: mockUsers, total: 2 }));

    await TestBed.configureTestingModule({
      imports: [UserListComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: UserService, useValue: userService },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load users', () => {
    expect(component).toBeTruthy();
    expect(component.users().length).toBe(2);
  });

  it('should show role badge and active status', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Agent');
    expect(el.textContent).toContain('Manager');
    expect(el.textContent).toContain('Active');
    expect(el.textContent).toContain('Inactive');
  });

  it('should open dialog on New User click', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(null) } as any);
    component.openNewUserDialog();
    expect(dialog.open).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/user-management/user-list.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/user-management/user-list.component.ts

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
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { CommonModule } from '@angular/common';
import { StaffUser, UserService } from './user.service';
import { UserFormDialogComponent } from './user-form-dialog.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatDialogModule, MatChipsModule],
  templateUrl: './user-list.component.html',
})
export class UserListComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);

  readonly users = signal<StaffUser[]>([]);
  readonly total = signal(0);

  readonly searchControl = new FormControl('');
  readonly roleFilter = new FormControl('');

  displayedColumns = ['fullName', 'email', 'role', 'department', 'status', 'availability', 'lastLogin'];

  ngOnInit(): void {
    this.loadUsers();
    this.searchControl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => this.loadUsers());
    this.roleFilter.valueChanges.subscribe(() => this.loadUsers());
  }

  loadUsers(): void {
    this.userService.list({
      page: 1,
      pageSize: 50,
      search: this.searchControl.value || undefined,
      role: this.roleFilter.value || undefined,
    }).subscribe(res => {
      this.users.set(res.data);
      this.total.set(res.total);
    });
  }

  openNewUserDialog(): void {
    const ref = this.dialog.open(UserFormDialogComponent, { width: '500px', data: null });
    ref.afterClosed().subscribe(created => {
      if (created) this.users.update(list => [...list, created]);
    });
  }
}
```

```html
<!-- src/app/admin/user-management/user-list.component.html -->

<div>
  <div class="flex items-center justify-between mb-4">
    <h2 class="text-xl font-semibold">Staff Users</h2>
    <button mat-raised-button color="primary" (click)="openNewUserDialog()">
      <mat-icon>person_add</mat-icon> New User
    </button>
  </div>

  <div class="flex gap-3 mb-4">
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>Search</mat-label>
      <input matInput [formControl]="searchControl" />
    </mat-form-field>
    <mat-form-field appearance="outline" class="w-40">
      <mat-label>Role</mat-label>
      <mat-select [formControl]="roleFilter">
        <mat-option value="">All</mat-option>
        <mat-option value="Admin">Admin</mat-option>
        <mat-option value="Manager">Manager</mat-option>
        <mat-option value="Agent">Agent</mat-option>
      </mat-select>
    </mat-form-field>
  </div>

  <mat-table [dataSource]="users()" class="w-full">
    <ng-container matColumnDef="fullName">
      <mat-header-cell *matHeaderCellDef>Name</mat-header-cell>
      <mat-cell *matCellDef="let u">
        <a [routerLink]="['/admin/users', u.id]" class="text-blue-600 hover:underline">{{ u.fullName }}</a>
      </mat-cell>
    </ng-container>
    <ng-container matColumnDef="email">
      <mat-header-cell *matHeaderCellDef>Email</mat-header-cell>
      <mat-cell *matCellDef="let u">{{ u.email }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="role">
      <mat-header-cell *matHeaderCellDef>Role</mat-header-cell>
      <mat-cell *matCellDef="let u"><span class="badge-role">{{ u.role }}</span></mat-cell>
    </ng-container>
    <ng-container matColumnDef="department">
      <mat-header-cell *matHeaderCellDef>Department</mat-header-cell>
      <mat-cell *matCellDef="let u">{{ u.primaryDepartmentName || '—' }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="status">
      <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
      <mat-cell *matCellDef="let u">
        <span [class.text-green-600]="u.isActive" [class.text-red-500]="!u.isActive">
          {{ u.isActive ? 'Active' : 'Inactive' }}
        </span>
      </mat-cell>
    </ng-container>
    <ng-container matColumnDef="availability">
      <mat-header-cell *matHeaderCellDef>Availability</mat-header-cell>
      <mat-cell *matCellDef="let u">{{ u.availabilityStatus || '—' }}</mat-cell>
    </ng-container>
    <ng-container matColumnDef="lastLogin">
      <mat-header-cell *matHeaderCellDef>Last Login</mat-header-cell>
      <mat-cell *matCellDef="let u">{{ u.lastLoginAt ? (u.lastLoginAt | date:'shortDate') : 'Never' }}</mat-cell>
    </ng-container>
    <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
    <mat-row *matRowDef="let row; columns: displayedColumns;" class="cursor-pointer hover:bg-gray-50"></mat-row>
  </mat-table>
</div>
```

```typescript
// src/app/admin/user-management/user-form-dialog.component.ts

import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { UserService } from './user.service';

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>New Staff User</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="flex flex-col gap-3">
        <mat-form-field appearance="outline"><mat-label>Full Name</mat-label><input matInput formControlName="fullName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Email</mat-label><input matInput type="email" formControlName="email" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Temporary Password</mat-label><input matInput type="password" formControlName="tempPassword" /></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Role</mat-label>
          <mat-select formControlName="role">
            <mat-option value="Admin">Admin</mat-option>
            <mat-option value="Manager">Manager</mat-option>
            <mat-option value="Agent">Agent</mat-option>
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">Create</button>
    </mat-dialog-actions>
  `,
})
export class UserFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    tempPassword: ['', [Validators.required, Validators.minLength(8)]],
    role: ['Agent', Validators.required],
    primaryDepartmentId: [''],
  });

  constructor(
    public dialogRef: MatDialogRef<UserFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: null
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    this.userService.create(this.form.value as any).subscribe(user => this.dialogRef.close(user));
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/user-management/user-list.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/user-management/
git commit -m "feat(admin): implement UserListComponent and UserFormDialogComponent (US-FE-027)"
```
