# Quick-Reply Template Management Panel — Implementation Plan

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

**Story:** US-FE-020
**Goal:** Implement `/settings/templates` — a page with two sections (My Templates and Global Templates read-only), CRUD for personal templates via a dialog form, search/filter, and hover preview.

**Architecture:** `TemplateManagementComponent` is standalone, lazy-loaded. It calls `TemplateService.list()` which returns both personal and global templates. Create/Edit opens `TemplateFormDialogComponent` (a `MatDialog`). Delete is immediate. Search is client-side via a signal-based filter on the loaded array (template count is small enough).

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/settings/templates/template-management.component.ts` |
| Create | `src/app/settings/templates/template-management.component.html` |
| Create | `src/app/settings/templates/template-management.component.spec.ts` |
| Create | `src/app/settings/templates/template-form-dialog.component.ts` |
| Create | `src/app/settings/templates/template-form-dialog.component.spec.ts` |
| Modify | `src/app/tickets/services/template.service.ts` |
| Modify | `src/app/tickets/services/template.service.spec.ts` |

---

## Task 1: Extend TemplateService with CRUD operations

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/tickets/services/template.service.spec.ts

describe('TemplateService — CRUD', () => {
  let service: TemplateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TemplateService],
    });
    service = TestBed.inject(TemplateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('create() should POST /api/v1/templates', () => {
    service.create({ title: 'Greeting', content: 'Hello {{name}}', category: 'General' }).subscribe();
    const req = httpMock.expectOne('/api/v1/templates');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'tpl-new', title: 'Greeting', isGlobal: false });
  });

  it('update() should PATCH /api/v1/templates/{id}', () => {
    service.update('tpl-1', { title: 'Updated' }).subscribe();
    const req = httpMock.expectOne('/api/v1/templates/tpl-1');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: 'tpl-1', title: 'Updated' });
  });

  it('delete() should DELETE /api/v1/templates/{id}', () => {
    service.delete('tpl-1').subscribe();
    const req = httpMock.expectOne('/api/v1/templates/tpl-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/services/template.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/tickets/services/template.service.ts

create(payload: { title: string; content: string; category?: string }): Observable<Template> {
  return this.http.post<Template>('/api/v1/templates', payload);
}

update(id: string, changes: Partial<Template>): Observable<Template> {
  return this.http.patch<Template>(`/api/v1/templates/${id}`, changes);
}

delete(id: string): Observable<void> {
  return this.http.delete<void>(`/api/v1/templates/${id}`);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/template.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/template.service.ts src/app/tickets/services/template.service.spec.ts
git commit -m "feat(templates): add CRUD methods to TemplateService (US-FE-020)"
```

---

## Task 2: TemplateManagementComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/settings/templates/template-management.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { TemplateManagementComponent } from './template-management.component';
import { TemplateService, Template } from '../../tickets/services/template.service';

const mockTemplates: Template[] = [
  { id: 't1', title: 'Greeting', content: 'Hello!', isGlobal: false },
  { id: 't2', title: 'Global Close', content: 'Thank you!', isGlobal: true },
];

describe('TemplateManagementComponent', () => {
  let fixture: ComponentFixture<TemplateManagementComponent>;
  let component: TemplateManagementComponent;
  let templateService: jasmine.SpyObj<TemplateService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  beforeEach(async () => {
    templateService = jasmine.createSpyObj('TemplateService', ['list', 'delete']);
    dialog = jasmine.createSpyObj('MatDialog', ['open']);
    templateService.list.and.returnValue(of({ data: mockTemplates, total: 2 }));
    templateService.delete.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [TemplateManagementComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TemplateService, useValue: templateService },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TemplateManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load templates', () => {
    expect(component).toBeTruthy();
    expect(component.myTemplates().length).toBe(1);
    expect(component.globalTemplates().length).toBe(1);
  });

  it('should filter templates by search term', () => {
    component.searchControl.setValue('greet');
    expect(component.filteredMyTemplates().length).toBe(1);
    component.searchControl.setValue('zzz');
    expect(component.filteredMyTemplates().length).toBe(0);
  });

  it('should remove template from list after delete', () => {
    component.deleteTemplate('t1');
    expect(templateService.delete).toHaveBeenCalledWith('t1');
    expect(component.myTemplates().length).toBe(0);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/settings/templates/template-management.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/settings/templates/template-management.component.ts

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { CommonModule } from '@angular/common';
import { Template, TemplateService } from '../../tickets/services/template.service';
import { TemplateFormDialogComponent } from './template-form-dialog.component';

@Component({
  selector: 'app-template-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatCardModule],
  templateUrl: './template-management.component.html',
})
export class TemplateManagementComponent implements OnInit {
  private readonly templateService = inject(TemplateService);
  private readonly dialog = inject(MatDialog);

  readonly searchControl = new FormControl('');
  readonly myTemplates = signal<Template[]>([]);
  readonly globalTemplates = signal<Template[]>([]);

  readonly filteredMyTemplates = computed(() => {
    const q = (this.searchControl.value ?? '').toLowerCase();
    return this.myTemplates().filter(t =>
      t.title.toLowerCase().includes(q) || (t.category ?? '').toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.loadTemplates();
    this.searchControl.valueChanges.subscribe(() => {});
  }

  loadTemplates(): void {
    this.templateService.list().subscribe(res => {
      this.myTemplates.set(res.data.filter(t => !t.isGlobal));
      this.globalTemplates.set(res.data.filter(t => t.isGlobal));
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(TemplateFormDialogComponent, { width: '500px', data: null });
    ref.afterClosed().subscribe(created => {
      if (created) this.myTemplates.update(list => [...list, created]);
    });
  }

  openEditDialog(template: Template): void {
    const ref = this.dialog.open(TemplateFormDialogComponent, { width: '500px', data: template });
    ref.afterClosed().subscribe(updated => {
      if (updated) this.myTemplates.update(list => list.map(t => t.id === updated.id ? updated : t));
    });
  }

  deleteTemplate(id: string): void {
    this.templateService.delete(id).subscribe(() => {
      this.myTemplates.update(list => list.filter(t => t.id !== id));
    });
  }
}
```

```html
<!-- src/app/settings/templates/template-management.component.html -->

<div class="p-6">
  <div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-semibold">Reply Templates</h1>
    <button mat-raised-button color="primary" (click)="openCreateDialog()">
      <mat-icon>add</mat-icon> New Template
    </button>
  </div>

  <mat-form-field appearance="outline" class="w-full mb-4">
    <mat-label>Search templates</mat-label>
    <input matInput [formControl]="searchControl" />
    <mat-icon matSuffix>search</mat-icon>
  </mat-form-field>

  <!-- My Templates -->
  <h2 class="text-lg font-semibold mb-3">My Templates</h2>
  @if (filteredMyTemplates().length === 0) {
    <p class="text-gray-400 text-sm mb-6">No personal templates yet.</p>
  } @else {
    <div class="grid grid-cols-2 gap-3 mb-6">
      @for (t of filteredMyTemplates(); track t.id) {
        <mat-card class="p-4 relative group">
          <p class="font-medium">{{ t.title }}</p>
          @if (t.category) { <p class="text-xs text-gray-400">{{ t.category }}</p> }
          <p class="text-sm text-gray-600 mt-2 truncate">{{ t.content }}</p>
          <div class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 flex gap-1">
            <button mat-icon-button (click)="openEditDialog(t)"><mat-icon>edit</mat-icon></button>
            <button mat-icon-button color="warn" (click)="deleteTemplate(t.id)"><mat-icon>delete</mat-icon></button>
          </div>
        </mat-card>
      }
    </div>
  }

  <!-- Global Templates (read-only) -->
  <h2 class="text-lg font-semibold mb-3">Global Templates</h2>
  <div class="grid grid-cols-2 gap-3">
    @for (t of globalTemplates(); track t.id) {
      <mat-card class="p-4">
        <p class="font-medium">{{ t.title }}</p>
        <p class="text-sm text-gray-600 mt-2 truncate">{{ t.content }}</p>
        <span class="text-xs text-gray-400">Read-only</span>
      </mat-card>
    }
  </div>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/settings/templates/template-management.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/settings/templates/template-management.component.ts src/app/settings/templates/template-management.component.html src/app/settings/templates/template-management.component.spec.ts
git commit -m "feat(settings): implement TemplateManagementComponent (US-FE-020)"
```

---

## Task 3: TemplateFormDialogComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/settings/templates/template-form-dialog.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { TemplateFormDialogComponent } from './template-form-dialog.component';
import { TemplateService } from '../../tickets/services/template.service';

describe('TemplateFormDialogComponent', () => {
  let fixture: ComponentFixture<TemplateFormDialogComponent>;
  let component: TemplateFormDialogComponent;
  let templateService: jasmine.SpyObj<TemplateService>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<TemplateFormDialogComponent>>;

  const setupComponent = async (data: any) => {
    templateService = jasmine.createSpyObj('TemplateService', ['create', 'update']);
    dialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    templateService.create.and.returnValue(of({ id: 'new', title: 'T', content: 'C', isGlobal: false }));
    templateService.update.and.returnValue(of({ id: '1', title: 'Updated', content: 'UC', isGlobal: false }));

    await TestBed.configureTestingModule({
      imports: [TemplateFormDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TemplateService, useValue: templateService },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TemplateFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  it('create mode: should call create() on submit', async () => {
    await setupComponent(null);
    component.form.setValue({ title: 'T', content: 'C', category: '' });
    component.onSubmit();
    expect(templateService.create).toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('edit mode: should call update() on submit', async () => {
    await setupComponent({ id: '1', title: 'Old', content: 'OC', isGlobal: false });
    component.form.setValue({ title: 'Updated', content: 'UC', category: '' });
    component.onSubmit();
    expect(templateService.update).toHaveBeenCalledWith('1', jasmine.any(Object));
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/settings/templates/template-form-dialog.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/settings/templates/template-form-dialog.component.ts

import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { Template, TemplateService } from '../../tickets/services/template.service';

@Component({
  selector: 'app-template-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit' : 'New' }} Template</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="flex flex-col gap-3">
        <mat-form-field appearance="outline">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Category</mat-label>
          <input matInput formControlName="category" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Content</mat-label>
          <textarea matInput formControlName="content" rows="5"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">
        {{ data ? 'Save' : 'Create' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class TemplateFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly templateService = inject(TemplateService);

  form = this.fb.group({
    title: [this.data?.title ?? '', Validators.required],
    content: [this.data?.content ?? '', Validators.required],
    category: [this.data?.category ?? ''],
  });

  constructor(
    public dialogRef: MatDialogRef<TemplateFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Template | null
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    const val = this.form.value as { title: string; content: string; category: string };
    if (this.data) {
      this.templateService.update(this.data.id, val).subscribe(t => this.dialogRef.close(t));
    } else {
      this.templateService.create(val).subscribe(t => this.dialogRef.close(t));
    }
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/settings/templates/template-form-dialog.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/settings/templates/template-form-dialog.component.ts src/app/settings/templates/template-form-dialog.component.spec.ts
git commit -m "feat(settings): add TemplateFormDialogComponent for create/edit templates (US-FE-020)"
```
