// src/app/customers/edit-customer-form/edit-customer-form.component.ts
import { Component, inject, signal, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Customer, CustomerService, UpdateCustomerDto } from '../services/customer.service';

@Component({
  selector: 'app-edit-customer-form',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule, MatSnackBarModule,
  ],
  template: `
    <div class="form-container">
      <h2>Edit Customer</h2>
      @if (customer) {
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Full Name *</mat-label>
            <input matInput formControlName="fullName" />
            @if (form.get('fullName')?.hasError('required') && form.get('fullName')?.touched) {
              <mat-error>Full name is required.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Full Name (Arabic) *</mat-label>
            <input matInput formControlName="fullNameAr" dir="rtl" />
            @if (form.get('fullNameAr')?.hasError('required') && form.get('fullNameAr')?.touched) {
              <mat-error>Full name in Arabic is required.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Email (read-only)</mat-label>
            <input matInput formControlName="email" type="email" />
            <mat-hint>Email cannot be changed after account creation.</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Phone</mat-label>
            <input matInput formControlName="phone" />
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Company Name</mat-label>
            <input matInput formControlName="companyName" />
          </mat-form-field>

          <div class="form-actions">
            <button mat-stroked-button type="button" (click)="router.navigate(['/app/customers', customer!.id])">Cancel</button>
            <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid">
              @if (submitting()) { <mat-spinner diameter="20" /> } @else { Save Changes }
            </button>
          </div>
        </form>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .form-container { padding: 24px; max-width: 560px; }
    .full-width { width: 100%; }
    .form-actions { display: flex; gap: 8px; margin-top: 8px; }
  `],
})
export class EditCustomerFormComponent implements OnInit {
  @Input() customer: Customer | null = null;

  protected readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);

  readonly submitting = signal(false);
  form!: FormGroup;

  ngOnInit(): void {
    if (!this.customer) {
      const id = this.route.snapshot.paramMap.get('id');
      if (id) {
        this.customerService.getById(id).subscribe(c => {
          this.customer = c;
          this.buildForm(c);
        });
      }
    } else {
      this.buildForm(this.customer);
    }
  }

  private buildForm(customer: Customer): void {
    this.form = this.fb.group({
      fullName: [customer.fullName, [Validators.required]],
      fullNameAr: [customer.fullNameAr ?? '', [Validators.required]],
      email: [{ value: customer.email, disabled: true }],
      phone: [customer.phone ?? ''],
      companyName: [customer.companyName ?? ''],
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const dto: UpdateCustomerDto = {
      fullName: this.form.get('fullName')!.value,
      fullNameAr: this.form.get('fullNameAr')!.value,
      phone: this.form.get('phone')!.value,
      companyName: this.form.get('companyName')!.value,
    };

    this.submitting.set(true);
    this.customerService.update(this.customer!.id, dto).subscribe({
      next: () => {
        this.submitting.set(false);
        this.snackBar.open('Customer updated successfully.', 'Close', { duration: 4000 });
        this.router.navigate(['/app/customers', this.customer!.id]);
      },
      error: () => {
        this.submitting.set(false);
        this.snackBar.open('An error occurred. Please try again.', 'Close', { duration: 4000 });
      },
    });
  }
}
