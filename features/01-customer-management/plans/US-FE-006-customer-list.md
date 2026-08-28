# Customer List Page — Implementation Plan

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

**Story:** US-FE-006
**Goal:** Implement the `/customers` route — a searchable, filterable, paginated table of all customers with VIP badges, active status, and a "New Customer" button that opens the create form.

**Architecture:** `CustomerListComponent` is a standalone Angular component in `CustomersModule`, lazy-loaded at `/customers`. It uses Angular Signals for filter/search state and `CustomerService.list()` for server-side paginated data. MatTable + MatPaginator handle the grid. Search is debounced 300ms with `rxjs/operators/debounceTime`. VIP and Active filter chips update query params.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/customers/services/customer.service.ts` |
| Create | `src/app/customers/services/customer.service.spec.ts` |
| Create | `src/app/customers/customer-list/customer-list.component.ts` |
| Create | `src/app/customers/customer-list/customer-list.component.html` |
| Create | `src/app/customers/customer-list/customer-list.component.spec.ts` |
| Create | `src/app/customers/customers.routes.ts` |

---

## Task 1: CustomerService.list()

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/customers/services/customer.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CustomerService } from './customer.service';

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

  it('list() should GET /api/v1/customers with query params', () => {
    service.list({ page: 1, pageSize: 20, search: 'Ali', vipOnly: true, activeOnly: false }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('search')).toBe('Ali');
    expect(req.request.params.get('vipOnly')).toBe('true');
    req.flush({ data: [], total: 0, page: 1, pageSize: 20 });
  });

  it('list() should omit undefined search param', () => {
    service.list({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.params.has('search')).toBeFalse();
    req.flush({ data: [], total: 0, page: 1, pageSize: 20 });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/services/customer.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/customers/services/customer.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Customer {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  company?: string;
  isVip: boolean;
  isActive: boolean;
  ticketCount: number;
  createdAt: string;
}

export interface CustomerListQuery {
  page: number;
  pageSize: number;
  search?: string;
  vipOnly?: boolean;
  activeOnly?: boolean;
}

export interface CustomerPage {
  data: Customer[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);

  list(query: CustomerListQuery): Observable<CustomerPage> {
    let params = new HttpParams()
      .set('page', String(query.page))
      .set('pageSize', String(query.pageSize));
    if (query.search) params = params.set('search', query.search);
    if (query.vipOnly !== undefined) params = params.set('vipOnly', String(query.vipOnly));
    if (query.activeOnly !== undefined) params = params.set('activeOnly', String(query.activeOnly));
    return this.http.get<CustomerPage>('/api/v1/customers', { params });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/services/customer.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/services/
git commit -m "feat(customers): add CustomerService.list() (US-FE-006)"
```

---

## Task 2: CustomerListComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/customers/customer-list/customer-list.component.spec.ts

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { CustomerListComponent } from './customer-list.component';
import { CustomerService, CustomerPage } from '../services/customer.service';

const mockPage: CustomerPage = {
  data: [
    { id: '1', fullName: 'Ali Hassan', email: 'ali@example.com', isVip: true, isActive: true, ticketCount: 3, createdAt: '2025-01-01' },
    { id: '2', fullName: 'Sara Omar', email: 'sara@example.com', isVip: false, isActive: false, ticketCount: 0, createdAt: '2025-02-01' },
  ],
  total: 2,
  page: 1,
  pageSize: 20,
};

describe('CustomerListComponent', () => {
  let fixture: ComponentFixture<CustomerListComponent>;
  let component: CustomerListComponent;
  let customerService: jasmine.SpyObj<CustomerService>;

  beforeEach(async () => {
    customerService = jasmine.createSpyObj('CustomerService', ['list']);
    customerService.list.and.returnValue(of(mockPage));

    await TestBed.configureTestingModule({
      imports: [CustomerListComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: CustomerService, useValue: customerService }],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load customers on init', () => {
    expect(component).toBeTruthy();
    expect(customerService.list).toHaveBeenCalled();
    expect(component.customers().length).toBe(2);
  });

  it('should debounce search input', fakeAsync(() => {
    component.searchControl.setValue('Al');
    tick(299);
    expect(customerService.list).toHaveBeenCalledTimes(1);
    tick(1);
    expect(customerService.list).toHaveBeenCalledTimes(2);
  }));

  it('should toggle VIP filter chip', () => {
    component.toggleVipFilter();
    expect(component.vipOnly()).toBeTrue();
    component.toggleVipFilter();
    expect(component.vipOnly()).toBeFalse();
  });

  it('should show empty state when total is 0', () => {
    customerService.list.and.returnValue(of({ ...mockPage, data: [], total: 0 }));
    component.loadCustomers();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No customers found');
  });

  it('should display VIP badge for VIP customers', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('VIP');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/customers/customer-list/customer-list.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/customers/customer-list/customer-list.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { Customer, CustomerService } from '../services/customer.service';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatTableModule, MatPaginatorModule,
    MatInputModule, MatFormFieldModule, MatChipsModule, MatButtonModule,
    MatBadgeModule, MatIconModule,
  ],
  templateUrl: './customer-list.component.html',
})
export class CustomerListComponent implements OnInit {
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);

  readonly searchControl = new FormControl('');
  readonly customers = signal<Customer[]>([]);
  readonly total = signal(0);
  readonly vipOnly = signal(false);
  readonly activeOnly = signal(false);
  readonly loading = signal(false);

  page = 1;
  pageSize = 20;

  displayedColumns = ['fullName', 'email', 'phone', 'company', 'vip', 'active', 'ticketCount', 'createdAt'];

  ngOnInit(): void {
    this.loadCustomers();
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.page = 1;
      this.loadCustomers();
    });
  }

  loadCustomers(): void {
    this.loading.set(true);
    this.customerService.list({
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchControl.value || undefined,
      vipOnly: this.vipOnly() || undefined,
      activeOnly: this.activeOnly() || undefined,
    }).subscribe({
      next: res => {
        this.customers.set(res.data);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  toggleVipFilter(): void {
    this.vipOnly.update(v => !v);
    this.page = 1;
    this.loadCustomers();
  }

  toggleActiveFilter(): void {
    this.activeOnly.update(v => !v);
    this.page = 1;
    this.loadCustomers();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadCustomers();
  }

  navigateToCustomer(id: string): void {
    this.router.navigate(['/customers', id]);
  }
}
```

```html
<!-- src/app/customers/customer-list/customer-list.component.html -->

<div class="p-6">
  <div class="flex items-center justify-between mb-4">
    <h1 class="text-2xl font-semibold">Customers</h1>
    <button mat-raised-button color="primary" routerLink="/customers/new">
      <mat-icon>add</mat-icon> New Customer
    </button>
  </div>

  <div class="flex gap-3 mb-4">
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>Search by name, email, phone or company</mat-label>
      <input matInput [formControl]="searchControl" />
      <mat-icon matSuffix>search</mat-icon>
    </mat-form-field>

    <mat-chip-listbox>
      <mat-chip-option [selected]="vipOnly()" (click)="toggleVipFilter()">VIP Only</mat-chip-option>
      <mat-chip-option [selected]="activeOnly()" (click)="toggleActiveFilter()">Active Only</mat-chip-option>
    </mat-chip-listbox>
  </div>

  @if (loading()) {
    <p class="text-center text-gray-500 py-8">Loading…</p>
  } @else if (customers().length === 0) {
    <p class="text-center text-gray-500 py-8">No customers found</p>
  } @else {
    <mat-table [dataSource]="customers()" class="w-full">
      <ng-container matColumnDef="fullName">
        <mat-header-cell *matHeaderCellDef>Name</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.fullName }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="email">
        <mat-header-cell *matHeaderCellDef>Email</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.email }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="phone">
        <mat-header-cell *matHeaderCellDef>Phone</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.phone || '—' }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="company">
        <mat-header-cell *matHeaderCellDef>Company</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.company || '—' }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="vip">
        <mat-header-cell *matHeaderCellDef>VIP</mat-header-cell>
        <mat-cell *matCellDef="let c">
          @if (c.isVip) { <span class="badge-vip">VIP</span> }
        </mat-cell>
      </ng-container>

      <ng-container matColumnDef="active">
        <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.isActive ? 'Active' : 'Inactive' }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="ticketCount">
        <mat-header-cell *matHeaderCellDef>Tickets</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.ticketCount }}</mat-cell>
      </ng-container>

      <ng-container matColumnDef="createdAt">
        <mat-header-cell *matHeaderCellDef>Created</mat-header-cell>
        <mat-cell *matCellDef="let c">{{ c.createdAt | date:'mediumDate' }}</mat-cell>
      </ng-container>

      <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
      <mat-row *matRowDef="let row; columns: displayedColumns;"
               (click)="navigateToCustomer(row.id)" class="cursor-pointer hover:bg-gray-50">
      </mat-row>
    </mat-table>

    <mat-paginator
      [length]="total()"
      [pageSize]="pageSize"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPageChange($event)">
    </mat-paginator>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/customers/customer-list/customer-list.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/customers/customer-list/ src/app/customers/customers.routes.ts
git commit -m "feat(customers): implement CustomerListComponent with search/filter/pagination (US-FE-006)"
```
