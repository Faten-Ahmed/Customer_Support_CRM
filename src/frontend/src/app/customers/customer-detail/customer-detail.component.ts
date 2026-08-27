import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DatePipe } from '@angular/common';
import { Customer, CustomerService } from '../services/customer.service';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterModule, DatePipe,
    MatTabsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSnackBarModule,
  ],
  templateUrl: './customer-detail.component.html',
  styleUrl: './customer-detail.component.scss',
})
export class CustomerDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly authStore = inject(AuthStore);

  readonly customer = signal<Customer | null>(null);
  readonly editing = signal(false);
  readonly loading = signal(false);

  editForm: FormGroup = this.fb.group({
    phone: [''],
    companyName: [''],
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
        this.editForm.patchValue({ phone: c.phone, companyName: (c as any).companyName });
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
    if (c) this.editForm.patchValue({ phone: c.phone, companyName: (c as any).companyName });
    this.editing.set(false);
  }

  saveChanges(): void {
    const c = this.customer();
    if (!c) return;
    this.customerService.update(c.id, this.editForm.value).subscribe({
      next: updated => {
        this.customer.set(updated);
        this.editing.set(false);
        this.snackBar.open('Customer updated', 'OK', { duration: 3000 });
      },
    });
  }

  confirmDeactivate(): void {
    const c = this.customer();
    if (!c) return;
    this.customerService.deactivate(c.id).subscribe({
      next: () => {
        this.snackBar.open('Customer deactivated', 'OK', { duration: 3000 });
        this.router.navigate(['/app/customers']);
      },
    });
  }

  get isAdmin(): boolean {
    return this.authStore.user()?.role === 'Admin';
  }
}
