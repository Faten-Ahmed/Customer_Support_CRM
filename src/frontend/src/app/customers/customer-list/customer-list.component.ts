import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Customer, CustomerService } from '../services/customer.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterModule,
    MatTableModule,
    MatPaginatorModule,
    MatInputModule,
    MatFormFieldModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    TranslatePipe,
  ],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss',
})
export class CustomerListComponent implements OnInit, OnDestroy {
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();

  readonly searchControl = new FormControl('');
  readonly customers = signal<Customer[]>([]);
  readonly total = signal(0);
  readonly vipOnly = signal(false);
  readonly activeOnly = signal(false);
  readonly loading = signal(false);

  page = 1;
  pageSize = 20;
  displayedColumns = ['fullName', 'email', 'phone', 'vip', 'status', 'createdAt'];

  ngOnInit(): void {
    this.loadCustomers();
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(() => {
      this.page = 1;
      this.loadCustomers();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCustomers(): void {
    this.loading.set(true);
    this.customerService.list({
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchControl.value || undefined,
      isVip: this.vipOnly() || undefined,
      isActive: this.activeOnly() || undefined,
    }).subscribe({
      next: res => {
        this.customers.set(res.items);
        this.total.set(res.meta.totalCount);
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
    this.router.navigate(['/app/customers', id]);
  }
}
