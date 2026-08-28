# Portal Submit Ticket Form — Implementation Plan

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

**Story:** US-FE-034
**Goal:** Implement the portal ticket submission form at `/portal/tickets/new` — with department/category dropdowns, dynamic custom fields, and a `409 OPEN_TICKET_EXISTS` inline warning.

**Architecture:** `PortalSubmitTicketComponent` is standalone, lazy-loaded. It mirrors the internal `CreateTicketFormComponent` but without the customer autocomplete (customers are submitting for themselves). On department change, it calls `PortalFieldDefinitionService.list()` for dynamic fields. `409` from the API shows an inline warning with a link to the existing ticket.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/submit-ticket/portal-submit-ticket.component.ts` |
| Create | `src/app/portal/submit-ticket/portal-submit-ticket.component.html` |
| Create | `src/app/portal/submit-ticket/portal-submit-ticket.component.spec.ts` |
| Modify | `src/app/portal/services/portal-ticket.service.ts` |
| Modify | `src/app/portal/services/portal-ticket.service.spec.ts` |

---

## Task 1: Add PortalTicketService.create()

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/portal/services/portal-ticket.service.spec.ts

describe('PortalTicketService — create', () => {
  let service: PortalTicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PortalTicketService],
    });
    service = TestBed.inject(PortalTicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('create() should POST /api/v1/portal/tickets', () => {
    service.create({ departmentId: 'd1', subject: 'Problem', description: 'Details' }).subscribe();
    const req = httpMock.expectOne('/api/v1/portal/tickets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.subject).toBe('Problem');
    req.flush({ id: 't-new', subject: 'Problem' });
  });

  it('should propagate 409 OPEN_TICKET_EXISTS error', () => {
    let errStatus = 0;
    service.create({ departmentId: 'd1', subject: 'Problem', description: 'Details' }).subscribe({
      error: err => (errStatus = err.status),
    });
    const req = httpMock.expectOne('/api/v1/portal/tickets');
    req.flush({ code: 'OPEN_TICKET_EXISTS', existingTicketId: 't-existing' }, { status: 409, statusText: 'Conflict' });
    expect(errStatus).toBe(409);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/portal-ticket.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/portal/services/portal-ticket.service.ts

create(payload: { departmentId: string; categoryId?: string; subject: string; description: string; customFields?: { definitionId: string; value: string }[] }): Observable<PortalTicket> {
  return this.http.post<PortalTicket>('/api/v1/portal/tickets', payload);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/portal-ticket.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/portal-ticket.service.ts src/app/portal/services/portal-ticket.service.spec.ts
git commit -m "feat(portal): add PortalTicketService.create() (US-FE-034)"
```

---

## Task 2: PortalSubmitTicketComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/submit-ticket/portal-submit-ticket.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { PortalSubmitTicketComponent } from './portal-submit-ticket.component';
import { PortalTicketService } from '../services/portal-ticket.service';
import { FieldDefinitionService } from '../../tickets/services/field-definition.service';

describe('PortalSubmitTicketComponent', () => {
  let fixture: ComponentFixture<PortalSubmitTicketComponent>;
  let component: PortalSubmitTicketComponent;
  let ticketService: jasmine.SpyObj<PortalTicketService>;
  let fieldDefService: jasmine.SpyObj<FieldDefinitionService>;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('PortalTicketService', ['create']);
    fieldDefService = jasmine.createSpyObj('FieldDefinitionService', ['list']);
    ticketService.create.and.returnValue(of({ id: 't-new', subject: 'S', status: 'Open', updatedAt: '' }));
    fieldDefService.list.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [PortalSubmitTicketComponent, ReactiveFormsModule, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: PortalTicketService, useValue: ticketService },
        { provide: FieldDefinitionService, useValue: fieldDefService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalSubmitTicketComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when required fields are empty', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('should load custom fields when department changes', () => {
    fieldDefService.list.and.returnValue(of([{ id: 'f1', label: 'Account #', type: 'text', required: true }]));
    component.form.get('departmentId')!.setValue('d1');
    expect(fieldDefService.list).toHaveBeenCalledWith('d1');
    expect(component.customFieldDefs.length).toBe(1);
  });

  it('should show 409 inline warning on duplicate ticket', () => {
    ticketService.create.and.returnValue(throwError(() => ({
      status: 409,
      error: { code: 'OPEN_TICKET_EXISTS', existingTicketId: 't-existing' }
    })));
    component.form.patchValue({ departmentId: 'd1', subject: 'Issue', description: 'Desc' });
    component.onSubmit();
    expect(component.existingTicketId()).toBe('t-existing');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/submit-ticket/portal-submit-ticket.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/submit-ticket/portal-submit-ticket.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PortalTicketService } from '../services/portal-ticket.service';
import { FieldDefinition, FieldDefinitionService } from '../../tickets/services/field-definition.service';

@Component({
  selector: 'app-portal-submit-ticket',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule],
  templateUrl: './portal-submit-ticket.component.html',
})
export class PortalSubmitTicketComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(PortalTicketService);
  private readonly fieldDefService = inject(FieldDefinitionService);
  private readonly router = inject(Router);

  customFieldDefs: FieldDefinition[] = [];
  readonly existingTicketId = signal<string | null>(null);
  readonly submitting = signal(false);

  form = this.fb.group({
    departmentId: ['', Validators.required],
    categoryId: [''],
    subject: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', Validators.required],
    customFields: this.fb.array([]),
  });

  get customFieldsArray(): FormArray { return this.form.get('customFields') as FormArray; }

  ngOnInit(): void {
    this.form.get('departmentId')!.valueChanges.subscribe(deptId => {
      if (deptId) this.loadCustomFields(deptId);
    });
  }

  private loadCustomFields(departmentId: string): void {
    this.fieldDefService.list(departmentId).subscribe(defs => {
      this.customFieldDefs = defs;
      this.customFieldsArray.clear();
      defs.forEach(def => {
        this.customFieldsArray.push(this.fb.group({
          definitionId: [def.id],
          value: ['', def.required ? Validators.required : []],
        }));
      });
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.existingTicketId.set(null);
    const val = this.form.value as any;
    this.ticketService.create(val).subscribe({
      next: ticket => this.router.navigate(['/portal/tickets', ticket.id]),
      error: err => {
        this.submitting.set(false);
        if (err.status === 409) {
          this.existingTicketId.set(err.error?.existingTicketId ?? null);
        }
      },
    });
  }
}
```

```html
<!-- src/app/portal/submit-ticket/portal-submit-ticket.component.html -->

<div class="p-6 max-w-2xl mx-auto">
  <h1 class="text-2xl font-semibold mb-6">Submit a Support Ticket</h1>

  @if (existingTicketId()) {
    <div class="bg-yellow-50 border border-yellow-300 rounded p-4 mb-4 flex items-center gap-3">
      <mat-icon class="text-yellow-600">warning</mat-icon>
      <p class="text-sm">
        You already have an open ticket in this department.
        <a [routerLink]="['/portal/tickets', existingTicketId()]" class="text-blue-600 underline">View existing ticket</a>
      </p>
    </div>
  }

  <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
    <mat-form-field appearance="outline">
      <mat-label>Department</mat-label>
      <mat-select formControlName="departmentId">
        <mat-option value="d1">Support</mat-option>
        <mat-option value="d2">Billing</mat-option>
      </mat-select>
      @if (form.get('departmentId')?.hasError('required')) {
        <mat-error>Department is required</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Subject</mat-label>
      <input matInput formControlName="subject" />
      @if (form.get('subject')?.hasError('required')) {
        <mat-error>Subject is required</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Description</mat-label>
      <textarea matInput formControlName="description" rows="5" placeholder="Please describe your issue in detail"></textarea>
      @if (form.get('description')?.hasError('required')) {
        <mat-error>Description is required</mat-error>
      }
    </mat-form-field>

    @if (customFieldDefs.length > 0) {
      <h3 class="font-medium text-gray-700">Additional Information</h3>
      <ng-container formArrayName="customFields">
        @for (def of customFieldDefs; track def.id; let i = $index) {
          <mat-form-field appearance="outline" [formGroupName]="i">
            <mat-label>{{ def.label }}{{ def.required ? ' *' : '' }}</mat-label>
            <input matInput formControlName="value" />
          </mat-form-field>
        }
      </ng-container>
    }

    <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || submitting()">
      {{ submitting() ? 'Submitting…' : 'Submit Ticket' }}
    </button>
  </form>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/submit-ticket/portal-submit-ticket.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/submit-ticket/
git commit -m "feat(portal): implement PortalSubmitTicketComponent with 409 inline warning (US-FE-034)"
```
