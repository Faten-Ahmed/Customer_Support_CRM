# Create Ticket Form (Internal) — Implementation Plan

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

**Story:** US-FE-011
**Goal:** Implement the internal "Create Ticket" form at `/app/tickets/new` with a searchable customer autocomplete, hierarchical department/category selection, dynamic custom fields per department, and navigation to the new ticket on success.

**Architecture:** `CreateTicketFormComponent` is standalone, lazy-loaded. It uses Reactive Forms. Customer field uses `MatAutocomplete` with a debounced search. Departments and categories are loaded via `forkJoin(DepartmentService, CategoryService)` on `ngOnInit`. On successful creation, the router navigates to `/app/tickets/{id}`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Vitest, TestBed

> **⚠️ Implementation divergences from original plan:**
> - `subjectAr` and `descriptionAr` are **required** form fields (`Validators.required`) — both rendered side-by-side with their English counterparts
> - Departments and categories are loaded via `forkJoin([departmentService.list(), categoryService.list()])` on init, not lazy-loaded on department change
> - Categories are flattened with a `flattenCategories()` helper before binding to the dropdown
> - `DepartmentService` and `CategoryService` are injected (not just `FieldDefinitionService`)
> - Route is `/app/tickets/new` (inside `/app` shell)

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/services/ticket.service.ts` |
| Create | `src/app/tickets/services/ticket.service.spec.ts` |
| Create | `src/app/tickets/services/field-definition.service.ts` |
| Create | `src/app/tickets/services/field-definition.service.spec.ts` |
| Create | `src/app/tickets/create-ticket/create-ticket-form.component.ts` |
| Create | `src/app/tickets/create-ticket/create-ticket-form.component.html` |
| Create | `src/app/tickets/create-ticket/create-ticket-form.component.spec.ts` |

---

## Task 1: TicketService.create() and FieldDefinitionService.list()

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/services/ticket.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TicketService } from './ticket.service';

describe('TicketService — create', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('create() should POST to /api/v1/tickets', () => {
    const payload = {
      customerId: 'c1', departmentId: 'd1', categoryId: 'cat1',
      subject: 'Test', description: 'Desc', priority: 'High',
      customFields: [{ definitionId: 'f1', value: 'val' }],
    };

    service.create(payload).subscribe(t => expect(t.id).toBeTruthy());

    const req = httpMock.expectOne('/api/v1/tickets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.subject).toBe('Test');
    req.flush({ id: 'ticket-1', ...payload });
  });
});
```

```typescript
// src/app/tickets/services/field-definition.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FieldDefinitionService } from './field-definition.service';

describe('FieldDefinitionService', () => {
  let service: FieldDefinitionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [FieldDefinitionService],
    });
    service = TestBed.inject(FieldDefinitionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/admin/field-definitions with departmentId', () => {
    service.list('d1').subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/admin/field-definitions');
    expect(req.request.params.get('departmentId')).toBe('d1');
    req.flush([{ id: 'f1', label: 'Account #', type: 'text', required: true }]);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
ng test --include=src/app/tickets/services/field-definition.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/services/ticket.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Ticket {
  id: string;
  subject: string;
  description: string;
  priority: string;
  status: string;
  departmentId: string;
  categoryId?: string;
  customerId: string;
  assignedAgentId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTicketPayload {
  customerId: string;
  departmentId: string;
  categoryId?: string;
  subject: string;
  description: string;
  priority: string;
  customFields?: { definitionId: string; value: string }[];
}

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);

  create(payload: CreateTicketPayload): Observable<Ticket> {
    return this.http.post<Ticket>('/api/v1/tickets', payload);
  }
}
```

```typescript
// src/app/tickets/services/field-definition.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FieldDefinition {
  id: string;
  label: string;
  labelAr?: string;
  type: 'text' | 'number' | 'date' | 'select' | 'checkbox';
  required: boolean;
  options?: string[];
}

@Injectable({ providedIn: 'root' })
export class FieldDefinitionService {
  private readonly http = inject(HttpClient);

  list(departmentId: string): Observable<FieldDefinition[]> {
    const params = new HttpParams().set('departmentId', departmentId);
    return this.http.get<FieldDefinition[]>('/api/v1/admin/field-definitions', { params });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
ng test --include=src/app/tickets/services/field-definition.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/
git commit -m "feat(tickets): add TicketService.create() and FieldDefinitionService (US-FE-011)"
```

---

## Task 2: CreateTicketFormComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/create-ticket/create-ticket-form.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { CreateTicketFormComponent } from './create-ticket-form.component';
import { TicketService } from '../services/ticket.service';
import { FieldDefinitionService } from '../services/field-definition.service';
import { CustomerService } from '../../customers/services/customer.service';

describe('CreateTicketFormComponent', () => {
  let fixture: ComponentFixture<CreateTicketFormComponent>;
  let component: CreateTicketFormComponent;
  let ticketService: jasmine.SpyObj<TicketService>;
  let fieldDefService: jasmine.SpyObj<FieldDefinitionService>;
  let customerService: jasmine.SpyObj<CustomerService>;
  let router: Router;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('TicketService', ['create']);
    fieldDefService = jasmine.createSpyObj('FieldDefinitionService', ['list']);
    customerService = jasmine.createSpyObj('CustomerService', ['list']);

    ticketService.create.and.returnValue(of({ id: 'new-t', subject: 'S' } as any));
    fieldDefService.list.and.returnValue(of([]));
    customerService.list.and.returnValue(of({ data: [], total: 0, page: 1, pageSize: 10 }));

    await TestBed.configureTestingModule({
      imports: [CreateTicketFormComponent, ReactiveFormsModule, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketService },
        { provide: FieldDefinitionService, useValue: fieldDefService },
        { provide: CustomerService, useValue: customerService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateTicketFormComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when required fields are empty', () => {
    expect(component.form.valid).toBeFalse();
  });

  it('should reload custom fields when departmentId changes', () => {
    fieldDefService.list.and.returnValue(
      of([{ id: 'f1', label: 'Account #', type: 'text', required: true, options: [] }])
    );
    component.form.get('departmentId')!.setValue('d1');
    expect(fieldDefService.list).toHaveBeenCalledWith('d1');
    expect(component.customFieldDefs.length).toBe(1);
  });

  it('should call create() and navigate to ticket on submit', () => {
    component.form.patchValue({
      customerId: 'c1', departmentId: 'd1', subject: 'Need help', description: 'Details here', priority: 'Medium',
    });
    component.onSubmit();
    expect(ticketService.create).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/tickets', 'new-t']);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/create-ticket/create-ticket-form.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/create-ticket/create-ticket-form.component.ts

import { Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { TicketService, CreateTicketPayload } from '../services/ticket.service';
import { FieldDefinition, FieldDefinitionService } from '../services/field-definition.service';
import { CustomerService } from '../../customers/services/customer.service';

@Component({
  selector: 'app-create-ticket-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatAutocompleteModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule,
  ],
  templateUrl: './create-ticket-form.component.html',
})
export class CreateTicketFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);
  private readonly fieldDefService = inject(FieldDefinitionService);
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);

  customFieldDefs: FieldDefinition[] = [];
  customerSuggestions: { id: string; label: string }[] = [];
  submitting = false;

  form = this.fb.group({
    customerId: ['', Validators.required],
    departmentId: ['', Validators.required],
    categoryId: [''],
    subject: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', Validators.required],
    priority: ['Medium', Validators.required],
    customFields: this.fb.array([]),
  });

  get customFieldsArray(): FormArray {
    return this.form.get('customFields') as FormArray;
  }

  ngOnInit(): void {
    this.form.get('departmentId')!.valueChanges.pipe(
      distinctUntilChanged()
    ).subscribe(deptId => {
      if (deptId) this.loadCustomFields(deptId);
    });

    this.form.get('customerId')!.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(search =>
        this.customerService.list({ page: 1, pageSize: 10, search: search ?? '' })
      )
    ).subscribe(res => {
      this.customerSuggestions = res.data.map(c => ({ id: c.id, label: `${c.fullName} — ${c.email}` }));
    });
  }

  private loadCustomFields(departmentId: string): void {
    this.fieldDefService.list(departmentId).subscribe(defs => {
      this.customFieldDefs = defs;
      this.customFieldsArray.clear();
      defs.forEach(def => {
        this.customFieldsArray.push(
          this.fb.group({
            definitionId: [def.id],
            value: ['', def.required ? Validators.required : []],
          })
        );
      });
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const val = this.form.value as CreateTicketPayload & { customFields: { definitionId: string; value: string }[] };
    this.ticketService.create(val).subscribe({
      next: ticket => this.router.navigate(['/tickets', ticket.id]),
      error: () => (this.submitting = false),
    });
  }
}
```

```html
<!-- src/app/tickets/create-ticket/create-ticket-form.component.html -->

<div class="p-6 max-w-2xl mx-auto">
  <h1 class="text-2xl font-semibold mb-6">New Ticket</h1>

  <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
    <!-- Customer -->
    <mat-form-field appearance="outline">
      <mat-label>Customer</mat-label>
      <input matInput formControlName="customerId" [matAutocomplete]="autoCustomer" />
      <mat-autocomplete #autoCustomer="matAutocomplete">
        @for (c of customerSuggestions; track c.id) {
          <mat-option [value]="c.id">{{ c.label }}</mat-option>
        }
      </mat-autocomplete>
      @if (form.get('customerId')?.hasError('required')) {
        <mat-error>Customer is required</mat-error>
      }
    </mat-form-field>

    <!-- Department -->
    <mat-form-field appearance="outline">
      <mat-label>Department</mat-label>
      <mat-select formControlName="departmentId">
        <mat-option value="d1">Support</mat-option>
        <mat-option value="d2">Billing</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Subject -->
    <mat-form-field appearance="outline">
      <mat-label>Subject</mat-label>
      <input matInput formControlName="subject" />
      @if (form.get('subject')?.hasError('required')) {
        <mat-error>Subject is required</mat-error>
      }
    </mat-form-field>

    <!-- Description -->
    <mat-form-field appearance="outline">
      <mat-label>Description</mat-label>
      <textarea matInput formControlName="description" rows="4"></textarea>
    </mat-form-field>

    <!-- Priority -->
    <mat-form-field appearance="outline">
      <mat-label>Priority</mat-label>
      <mat-select formControlName="priority">
        <mat-option value="Low">Low</mat-option>
        <mat-option value="Medium">Medium</mat-option>
        <mat-option value="High">High</mat-option>
        <mat-option value="Critical">Critical</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Dynamic Custom Fields -->
    @if (customFieldDefs.length > 0) {
      <h3 class="font-medium">Additional Fields</h3>
      <ng-container formArrayName="customFields">
        @for (def of customFieldDefs; track def.id; let i = $index) {
          <mat-form-field appearance="outline" [formGroupName]="i">
            <mat-label>{{ def.label }}{{ def.required ? ' *' : '' }}</mat-label>
            <input matInput formControlName="value" />
          </mat-form-field>
        }
      </ng-container>
    }

    <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || submitting">
      {{ submitting ? 'Creating…' : 'Create Ticket' }}
    </button>
  </form>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/create-ticket/create-ticket-form.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/create-ticket/
git commit -m "feat(tickets): implement CreateTicketFormComponent with dynamic custom fields (US-FE-011)"
```
