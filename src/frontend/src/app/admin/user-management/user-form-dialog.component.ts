import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { UserService } from './user.service';

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>New User</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display: flex; flex-direction: column; gap: 12px; min-width: 320px; padding-top: 8px;">
        <mat-form-field>
          <mat-label>First Name</mat-label>
          <input matInput formControlName="firstName" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Last Name</mat-label>
          <input matInput formControlName="lastName" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Email</mat-label>
          <input matInput formControlName="email" type="email" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Role</mat-label>
          <mat-select formControlName="role">
            <mat-option value="Admin">Admin</mat-option>
            <mat-option value="Manager">Manager</mat-option>
            <mat-option value="Agent">Agent</mat-option>
            <mat-option value="Customer">Customer</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Temporary Password</mat-label>
          <input matInput formControlName="tempPassword" type="password" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Primary Department ID</mat-label>
          <input matInput formControlName="primaryDepartmentId" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class UserFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  private readonly userService = inject(UserService);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    role: ['Agent', Validators.required],
    tempPassword: ['', Validators.required],
    primaryDepartmentId: ['', Validators.required],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.userService
      .create({
        firstName: v.firstName,
        lastName: v.lastName,
        email: v.email,
        role: v.role,
        tempPassword: v.tempPassword,
        primaryDepartmentId: v.primaryDepartmentId,
      })
      .subscribe({
        next: result => this.dialogRef.close(result),
        error: () => {},
      });
  }
}
