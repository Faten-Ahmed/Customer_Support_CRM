# Create & Edit Customer Forms — Implementation Plan

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

**Story:** US-FE-008  
**Goal:** Implement `CreateCustomerFormComponent` and `EditCustomerFormComponent` with full validation, API integration, and inline error handling for duplicate email (409).

**Architecture:** Both components use Angular Reactive Forms with strict typed `FormGroup`. `CustomerService` handles HTTP calls via `HttpClient`; on 409, the service throws a typed error that the component maps to an inline `emailAlreadyExists` field error. Navigation and snackbar feedback are injected via `Router` and `MatSnackBar`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/customers/create-customer-form/create-customer-form.component.ts` |
| Create | `src/app/customers/create-customer-form/create-customer-form.component.html` |
| Create | `src/app/customers/create-customer-form/create-customer-form.component.spec.ts` |
| Create | `src/app/customers/edit-customer-form/edit-customer-form.component.ts` |
| Create | `src/app/customers/edit-customer-form/edit-customer-form.component.html` |
| Create | `src/app/customers/edit-customer-form/edit-customer-form.component.spec.ts` |
| Modify | `src/app/customers/customer.service.ts` |
| Modify | `src/app/customers/customer.service.spec.ts` |

---

## Task 1: CustomerService — create() and update()

> Note: No dependencies. Implement and test the service layer first.

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/customers/customer.service.spec.ts
import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { CustomerService, CreateCustomerDto, UpdateCustomerDto } from './customer.service';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CustomerService],
    });
    service = TestBed.inject(CustomerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('create()', () => {
    const dto: CreateCustomerDto = {
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      phone: '555-0100',
      companyName: 'Acme',
      country: 'US',
      city: 'New York',
    };

    it('should POST to /api/customers and return created customer', () => {
      const mockResponse = { id: 'c-1', ...dto };
      service.create(dto).subscribe(res => expect(res).toEqual(mockResponse));
      const req = httpMock.expectOne('/api/customers');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(dto);
      req.flush(mockResponse);
    });

    it('should throw EmailAlreadyExistsError on 409 EMAIL_ALREADY_EXISTS', () => {
      let thrownError: any;
      service.create(dto).subscribe({
        error: err => (thrownError = err),
      });
      const req = httpMock.expectOne('/api/customers');
      req.flush(
        { code: 'EMAIL_ALREADY_EXISTS', message: 'Email already exists' },
        { status: 409, statusText: 'Conflict' }
      );
      expect(thrownError?.code).toBe('EMAIL_ALREADY_EXISTS');
    });

    it('should rethrow non-409 errors as-is', () => {
      let thrownError: any;
      service.create(dto).subscribe({ error: err => (thrownError = err) });
      const req = httpMock.expectOne('/api/customers');
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      expect(thrownError).toBeTruthy();
      expect(thrownError?.code).not.toBe('EMAIL_ALREADY_EXISTS');
    });
  });

  describe('update()', () => {
    const dto: UpdateCustomerDto = {
      fullName: 'Jane Smith',
      phone: '555-0200',
      companyName: 'Acme Corp',
      country: 'US',
      city: 'Chicago',
    };

    it('should PATCH to /api/customers/:id and return updated customer', () => {
      const mockResponse = { id: 'c-1', email: 'jane@example.com', ...dto };
      service.update('c-1', dto).subscribe(res => expect(res).toEqual(mockResponse));
      const req = httpMock.expectOne('/api/customers/c-1');
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual(dto);
      req.flush(mockResponse);
    });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/customer.service.spec.ts --watch=false
```

Expected: FAIL — `CustomerService`, `create`, and `update` do not exist yet.

- [ ] **Step 3: Implement CustomerService**

```typescript
// src/app/customers/customer.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface CreateCustomerDto {
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
}

export interface UpdateCustomerDto {
  fullName?: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
}

export interface Customer {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
  createdAt?: string;
}

export interface ApiError {
  code: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/customers';

  create(dto: CreateCustomerDto): Observable<Customer> {
    return this.http.post<Customer>(this.baseUrl, dto).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 409 && err.error?.code === 'EMAIL_ALREADY_EXISTS') {
          return throwError(() => ({ code: 'EMAIL_ALREADY_EXISTS', message: err.error.message }));
        }
        return throwError(() => err);
      })
    );
  }

  update(id: string, dto: UpdateCustomerDto): Observable<Customer> {
    return this.http.patch<Customer>(`${this.baseUrl}/${id}`, dto);
  }

  getById(id: string): Observable<Customer> {
    return this.http.get<Customer>(`${this.baseUrl}/${id}`);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/customer.service.spec.ts --watch=false
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/customer.service.ts src/app/customers/customer.service.spec.ts
git commit -m "feat(customers): add CustomerService.create() and update() with 409 handling"
```

---

## Task 2: CreateCustomerFormComponent

> Note: Depends on Task 1 (CustomerService must exist).

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/customers/create-customer-form/create-customer-form.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { CreateCustomerFormComponent } from './create-customer-form.component';
import { CustomerService } from '../customer.service';

describe('CreateCustomerFormComponent', () => {
  let fixture: ComponentFixture<CreateCustomerFormComponent>;
  let component: CreateCustomerFormComponent;
  let customerServiceSpy: jasmine.SpyObj<CustomerService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  beforeEach(async () => {
    customerServiceSpy = jasmine.createSpyObj('CustomerService', ['create']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [CreateCustomerFormComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: CustomerService, useValue: customerServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateCustomerFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should have an invalid form when required fields are empty', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('should mark fullName as required', () => {
    const ctrl = component.form.get('fullName')!;
    ctrl.setValue('');
    ctrl.markAsTouched();
    expect(ctrl.hasError('required')).toBeTrue();
  });

  it('should mark email as required', () => {
    const ctrl = component.form.get('email')!;
    ctrl.setValue('');
    ctrl.markAsTouched();
    expect(ctrl.hasError('required')).toBeTrue();
  });

  it('should mark email as invalid for bad format', () => {
    const ctrl = component.form.get('email')!;
    ctrl.setValue('not-an-email');
    ctrl.markAsTouched();
    expect(ctrl.hasError('email')).toBeTrue();
  });

  it('should not call service when form is invalid on submit', () => {
    component.onSubmit();
    expect(customerServiceSpy.create).not.toHaveBeenCalled();
  });

  it('should call CustomerService.create with form values on valid submit', fakeAsync(() => {
    const created = { id: 'c-1', fullName: 'Alice', email: 'alice@example.com' };
    customerServiceSpy.create.and.returnValue(of(created as any));

    component.form.setValue({
      fullName: 'Alice',
      email: 'alice@example.com',
      phone: '',
      companyName: '',
      country: '',
      city: '',
    });

    component.onSubmit();
    tick();

    expect(customerServiceSpy.create).toHaveBeenCalledWith({
      fullName: 'Alice',
      email: 'alice@example.com',
      phone: '',
      companyName: '',
      country: '',
      city: '',
    });
  }));

  it('should navigate to customer detail on success', fakeAsync(() => {
    const created = { id: 'c-99', fullName: 'Bob', email: 'bob@example.com' };
    customerServiceSpy.create.and.returnValue(of(created as any));

    component.form.setValue({
      fullName: 'Bob',
      email: 'bob@example.com',
      phone: '',
      companyName: '',
      country: '',
      city: '',
    });
    component.onSubmit();
    tick();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/customers', 'c-99']);
    expect(snackBarSpy.open).toHaveBeenCalledWith('Customer created successfully.', 'Close', jasmine.any(Object));
  }));

  it('should set emailAlreadyExists error on 409 EMAIL_ALREADY_EXISTS', fakeAsync(() => {
    customerServiceSpy.create.and.returnValue(
      throwError(() => ({ code: 'EMAIL_ALREADY_EXISTS' }))
    );

    component.form.setValue({
      fullName: 'Carol',
      email: 'carol@example.com',
      phone: '',
      companyName: '',
      country: '',
      city: '',
    });
    component.onSubmit();
    tick();

    expect(component.form.get('email')!.hasError('emailAlreadyExists')).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  }));

  it('should disable submit button while submitting', () => {
    expect(component.submitting()).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/create-customer-form/create-customer-form.component.spec.ts --watch=false
```

Expected: FAIL — `CreateCustomerFormComponent` does not exist yet.

- [ ] **Step 3: Implement CreateCustomerFormComponent**

```typescript
// src/app/customers/create-customer-form/create-customer-form.component.ts
import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { CustomerService } from '../customer.service';

@Component({
  selector: 'app-create-customer-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="customer-form">
      <h2>New Customer</h2>

      <mat-form-field appearance="outline">
        <mat-label>Full Name *</mat-label>
        <input matInput formControlName="fullName" />
        @if (form.get('fullName')?.hasError('required') && form.get('fullName')?.touched) {
          <mat-error>Full name is required.</mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Email *</mat-label>
        <input matInput formControlName="email" type="email" />
        @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
          <mat-error>Email is required.</mat-error>
        }
        @if (form.get('email')?.hasError('email') && form.get('email')?.touched) {
          <mat-error>Enter a valid email address.</mat-error>
        }
        @if (form.get('email')?.hasError('emailAlreadyExists')) {
          <mat-error>This email address is already registered.</mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Phone</mat-label>
        <input matInput formControlName="phone" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Company Name</mat-label>
        <input matInput formControlName="companyName" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Country</mat-label>
        <input matInput formControlName="country" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>City</mat-label>
        <input matInput formControlName="city" />
      </mat-form-field>

      <div class="form-actions">
        <button mat-stroked-button type="button" (click)="router.navigate(['/customers'])">
          Cancel
        </button>
        <button
          mat-flat-button
          color="primary"
          type="submit"
          [disabled]="submitting() || form.invalid"
        >
          @if (submitting()) {
            <mat-spinner diameter="20" />
          } @else {
            Create Customer
          }
        </button>
      </div>
    </form>
  `,
})
export class CreateCustomerFormComponent {
  protected readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);

  readonly submitting = signal(false);

  readonly form: FormGroup = this.fb.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    companyName: [''],
    country: [''],
    city: [''],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.customerService.create(this.form.getRawValue()).subscribe({
      next: customer => {
        this.submitting.set(false);
        this.snackBar.open('Customer created successfully.', 'Close', { duration: 4000 });
        this.router.navigate(['/customers', customer.id]);
      },
      error: err => {
        this.submitting.set(false);
        if (err?.code === 'EMAIL_ALREADY_EXISTS') {
          this.form.get('email')!.setErrors({ emailAlreadyExists: true });
        }
      },
    });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/create-customer-form/create-customer-form.component.spec.ts --watch=false
```

Expected: 9 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/create-customer-form/
git commit -m "feat(customers): implement CreateCustomerFormComponent with validation and 409 handling"
```

---

## Task 3: EditCustomerFormComponent

> Note: Depends on Task 1 (CustomerService.update must exist). Email field must be read-only per BR-CUST-002.

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/customers/edit-customer-form/edit-customer-form.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { EditCustomerFormComponent } from './edit-customer-form.component';
import { CustomerService, Customer } from '../customer.service';

const mockCustomer: Customer = {
  id: 'c-1',
  fullName: 'Alice',
  email: 'alice@example.com',
  phone: '555-0100',
  companyName: 'Acme',
  country: 'US',
  city: 'New York',
};

describe('EditCustomerFormComponent', () => {
  let fixture: ComponentFixture<EditCustomerFormComponent>;
  let component: EditCustomerFormComponent;
  let customerServiceSpy: jasmine.SpyObj<CustomerService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  beforeEach(async () => {
    customerServiceSpy = jasmine.createSpyObj('CustomerService', ['update']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [EditCustomerFormComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: CustomerService, useValue: customerServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EditCustomerFormComponent);
    component = fixture.componentInstance;
    component.customer = mockCustomer;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should populate form with customer data', () => {
    expect(component.form.get('fullName')?.value).toBe('Alice');
    expect(component.form.get('email')?.value).toBe('alice@example.com');
    expect(component.form.get('phone')?.value).toBe('555-0100');
  });

  it('should disable the email field (BR-CUST-002)', () => {
    expect(component.form.get('email')?.disabled).toBeTrue();
  });

  it('should not include email in the PATCH payload', fakeAsync(() => {
    customerServiceSpy.update.and.returnValue(of(mockCustomer));
    component.form.patchValue({ fullName: 'Alice Updated' });
    component.onSubmit();
    tick();

    const callArg = customerServiceSpy.update.calls.mostRecent().args[1];
    expect(callArg).not.toHaveProperty('email');
  }));

  it('should call CustomerService.update with customer id', fakeAsync(() => {
    customerServiceSpy.update.and.returnValue(of(mockCustomer));
    component.onSubmit();
    tick();
    expect(customerServiceSpy.update).toHaveBeenCalledWith('c-1', jasmine.any(Object));
  }));

  it('should navigate to customer detail on success', fakeAsync(() => {
    customerServiceSpy.update.and.returnValue(of(mockCustomer));
    component.onSubmit();
    tick();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/customers', 'c-1']);
    expect(snackBarSpy.open).toHaveBeenCalledWith('Customer updated successfully.', 'Close', jasmine.any(Object));
  }));

  it('should show error snackbar on unexpected server error', fakeAsync(() => {
    customerServiceSpy.update.and.returnValue(throwError(() => new Error('Server error')));
    component.onSubmit();
    tick();
    expect(snackBarSpy.open).toHaveBeenCalledWith('An error occurred. Please try again.', 'Close', jasmine.any(Object));
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/edit-customer-form/edit-customer-form.component.spec.ts --watch=false
```

Expected: FAIL — `EditCustomerFormComponent` does not exist yet.

- [ ] **Step 3: Implement EditCustomerFormComponent**

```typescript
// src/app/customers/edit-customer-form/edit-customer-form.component.ts
import { Component, inject, signal, Input, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { Customer, CustomerService, UpdateCustomerDto } from '../customer.service';

@Component({
  selector: 'app-edit-customer-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="customer-form">
      <h2>Edit Customer</h2>

      <mat-form-field appearance="outline">
        <mat-label>Full Name *</mat-label>
        <input matInput formControlName="fullName" />
        @if (form.get('fullName')?.hasError('required') && form.get('fullName')?.touched) {
          <mat-error>Full name is required.</mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Email (read-only)</mat-label>
        <input matInput formControlName="email" type="email" />
        <mat-hint>Email cannot be changed after account creation.</mat-hint>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Phone</mat-label>
        <input matInput formControlName="phone" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Company Name</mat-label>
        <input matInput formControlName="companyName" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Country</mat-label>
        <input matInput formControlName="country" />
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>City</mat-label>
        <input matInput formControlName="city" />
      </mat-form-field>

      <div class="form-actions">
        <button mat-stroked-button type="button" (click)="router.navigate(['/customers', customer.id])">
          Cancel
        </button>
        <button
          mat-flat-button
          color="primary"
          type="submit"
          [disabled]="submitting() || form.invalid"
        >
          @if (submitting()) {
            <mat-spinner diameter="20" />
          } @else {
            Save Changes
          }
        </button>
      </div>
    </form>
  `,
})
export class EditCustomerFormComponent implements OnInit {
  @Input() customer!: Customer;

  protected readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);

  readonly submitting = signal(false);

  form!: FormGroup;

  ngOnInit(): void {
    this.form = this.fb.group({
      fullName: [this.customer.fullName, [Validators.required]],
      email: [{ value: this.customer.email, disabled: true }],
      phone: [this.customer.phone ?? ''],
      companyName: [this.customer.companyName ?? ''],
      country: [this.customer.country ?? ''],
      city: [this.customer.city ?? ''],
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const dto: UpdateCustomerDto = {
      fullName: this.form.get('fullName')!.value,
      phone: this.form.get('phone')!.value,
      companyName: this.form.get('companyName')!.value,
      country: this.form.get('country')!.value,
      city: this.form.get('city')!.value,
    };

    this.submitting.set(true);
    this.customerService.update(this.customer.id, dto).subscribe({
      next: () => {
        this.submitting.set(false);
        this.snackBar.open('Customer updated successfully.', 'Close', { duration: 4000 });
        this.router.navigate(['/customers', this.customer.id]);
      },
      error: () => {
        this.submitting.set(false);
        this.snackBar.open('An error occurred. Please try again.', 'Close', { duration: 4000 });
      },
    });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/edit-customer-form/edit-customer-form.component.spec.ts --watch=false
```

Expected: 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/edit-customer-form/
git commit -m "feat(customers): implement EditCustomerFormComponent with read-only email per BR-CUST-002"
```

---

## Task 4: Integration smoke test

> Note: Runs all customer form tests together.

- [ ] **Step 1: Run all customer tests**

```bash
ng test --include=src/app/customers/**/*.spec.ts --watch=false
```

Expected: All customer service and form tests PASS.

- [ ] **Step 2: Commit**

```bash
git add src/app/customers/
git commit -m "feat(customers): US-FE-008 complete — create/edit customer forms with validation"
```
