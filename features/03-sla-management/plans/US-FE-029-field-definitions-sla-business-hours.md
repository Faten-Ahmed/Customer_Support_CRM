# Field Definitions, SLA Policies & Business Hours — Implementation Plan

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

**Story:** US-FE-029
**Goal:** Build admin configuration pages for custom field definitions, SLA policies, and business hours under `/admin/`.

**Architecture:** Three standalone Angular components (`FieldDefinitionListComponent`, `SlaPolicyTableComponent`, `BusinessHoursEditorComponent`) each backed by a dedicated service that communicates with the REST API. Forms use Angular Reactive Forms with inline validation; the business hours editor tracks per-card unsaved state via a `Signal<boolean>` derived from form dirty status. All components are lazy-loaded under the `AdminModule` route.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/admin/field-definitions/field-definition-list/field-definition-list.component.ts` |
| Create | `src/app/admin/field-definitions/field-definition-list/field-definition-list.component.spec.ts` |
| Create | `src/app/admin/field-definitions/field-definition.service.ts` |
| Create | `src/app/admin/field-definitions/field-definition.service.spec.ts` |
| Create | `src/app/admin/sla/sla-policy-table/sla-policy-table.component.ts` |
| Create | `src/app/admin/sla/sla-policy-table/sla-policy-table.component.spec.ts` |
| Create | `src/app/admin/sla/sla-policy.service.ts` |
| Create | `src/app/admin/business-hours/business-hours-editor/business-hours-editor.component.ts` |
| Create | `src/app/admin/business-hours/business-hours-editor/business-hours-editor.component.spec.ts` |
| Create | `src/app/admin/business-hours/business-hours.service.ts` |

---

## Task 1: FieldDefinitionService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/field-definitions/field-definition.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FieldDefinitionService, FieldDefinition, FieldType } from './field-definition.service';

describe('FieldDefinitionService', () => {
  let service: FieldDefinitionService;
  let http: HttpTestingController;

  const mockFieldDefs: FieldDefinition[] = [
    { id: '1', name: 'Account Number', type: FieldType.Text, departmentId: 'dept-1', isActive: true, options: [] },
    { id: '2', name: 'Category', type: FieldType.Dropdown, departmentId: 'dept-1', isActive: true, options: ['A', 'B', 'C'] },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [FieldDefinitionService],
    });
    service = TestBed.inject(FieldDefinitionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll() should GET /api/admin/field-definitions', () => {
    service.getAll().subscribe(defs => {
      expect(defs.length).toBe(2);
      expect(defs[0].name).toBe('Account Number');
    });
    const req = http.expectOne('/api/admin/field-definitions');
    expect(req.request.method).toBe('GET');
    req.flush(mockFieldDefs);
  });

  it('create() should POST /api/admin/field-definitions', () => {
    const newDef = { name: 'Phone', type: FieldType.Text, departmentId: 'dept-1', isActive: true, options: [] };
    service.create(newDef).subscribe(def => {
      expect(def.id).toBe('3');
    });
    const req = http.expectOne('/api/admin/field-definitions');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newDef);
    req.flush({ ...newDef, id: '3' });
  });

  it('update() should PUT /api/admin/field-definitions/:id', () => {
    const updated = { ...mockFieldDefs[0], name: 'Account No.' };
    service.update('1', updated).subscribe(def => {
      expect(def.name).toBe('Account No.');
    });
    const req = http.expectOne('/api/admin/field-definitions/1');
    expect(req.request.method).toBe('PUT');
    req.flush(updated);
  });

  it('deactivate() should PUT with isActive=false', () => {
    service.deactivate('1').subscribe(def => {
      expect(def.isActive).toBeFalse();
    });
    const req = http.expectOne('/api/admin/field-definitions/1/deactivate');
    expect(req.request.method).toBe('PUT');
    req.flush({ ...mockFieldDefs[0], isActive: false });
  });

  it('getGroupedByDepartment() should return map keyed by departmentId', () => {
    service.getAll().subscribe();
    http.expectOne('/api/admin/field-definitions').flush(mockFieldDefs);

    service.getGroupedByDepartment(mockFieldDefs).subscribe(grouped => {
      expect(grouped['dept-1'].length).toBe(2);
    });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/field-definitions/field-definition.service.spec.ts --watch=false
```

Expected: FAIL — `FieldDefinitionService` does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/field-definitions/field-definition.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

export enum FieldType {
  Text = 'Text',
  Number = 'Number',
  Date = 'Date',
  Dropdown = 'Dropdown',
  Checkbox = 'Checkbox',
}

export interface FieldDefinition {
  id: string;
  name: string;
  type: FieldType;
  departmentId: string;
  isActive: boolean;
  options: string[];
}

export type CreateFieldDefinitionDto = Omit<FieldDefinition, 'id'>;

@Injectable({ providedIn: 'root' })
export class FieldDefinitionService {
  private readonly base = '/api/admin/field-definitions';

  constructor(private http: HttpClient) {}

  getAll(): Observable<FieldDefinition[]> {
    return this.http.get<FieldDefinition[]>(this.base);
  }

  create(dto: CreateFieldDefinitionDto): Observable<FieldDefinition> {
    return this.http.post<FieldDefinition>(this.base, dto);
  }

  update(id: string, dto: Partial<FieldDefinition>): Observable<FieldDefinition> {
    return this.http.put<FieldDefinition>(`${this.base}/${id}`, dto);
  }

  deactivate(id: string): Observable<FieldDefinition> {
    return this.http.put<FieldDefinition>(`${this.base}/${id}/deactivate`, {});
  }

  getGroupedByDepartment(defs: FieldDefinition[]): Observable<Record<string, FieldDefinition[]>> {
    const grouped = defs.reduce((acc, def) => {
      if (!acc[def.departmentId]) acc[def.departmentId] = [];
      acc[def.departmentId].push(def);
      return acc;
    }, {} as Record<string, FieldDefinition[]>);
    return of(grouped);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/field-definitions/field-definition.service.spec.ts --watch=false
```

Expected: 6 tests PASS.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(admin): add FieldDefinitionService with GET/POST/PUT/deactivate"
```

---

## Task 2: FieldDefinitionListComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/field-definitions/field-definition-list/field-definition-list.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { of } from 'rxjs';
import { FieldDefinitionListComponent } from './field-definition-list.component';
import { FieldDefinitionService, FieldDefinition, FieldType } from '../field-definition.service';

describe('FieldDefinitionListComponent', () => {
  let component: FieldDefinitionListComponent;
  let fixture: ComponentFixture<FieldDefinitionListComponent>;
  let serviceSpy: jasmine.SpyObj<FieldDefinitionService>;

  const mockDefs: FieldDefinition[] = [
    { id: '1', name: 'Account Number', type: FieldType.Text, departmentId: 'dept-1', isActive: true, options: [] },
    { id: '2', name: 'Category', type: FieldType.Dropdown, departmentId: 'dept-1', isActive: true, options: ['A', 'B'] },
  ];

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('FieldDefinitionService', ['getAll', 'create', 'update', 'deactivate', 'getGroupedByDepartment']);
    serviceSpy.getAll.and.returnValue(of(mockDefs));
    serviceSpy.getGroupedByDepartment.and.returnValue(of({ 'dept-1': mockDefs }));

    await TestBed.configureTestingModule({
      imports: [
        FieldDefinitionListComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatDialogModule,
        MatSelectModule,
        MatInputModule,
      ],
      providers: [{ provide: FieldDefinitionService, useValue: serviceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(FieldDefinitionListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load field definitions on init', () => {
    expect(serviceSpy.getAll).toHaveBeenCalled();
    expect(component.fieldDefs().length).toBe(2);
  });

  it('should group field definitions by department', () => {
    const grouped = component.groupedDefs();
    expect(grouped['dept-1'].length).toBe(2);
  });

  it('should show options field only when type is Dropdown', () => {
    component.openCreateDialog();
    component.form.get('type')!.setValue(FieldType.Text);
    fixture.detectChanges();
    expect(component.showOptionsField()).toBeFalse();

    component.form.get('type')!.setValue(FieldType.Dropdown);
    fixture.detectChanges();
    expect(component.showOptionsField()).toBeTrue();
  });

  it('should call deactivate service when deactivate clicked', fakeAsync(() => {
    serviceSpy.deactivate.and.returnValue(of({ ...mockDefs[0], isActive: false }));
    serviceSpy.getAll.and.returnValue(of([{ ...mockDefs[0], isActive: false }, mockDefs[1]]));
    component.deactivate('1');
    tick();
    expect(serviceSpy.deactivate).toHaveBeenCalledWith('1');
  }));

  it('should validate form: name required', () => {
    component.openCreateDialog();
    component.form.get('name')!.setValue('');
    component.form.get('name')!.markAsTouched();
    expect(component.form.get('name')!.valid).toBeFalse();
  });

  it('should require options when type is Dropdown', () => {
    component.openCreateDialog();
    component.form.get('type')!.setValue(FieldType.Dropdown);
    component.form.get('options')!.setValue('');
    component.form.get('options')!.markAsTouched();
    expect(component.form.get('options')!.valid).toBeFalse();
  });

  it('should call create service on valid submit', fakeAsync(() => {
    serviceSpy.create.and.returnValue(of({ id: '3', name: 'Phone', type: FieldType.Text, departmentId: 'dept-1', isActive: true, options: [] }));
    component.openCreateDialog();
    component.form.get('name')!.setValue('Phone');
    component.form.get('type')!.setValue(FieldType.Text);
    component.form.get('departmentId')!.setValue('dept-1');
    component.submitForm();
    tick();
    expect(serviceSpy.create).toHaveBeenCalled();
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/field-definitions/field-definition-list/field-definition-list.component.spec.ts --watch=false
```

Expected: FAIL — component does not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/field-definitions/field-definition-list/field-definition-list.component.ts
import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { FieldDefinitionService, FieldDefinition, FieldType, CreateFieldDefinitionDto } from '../field-definition.service';

@Component({
  selector: 'app-field-definition-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatSelectModule,
    MatInputModule,
    MatFormFieldModule,
    MatDialogModule,
    MatChipsModule,
    MatIconModule,
    MatExpansionModule,
    MatSlideToggleModule,
  ],
  template: `
    <div class="field-def-page">
      <div class="page-header">
        <h1>Field Definitions</h1>
        <button mat-raised-button color="primary" (click)="openCreateDialog()">
          <mat-icon>add</mat-icon> Add Field
        </button>
      </div>

      <mat-accordion>
        @for (entry of groupedDefEntries(); track entry[0]) {
          <mat-expansion-panel [expanded]="true">
            <mat-expansion-panel-header>
              <mat-panel-title>Department: {{ entry[0] }}</mat-panel-title>
            </mat-expansion-panel-header>
            <table mat-table [dataSource]="entry[1]" class="full-width">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Name</th>
                <td mat-cell *matCellDef="let def">{{ def.name }}</td>
              </ng-container>
              <ng-container matColumnDef="type">
                <th mat-header-cell *matHeaderCellDef>Type</th>
                <td mat-cell *matCellDef="let def">{{ def.type }}</td>
              </ng-container>
              <ng-container matColumnDef="options">
                <th mat-header-cell *matHeaderCellDef>Options</th>
                <td mat-cell *matCellDef="let def">
                  @if (def.type === 'Dropdown') {
                    <mat-chip-set>
                      @for (opt of def.options; track opt) {
                        <mat-chip>{{ opt }}</mat-chip>
                      }
                    </mat-chip-set>
                  }
                </td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Active</th>
                <td mat-cell *matCellDef="let def">
                  <mat-slide-toggle [checked]="def.isActive" (change)="toggleActive(def)" />
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let def">
                  <button mat-icon-button (click)="openEditDialog(def)"><mat-icon>edit</mat-icon></button>
                  <button mat-icon-button color="warn" (click)="deactivate(def.id)" [disabled]="!def.isActive">
                    <mat-icon>block</mat-icon>
                  </button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;" [class.inactive-row]="!row.isActive"></tr>
            </table>
          </mat-expansion-panel>
        }
      </mat-accordion>

      @if (showDialog()) {
        <div class="dialog-overlay">
          <mat-card class="dialog-card">
            <mat-card-header>
              <mat-card-title>{{ editingId() ? 'Edit' : 'Create' }} Field Definition</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <form [formGroup]="form" class="dialog-form">
                <mat-form-field appearance="outline">
                  <mat-label>Name</mat-label>
                  <input matInput formControlName="name" placeholder="Field name" />
                  @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
                    <mat-error>Name is required</mat-error>
                  }
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Type</mat-label>
                  <mat-select formControlName="type">
                    @for (type of fieldTypes; track type) {
                      <mat-option [value]="type">{{ type }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Department ID</mat-label>
                  <input matInput formControlName="departmentId" />
                </mat-form-field>

                @if (showOptionsField()) {
                  <mat-form-field appearance="outline">
                    <mat-label>Options (comma-separated)</mat-label>
                    <input matInput formControlName="options" placeholder="Option A, Option B" />
                    @if (form.get('options')?.hasError('required') && form.get('options')?.touched) {
                      <mat-error>At least one option is required for Dropdown</mat-error>
                    }
                  </mat-form-field>
                }
              </form>
            </mat-card-content>
            <mat-card-actions align="end">
              <button mat-button (click)="closeDialog()">Cancel</button>
              <button mat-raised-button color="primary" (click)="submitForm()" [disabled]="form.invalid">Save</button>
            </mat-card-actions>
          </mat-card>
        </div>
      }
    </div>
  `,
  styles: [`
    .field-def-page { padding: 24px; }
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    .full-width { width: 100%; }
    .inactive-row { opacity: 0.5; }
    .dialog-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .dialog-card { width: 480px; }
    .dialog-form { display: flex; flex-direction: column; gap: 12px; padding-top: 16px; }
  `],
})
export class FieldDefinitionListComponent implements OnInit {
  fieldDefs = signal<FieldDefinition[]>([]);
  groupedDefs = signal<Record<string, FieldDefinition[]>>({});
  showDialog = signal(false);
  editingId = signal<string | null>(null);

  groupedDefEntries = computed(() => Object.entries(this.groupedDefs()));
  showOptionsField = computed(() => this.form.get('type')?.value === FieldType.Dropdown);

  displayedColumns = ['name', 'type', 'options', 'status', 'actions'];
  fieldTypes = Object.values(FieldType);

  form!: FormGroup;

  constructor(private svc: FieldDefinitionService, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.loadDefs();
  }

  private loadDefs(): void {
    this.svc.getAll().subscribe(defs => {
      this.fieldDefs.set(defs);
      this.svc.getGroupedByDepartment(defs).subscribe(grouped => this.groupedDefs.set(grouped));
    });
  }

  openCreateDialog(): void {
    this.editingId.set(null);
    this.form = this.fb.group({
      name: ['', Validators.required],
      type: [FieldType.Text, Validators.required],
      departmentId: ['', Validators.required],
      options: [''],
      isActive: [true],
    });
    this.form.get('type')!.valueChanges.subscribe(type => {
      const optCtrl = this.form.get('options')!;
      if (type === FieldType.Dropdown) {
        optCtrl.setValidators(Validators.required);
      } else {
        optCtrl.clearValidators();
        optCtrl.setValue('');
      }
      optCtrl.updateValueAndValidity();
    });
    this.showDialog.set(true);
  }

  openEditDialog(def: FieldDefinition): void {
    this.editingId.set(def.id);
    this.form = this.fb.group({
      name: [def.name, Validators.required],
      type: [def.type, Validators.required],
      departmentId: [def.departmentId, Validators.required],
      options: [def.options.join(', ')],
      isActive: [def.isActive],
    });
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (this.form.invalid) return;
    const raw = this.form.value;
    const dto: CreateFieldDefinitionDto = {
      name: raw.name,
      type: raw.type,
      departmentId: raw.departmentId,
      isActive: raw.isActive ?? true,
      options: raw.type === FieldType.Dropdown
        ? raw.options.split(',').map((s: string) => s.trim()).filter(Boolean)
        : [],
    };

    const id = this.editingId();
    const op$ = id ? this.svc.update(id, dto) : this.svc.create(dto);
    op$.subscribe(() => { this.closeDialog(); this.loadDefs(); });
  }

  deactivate(id: string): void {
    this.svc.deactivate(id).subscribe(() => this.loadDefs());
  }

  toggleActive(def: FieldDefinition): void {
    if (def.isActive) {
      this.deactivate(def.id);
    } else {
      this.svc.update(def.id, { isActive: true }).subscribe(() => this.loadDefs());
    }
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/field-definitions/field-definition-list/field-definition-list.component.spec.ts --watch=false
```

Expected: 8 tests PASS.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(admin): add FieldDefinitionListComponent with create/edit/deactivate and Dropdown options toggle"
```

---

## Task 3: SlaPolicyService & SlaPolicyTableComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/sla/sla-policy.service.spec.ts — embed in sla-policy-table.component.spec.ts or as standalone

// src/app/admin/sla/sla-policy-table/sla-policy-table.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { SlaPolicyTableComponent } from './sla-policy-table.component';
import { SlaPolicyService, SlaPolicy, SlaPriority } from '../sla-policy.service';

describe('SlaPolicyTableComponent', () => {
  let component: SlaPolicyTableComponent;
  let fixture: ComponentFixture<SlaPolicyTableComponent>;
  let serviceSpy: jasmine.SpyObj<SlaPolicyService>;

  const mockPolicies: SlaPolicy[] = [
    { id: '1', name: 'Critical SLA', priority: SlaPriority.Critical, firstResponseMinutes: 30, resolutionMinutes: 240 },
    { id: '2', name: 'High SLA', priority: SlaPriority.High, firstResponseMinutes: 60, resolutionMinutes: 480 },
  ];

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('SlaPolicyService', ['getAll', 'update']);
    serviceSpy.getAll.and.returnValue(of(mockPolicies));

    await TestBed.configureTestingModule({
      imports: [SlaPolicyTableComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: SlaPolicyService, useValue: serviceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaPolicyTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load policies on init and group by priority', () => {
    expect(serviceSpy.getAll).toHaveBeenCalled();
    expect(component.policies().length).toBe(2);
    expect(component.policiesByPriority()['Critical'].length).toBe(1);
  });

  it('should enter inline edit mode for a row', () => {
    component.startEdit(mockPolicies[0]);
    expect(component.editingId()).toBe('1');
    expect(component.editForm.get('firstResponseMinutes')?.value).toBe(30);
  });

  it('should validate: firstResponseMinutes must be positive', () => {
    component.startEdit(mockPolicies[0]);
    component.editForm.get('firstResponseMinutes')!.setValue(-5);
    component.editForm.get('firstResponseMinutes')!.markAsTouched();
    expect(component.editForm.get('firstResponseMinutes')!.valid).toBeFalse();
  });

  it('should call update on saveEdit', fakeAsync(() => {
    serviceSpy.update.and.returnValue(of({ ...mockPolicies[0], firstResponseMinutes: 20 }));
    serviceSpy.getAll.and.returnValue(of([{ ...mockPolicies[0], firstResponseMinutes: 20 }, mockPolicies[1]]));
    component.startEdit(mockPolicies[0]);
    component.editForm.get('firstResponseMinutes')!.setValue(20);
    component.saveEdit();
    tick();
    expect(serviceSpy.update).toHaveBeenCalledWith('1', jasmine.objectContaining({ firstResponseMinutes: 20 }));
  }));

  it('should cancel edit and reset editingId', () => {
    component.startEdit(mockPolicies[0]);
    component.cancelEdit();
    expect(component.editingId()).toBeNull();
  });

  it('should show inline error if resolutionMinutes < firstResponseMinutes', () => {
    component.startEdit(mockPolicies[0]);
    component.editForm.get('firstResponseMinutes')!.setValue(300);
    component.editForm.get('resolutionMinutes')!.setValue(100);
    component.editForm.updateValueAndValidity();
    expect(component.editForm.hasError('resolutionBeforeResponse')).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/sla/sla-policy-table/sla-policy-table.component.spec.ts --watch=false
```

Expected: FAIL — component and service do not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/sla/sla-policy.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum SlaPriority {
  Critical = 'Critical',
  High = 'High',
  Medium = 'Medium',
  Low = 'Low',
}

export interface SlaPolicy {
  id: string;
  name: string;
  priority: SlaPriority;
  firstResponseMinutes: number;
  resolutionMinutes: number;
}

@Injectable({ providedIn: 'root' })
export class SlaPolicyService {
  private readonly base = '/api/admin/sla-policies';

  constructor(private http: HttpClient) {}

  getAll(): Observable<SlaPolicy[]> {
    return this.http.get<SlaPolicy[]>(this.base);
  }

  update(id: string, dto: Partial<SlaPolicy>): Observable<SlaPolicy> {
    return this.http.put<SlaPolicy>(`${this.base}/${id}`, dto);
  }
}
```

```typescript
// src/app/admin/sla/sla-policy-table/sla-policy-table.component.ts
import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { SlaPolicyService, SlaPolicy, SlaPriority } from '../sla-policy.service';

function resolutionAfterResponseValidator(ctrl: AbstractControl): ValidationErrors | null {
  const first = ctrl.get('firstResponseMinutes')?.value;
  const res = ctrl.get('resolutionMinutes')?.value;
  if (first != null && res != null && res < first) {
    return { resolutionBeforeResponse: true };
  }
  return null;
}

@Component({
  selector: 'app-sla-policy-table',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatIconModule,
  ],
  template: `
    <div class="sla-page">
      <h1>SLA Policies</h1>
      @for (priority of priorities; track priority) {
        @if (policiesByPriority()[priority]?.length) {
          <h2>{{ priority }}</h2>
          <table mat-table [dataSource]="policiesByPriority()[priority]!" class="full-width mat-elevation-z2">
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Policy Name</th>
              <td mat-cell *matCellDef="let policy">{{ policy.name }}</td>
            </ng-container>
            <ng-container matColumnDef="firstResponse">
              <th mat-header-cell *matHeaderCellDef>First Response (min)</th>
              <td mat-cell *matCellDef="let policy">
                @if (editingId() === policy.id) {
                  <mat-form-field appearance="outline" style="width:120px">
                    <input matInput type="number" [formControl]="$any(editForm.get('firstResponseMinutes'))" />
                    @if (editForm.get('firstResponseMinutes')?.hasError('min')) {
                      <mat-error>Must be > 0</mat-error>
                    }
                  </mat-form-field>
                } @else {
                  {{ policy.firstResponseMinutes }}
                }
              </td>
            </ng-container>
            <ng-container matColumnDef="resolution">
              <th mat-header-cell *matHeaderCellDef>Resolution (min)</th>
              <td mat-cell *matCellDef="let policy">
                @if (editingId() === policy.id) {
                  <mat-form-field appearance="outline" style="width:120px">
                    <input matInput type="number" [formControl]="$any(editForm.get('resolutionMinutes'))" />
                    @if (editForm.hasError('resolutionBeforeResponse')) {
                      <mat-error>Must be ≥ first response</mat-error>
                    }
                  </mat-form-field>
                } @else {
                  {{ policy.resolutionMinutes }}
                }
              </td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let policy">
                @if (editingId() === policy.id) {
                  <button mat-icon-button color="primary" (click)="saveEdit()" [disabled]="editForm.invalid"><mat-icon>check</mat-icon></button>
                  <button mat-icon-button (click)="cancelEdit()"><mat-icon>close</mat-icon></button>
                } @else {
                  <button mat-icon-button (click)="startEdit(policy)"><mat-icon>edit</mat-icon></button>
                }
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        }
      }
    </div>
  `,
  styles: [`.sla-page { padding: 24px; } .full-width { width: 100%; margin-bottom: 24px; }`],
})
export class SlaPolicyTableComponent implements OnInit {
  policies = signal<SlaPolicy[]>([]);
  editingId = signal<string | null>(null);
  editForm!: FormGroup;

  policiesByPriority = computed(() => {
    return this.policies().reduce((acc, p) => {
      if (!acc[p.priority]) acc[p.priority] = [];
      acc[p.priority].push(p);
      return acc;
    }, {} as Record<string, SlaPolicy[]>);
  });

  priorities = Object.values(SlaPriority);
  displayedColumns = ['name', 'firstResponse', 'resolution', 'actions'];

  constructor(private svc: SlaPolicyService, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.loadPolicies();
  }

  private loadPolicies(): void {
    this.svc.getAll().subscribe(p => this.policies.set(p));
  }

  startEdit(policy: SlaPolicy): void {
    this.editingId.set(policy.id);
    this.editForm = this.fb.group({
      firstResponseMinutes: [policy.firstResponseMinutes, [Validators.required, Validators.min(1)]],
      resolutionMinutes: [policy.resolutionMinutes, [Validators.required, Validators.min(1)]],
    }, { validators: resolutionAfterResponseValidator });
  }

  saveEdit(): void {
    if (this.editForm.invalid) return;
    const id = this.editingId()!;
    this.svc.update(id, this.editForm.value).subscribe(() => {
      this.editingId.set(null);
      this.loadPolicies();
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/sla/sla-policy-table/sla-policy-table.component.spec.ts --watch=false
```

Expected: 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(admin): add SlaPolicyService and SlaPolicyTableComponent with inline edit and cross-field validation"
```

---

## Task 4: BusinessHoursService & BusinessHoursEditorComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/admin/business-hours/business-hours-editor/business-hours-editor.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { BusinessHoursEditorComponent } from './business-hours-editor.component';
import { BusinessHoursService, BusinessHoursCard } from '../business-hours.service';

describe('BusinessHoursEditorComponent', () => {
  let component: BusinessHoursEditorComponent;
  let fixture: ComponentFixture<BusinessHoursEditorComponent>;
  let serviceSpy: jasmine.SpyObj<BusinessHoursService>;

  const mockCards: BusinessHoursCard[] = [
    {
      id: 'global',
      label: 'Global',
      departmentId: null,
      workDays: [false, true, true, true, true, true, false],
      startTime: '08:00',
      endTime: '17:00',
      timezone: 'UTC',
      holidays: [{ date: '2026-01-01', name: 'New Year' }],
    },
    {
      id: 'dept-1',
      label: 'Support',
      departmentId: 'dept-1',
      workDays: [false, true, true, true, true, true, false],
      startTime: '09:00',
      endTime: '18:00',
      timezone: 'Asia/Riyadh',
      holidays: [],
    },
  ];

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('BusinessHoursService', ['getAll', 'save']);
    serviceSpy.getAll.and.returnValue(of(mockCards));
    serviceSpy.save.and.returnValue(of(mockCards[0]));

    await TestBed.configureTestingModule({
      imports: [BusinessHoursEditorComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: BusinessHoursService, useValue: serviceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(BusinessHoursEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load cards on init', () => {
    expect(serviceSpy.getAll).toHaveBeenCalled();
    expect(component.cards().length).toBe(2);
  });

  it('should mark card as unsaved when form is changed', fakeAsync(() => {
    const form = component.cardForms['global'];
    form.get('startTime')!.setValue('07:00');
    tick();
    expect(component.unsavedCards().has('global')).toBeTrue();
  }));

  it('should save card and clear unsaved state', fakeAsync(() => {
    component.saveCard('global');
    tick();
    expect(serviceSpy.save).toHaveBeenCalledWith('global', jasmine.any(Object));
    expect(component.unsavedCards().has('global')).toBeFalse();
  }));

  it('should add a holiday to a card', () => {
    component.addHoliday('global', '2026-12-25', 'Christmas');
    expect(component.cardForms['global'].get('holidays')!.value.length).toBe(2);
  });

  it('should remove a holiday from a card', () => {
    component.removeHoliday('global', 0);
    expect(component.cardForms['global'].get('holidays')!.value.length).toBe(0);
  });

  it('should validate: startTime must be before endTime', () => {
    const form = component.cardForms['global'];
    form.get('startTime')!.setValue('18:00');
    form.get('endTime')!.setValue('08:00');
    form.updateValueAndValidity();
    expect(form.hasError('endBeforeStart')).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/admin/business-hours/business-hours-editor/business-hours-editor.component.spec.ts --watch=false
```

Expected: FAIL — component and service do not exist.

- [ ] **Step 3: Implement**

```typescript
// src/app/admin/business-hours/business-hours.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Holiday {
  date: string;
  name: string;
}

export interface BusinessHoursCard {
  id: string;
  label: string;
  departmentId: string | null;
  workDays: boolean[];   // index 0=Sun … 6=Sat
  startTime: string;     // HH:mm
  endTime: string;       // HH:mm
  timezone: string;
  holidays: Holiday[];
}

@Injectable({ providedIn: 'root' })
export class BusinessHoursService {
  private readonly base = '/api/admin/business-hours';

  constructor(private http: HttpClient) {}

  getAll(): Observable<BusinessHoursCard[]> {
    return this.http.get<BusinessHoursCard[]>(this.base);
  }

  save(id: string, dto: Partial<BusinessHoursCard>): Observable<BusinessHoursCard> {
    return this.http.put<BusinessHoursCard>(`${this.base}/${id}`, dto);
  }
}
```

```typescript
// src/app/admin/business-hours/business-hours-editor/business-hours-editor.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { BusinessHoursService, BusinessHoursCard, Holiday } from '../business-hours.service';

const TIMEZONES = ['UTC', 'Asia/Riyadh', 'Asia/Dubai', 'America/New_York', 'Europe/London'];
const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

function endAfterStartValidator(ctrl: AbstractControl): ValidationErrors | null {
  const start = ctrl.get('startTime')?.value as string;
  const end = ctrl.get('endTime')?.value as string;
  if (start && end && end <= start) return { endBeforeStart: true };
  return null;
}

@Component({
  selector: 'app-business-hours-editor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatCheckboxModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatBadgeModule,
  ],
  template: `
    <div class="bh-page">
      <h1>Business Hours</h1>
      <div class="cards-grid">
        @for (card of cards(); track card.id) {
          <mat-card class="bh-card" [class.unsaved]="unsavedCards().has(card.id)">
            <mat-card-header>
              <mat-card-title>
                {{ card.label }}
                @if (unsavedCards().has(card.id)) {
                  <span class="unsaved-indicator">● Unsaved changes</span>
                }
              </mat-card-title>
            </mat-card-header>
            <mat-card-content [formGroup]="cardForms[card.id]">
              <div class="work-days">
                <label>Work Days</label>
                <div class="day-checkboxes">
                  @for (day of dayLabels; track day; let i = $index) {
                    <mat-checkbox [formControl]="$any(getWorkDaysArray(card.id).at(i))">{{ day }}</mat-checkbox>
                  }
                </div>
              </div>
              <div class="time-row">
                <mat-form-field appearance="outline">
                  <mat-label>Start Time</mat-label>
                  <input matInput type="time" formControlName="startTime" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>End Time</mat-label>
                  <input matInput type="time" formControlName="endTime" />
                  @if (cardForms[card.id].hasError('endBeforeStart')) {
                    <mat-error>End must be after start</mat-error>
                  }
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Timezone</mat-label>
                  <mat-select formControlName="timezone">
                    @for (tz of timezones; track tz) {
                      <mat-option [value]="tz">{{ tz }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>

              <div class="holidays-section">
                <h4>Holidays</h4>
                @for (h of getHolidaysArray(card.id).controls; track h; let hi = $index) {
                  <div class="holiday-row" [formGroup]="$any(h)">
                    <mat-form-field appearance="outline" style="width:140px">
                      <input matInput type="date" formControlName="date" />
                    </mat-form-field>
                    <mat-form-field appearance="outline" style="flex:1">
                      <input matInput formControlName="name" placeholder="Holiday name" />
                    </mat-form-field>
                    <button mat-icon-button color="warn" (click)="removeHoliday(card.id, hi)">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </div>
                }
                <button mat-stroked-button (click)="addHoliday(card.id, '', '')">
                  <mat-icon>add</mat-icon> Add Holiday
                </button>
              </div>
            </mat-card-content>
            <mat-card-actions align="end">
              <button mat-raised-button color="primary" (click)="saveCard(card.id)" [disabled]="cardForms[card.id].invalid">
                Save
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .bh-page { padding: 24px; }
    .cards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(480px, 1fr)); gap: 24px; }
    .bh-card.unsaved { border: 2px solid #f59e0b; }
    .unsaved-indicator { font-size: 12px; color: #f59e0b; margin-left: 8px; }
    .work-days { margin-bottom: 16px; }
    .day-checkboxes { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
    .time-row { display: flex; gap: 16px; flex-wrap: wrap; }
    .holiday-row { display: flex; gap: 8px; align-items: center; }
    .holidays-section { margin-top: 16px; }
  `],
})
export class BusinessHoursEditorComponent implements OnInit {
  cards = signal<BusinessHoursCard[]>([]);
  unsavedCards = signal<Set<string>>(new Set());
  cardForms: Record<string, FormGroup> = {};
  dayLabels = DAY_LABELS;
  timezones = TIMEZONES;

  constructor(private svc: BusinessHoursService, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.svc.getAll().subscribe(cards => {
      this.cards.set(cards);
      cards.forEach(c => this.buildForm(c));
    });
  }

  private buildForm(card: BusinessHoursCard): void {
    const form = this.fb.group({
      workDays: this.fb.array(card.workDays.map(d => this.fb.control(d))),
      startTime: [card.startTime, Validators.required],
      endTime: [card.endTime, Validators.required],
      timezone: [card.timezone, Validators.required],
      holidays: this.fb.array(card.holidays.map(h => this.fb.group({ date: [h.date], name: [h.name] }))),
    }, { validators: endAfterStartValidator });

    form.valueChanges.subscribe(() => {
      if (form.dirty) {
        this.unsavedCards.update(s => new Set([...s, card.id]));
      }
    });

    this.cardForms[card.id] = form;
  }

  getWorkDaysArray(cardId: string): FormArray {
    return this.cardForms[cardId].get('workDays') as FormArray;
  }

  getHolidaysArray(cardId: string): FormArray {
    return this.cardForms[cardId].get('holidays') as FormArray;
  }

  addHoliday(cardId: string, date: string, name: string): void {
    this.getHolidaysArray(cardId).push(this.fb.group({ date: [date], name: [name] }));
    this.cardForms[cardId].markAsDirty();
    this.unsavedCards.update(s => new Set([...s, cardId]));
  }

  removeHoliday(cardId: string, index: number): void {
    this.getHolidaysArray(cardId).removeAt(index);
    this.cardForms[cardId].markAsDirty();
    this.unsavedCards.update(s => new Set([...s, cardId]));
  }

  saveCard(cardId: string): void {
    const form = this.cardForms[cardId];
    if (form.invalid) return;
    const raw = form.value;
    const dto: Partial<BusinessHoursCard> = {
      workDays: raw.workDays,
      startTime: raw.startTime,
      endTime: raw.endTime,
      timezone: raw.timezone,
      holidays: raw.holidays as Holiday[],
    };
    this.svc.save(cardId, dto).subscribe(() => {
      form.markAsPristine();
      this.unsavedCards.update(s => { const next = new Set(s); next.delete(cardId); return next; });
    });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/admin/business-hours/business-hours-editor/business-hours-editor.component.spec.ts --watch=false
```

Expected: 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(admin): add BusinessHoursService and BusinessHoursEditorComponent with per-card unsaved state, holiday management, and time validation"
```
