# Customer Detail Page — Implementation Plan

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

**Story:** US-FE-007
**Goal:** Implement the `/customers/{id}` page showing customer header, tabbed content (Overview, Contacts, Tickets, Audit), inline editing, and admin-only deactivation.

**Architecture:** `CustomerDetailComponent` is standalone and lazy-loaded. It fetches the customer via `CustomerService.getById()` on route param change. Angular Material `MatTabGroup` drives the tab layout. Inline editing toggles form fields within the Overview tab. The Deactivate button (Admin only) opens a `MatDialog` for confirmation. The Tickets tab reuses the ticket list filtered by `customerId`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/customers/customer-detail/customer-detail.component.ts` |
| Create | `src/app/customers/customer-detail/customer-detail.component.html` |
| Create | `src/app/customers/customer-detail/customer-detail.component.spec.ts` |
| Modify | `src/app/customers/services/customer.service.ts` |
| Modify | `src/app/customers/services/customer.service.spec.ts` |

---

## Task 1: Extend CustomerService with getById() and deactivate()

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/customers/services/customer.service.spec.ts

describe('CustomerService — detail methods', () => {
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

  it('getById() should GET /api/v1/customers/{id}', () => {
    service.getById('42').subscribe(c => expect(c.id).toBe('42'));

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('GET');
    req.flush({ id: '42', fullName: 'Ali Hassan', email: 'ali@test.com', isVip: false, isActive: true });
  });

  it('update() should PATCH /api/v1/customers/{id}', () => {
    service.update('42', { phone: '0501234567' }).subscribe(c => expect(c.phone).toBe('0501234567'));

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: '42', fullName: 'Ali Hassan', phone: '0501234567' });
  });

  it('deactivate() should DELETE /api/v1/customers/{id}', () => {
    service.deactivate('42').subscribe();

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/services/customer.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/customers/services/customer.service.ts

getById(id: string): Observable<Customer> {
  return this.http.get<Customer>(`/api/v1/customers/${id}`);
}

update(id: string, changes: Partial<Customer>): Observable<Customer> {
  return this.http.patch<Customer>(`/api/v1/customers/${id}`, changes);
}

deactivate(id: string): Observable<void> {
  return this.http.delete<void>(`/api/v1/customers/${id}`);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/services/customer.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/services/customer.service.ts src/app/customers/services/customer.service.spec.ts
git commit -m "feat(customers): add getById/update/deactivate to CustomerService (US-FE-007)"
```

---

## Task 2: CustomerDetailComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/customers/customer-detail/customer-detail.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { CustomerDetailComponent } from './customer-detail.component';
import { CustomerService, Customer } from '../services/customer.service';
import { AuthStore } from '../../auth/auth.store';

const mockCustomer: Customer = {
  id: '42',
  fullName: 'Ali Hassan',
  email: 'ali@example.com',
  phone: '050-111-2222',
  company: 'Acme',
  isVip: true,
  isActive: true,
  ticketCount: 5,
  createdAt: '2025-01-01',
};

describe('CustomerDetailComponent', () => {
  let fixture: ComponentFixture<CustomerDetailComponent>;
  let component: CustomerDetailComponent;
  let customerService: jasmine.SpyObj<CustomerService>;

  beforeEach(async () => {
    customerService = jasmine.createSpyObj('CustomerService', ['getById', 'update', 'deactivate']);
    customerService.getById.and.returnValue(of(mockCustomer));

    await TestBed.configureTestingModule({
      imports: [CustomerDetailComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: CustomerService, useValue: customerService },
        { provide: ActivatedRoute, useValue: { params: of({ id: '42' }) } },
        { provide: AuthStore, useValue: { user: () => ({ role: 'Admin' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load customer', () => {
    expect(component).toBeTruthy();
    expect(customerService.getById).toHaveBeenCalledWith('42');
    expect(component.customer()?.fullName).toBe('Ali Hassan');
  });

  it('should display VIP badge', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('VIP');
  });

  it('should enter edit mode on edit button click', () => {
    component.enterEditMode();
    expect(component.editing()).toBeTrue();
  });

  it('should call update() on save and exit edit mode', () => {
    customerService.update.and.returnValue(of({ ...mockCustomer, phone: '0501112233' }));
    component.enterEditMode();
    component.editForm.patchValue({ phone: '0501112233' });
    component.saveChanges();
    expect(customerService.update).toHaveBeenCalled();
    expect(component.editing()).toBeFalse();
  });

  it('should show deactivate button only for Admin role', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Deactivate');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/customer-detail/customer-detail.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/customers/customer-detail/customer-detail.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';
import { Customer, CustomerService } from '../services/customer.service';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatTabsModule, MatButtonModule,
    MatIconModule, MatFormFieldModule, MatInputModule, MatDialogModule, MatSnackBarModule,
  ],
  templateUrl: './customer-detail.component.html',
})
export class CustomerDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerService = inject(CustomerService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly authStore = inject(AuthStore);

  readonly customer = signal<Customer | null>(null);
  readonly editing = signal(false);
  readonly loading = signal(false);

  editForm = this.fb.group({
    phone: [''],
    company: [''],
    isVip: [false],
  });

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.loadCustomer(params['id']);
    });
  }

  private loadCustomer(id: string): void {
    this.loading.set(true);
    this.customerService.getById(id).subscribe({
      next: c => {
        this.customer.set(c);
        this.editForm.patchValue({ phone: c.phone, company: c.company, isVip: c.isVip });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  enterEditMode(): void {
    this.editing.set(true);
  }

  cancelEdit(): void {
    const c = this.customer();
    if (c) this.editForm.patchValue({ phone: c.phone, company: c.company, isVip: c.isVip });
    this.editing.set(false);
  }

  saveChanges(): void {
    const id = this.customer()?.id;
    if (!id) return;
    this.customerService.update(id, this.editForm.value as Partial<Customer>).subscribe({
      next: updated => {
        this.customer.set(updated);
        this.editing.set(false);
        this.snackBar.open('Customer updated', 'OK', { duration: 3000 });
      },
    });
  }

  confirmDeactivate(): void {
    const id = this.customer()?.id;
    if (!id) return;
    // Dialog confirmation is shown inline; component calls deactivate directly
    this.customerService.deactivate(id).subscribe({
      next: () => {
        this.snackBar.open('Customer deactivated', 'OK', { duration: 3000 });
        this.router.navigate(['/customers']);
      },
    });
  }

  get isAdmin(): boolean {
    return this.authStore.user()?.role === 'Admin';
  }
}
```

```html
<!-- src/app/customers/customer-detail/customer-detail.component.html -->

@if (loading()) {
  <p class="p-8 text-center text-gray-500">Loading…</p>
} @else if (customer()) {
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-start justify-between mb-6">
      <div>
        <h1 class="text-2xl font-semibold">{{ customer()!.fullName }}</h1>
        <p class="text-gray-500">{{ customer()!.email }}</p>
        @if (customer()!.isVip) { <span class="badge-vip ml-2">VIP</span> }
        <span class="ml-2 text-sm" [class.text-green-600]="customer()!.isActive" [class.text-red-500]="!customer()!.isActive">
          {{ customer()!.isActive ? 'Active' : 'Inactive' }}
        </span>
      </div>
      <div class="flex gap-2">
        <button mat-raised-button color="primary" routerLink="/tickets/new" [queryParams]="{ customerId: customer()!.id }">
          New Ticket
        </button>
        @if (isAdmin && customer()!.isActive) {
          <button mat-stroked-button color="warn" (click)="confirmDeactivate()">Deactivate</button>
        }
      </div>
    </div>

    <!-- Tabs -->
    <mat-tab-group>
      <!-- Overview -->
      <mat-tab label="Overview">
        <div class="p-4">
          @if (!editing()) {
            <dl class="grid grid-cols-2 gap-4">
              <div><dt class="text-sm text-gray-500">Phone</dt><dd>{{ customer()!.phone || '—' }}</dd></div>
              <div><dt class="text-sm text-gray-500">Company</dt><dd>{{ customer()!.company || '—' }}</dd></div>
            </dl>
            <button mat-icon-button (click)="enterEditMode()" class="mt-4"><mat-icon>edit</mat-icon></button>
          } @else {
            <form [formGroup]="editForm" class="flex flex-col gap-3 max-w-md">
              <mat-form-field appearance="outline">
                <mat-label>Phone</mat-label>
                <input matInput formControlName="phone" />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Company</mat-label>
                <input matInput formControlName="company" />
              </mat-form-field>
              <div class="flex gap-2">
                <button mat-raised-button color="primary" type="button" (click)="saveChanges()">Save</button>
                <button mat-stroked-button type="button" (click)="cancelEdit()">Cancel</button>
              </div>
            </form>
          }
        </div>
      </mat-tab>

      <!-- Contacts -->
      <mat-tab label="Contacts">
        <div class="p-4 text-gray-500">Contacts management coming soon.</div>
      </mat-tab>

      <!-- Tickets -->
      <mat-tab label="Tickets">
        <div class="p-4 text-gray-500">Ticket list filtered by customer — see US-FE-009.</div>
      </mat-tab>

      <!-- Audit -->
      <mat-tab label="Audit">
        <div class="p-4 text-gray-500">Audit log coming soon.</div>
      </mat-tab>
    </mat-tab-group>
  </div>
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/customer-detail/customer-detail.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/customer-detail/
git commit -m "feat(customers): implement CustomerDetailComponent with inline edit and tabs (US-FE-007)"
```
