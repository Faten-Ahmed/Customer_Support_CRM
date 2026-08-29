// src/app/customers/create-customer-form/create-customer-form.component.ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CustomerService, CreateCustomerDto } from '../services/customer.service';

@Component({
  selector: 'app-create-customer-form',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule, MatSnackBarModule,
  ],
  template: `
    <div class="form-container">
      <h2>New Customer</h2>
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

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Phone</mat-label>
          <input matInput formControlName="phone" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Company Name</mat-label>
          <input matInput formControlName="companyName" />
        </mat-form-field>

        <div class="form-actions">
          <button mat-stroked-button type="button" (click)="router.navigate(['/app/customers'])">Cancel</button>
          <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid">
            @if (submitting()) { <mat-spinner diameter="20" /> } @else { Create Customer }
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .form-container { padding: 24px; max-width: 560px; }
    .full-width { width: 100%; }
    .form-actions { display: flex; gap: 8px; margin-top: 8px; }
  `],
})
export class CreateCustomerFormComponent {
  protected readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);

  readonly submitting = signal(false);

  readonly form: FormGroup = this.fb.group({
    fullName: ['', [Validators.required]],
    fullNameAr: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    companyName: [''],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const { fullName, fullNameAr, email, phone, companyName } = this.form.getRawValue();
    const dto: CreateCustomerDto = { fullName, fullNameAr, email, ...(phone ? { phone } : {}), ...(companyName ? { companyName } : {}) };
    this.customerService.create(dto).subscribe({
      next: customer => {
        this.submitting.set(false);
        this.snackBar.open('Customer created successfully.', 'Close', { duration: 4000 });
        this.router.navigate(['/app/customers', customer.id]);
      },
      error: (err: { code?: string }) => {
        this.submitting.set(false);
        if (err?.code === 'EMAIL_ALREADY_EXISTS') {
          this.form.get('email')!.setErrors({ emailAlreadyExists: true });
        }
      },
    });
  }
}
