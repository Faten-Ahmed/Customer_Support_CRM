import { Component, OnInit, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { map, startWith } from 'rxjs/operators';
import { signal } from '@angular/core';
import { UserService } from './user.service';
import { DepartmentService, Department } from '../departments/department.service';

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
    MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>New User</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display: flex; flex-direction: column; gap: 12px; min-width: 360px; padding-top: 8px;">
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;">
          <mat-form-field>
            <mat-label>First Name</mat-label>
            <input matInput formControlName="firstName" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Last Name</mat-label>
            <input matInput formControlName="lastName" />
          </mat-form-field>
        </div>
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
          </mat-select>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Temporary Password</mat-label>
          <input matInput formControlName="tempPassword" type="password" />
        </mat-form-field>
        @if (!isAdmin()) {
          <mat-form-field>
            <mat-label>Primary Department</mat-label>
            <mat-select formControlName="primaryDepartmentId">
              @if (loadingDepts()) {
                <mat-option disabled>Loading…</mat-option>
              } @else {
                @for (dept of departments(); track dept.id) {
                  <mat-option [value]="dept.id">{{ dept.name }}</mat-option>
                }
              }
            </mat-select>
          </mat-form-field>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class UserFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  private readonly userService = inject(UserService);
  private readonly departmentService = inject(DepartmentService);

  readonly departments = signal<Department[]>([]);
  readonly loadingDepts = signal(false);

  readonly form = this.fb.nonNullable.group({
    firstName:           ['', Validators.required],
    lastName:            ['', Validators.required],
    email:               ['', [Validators.required, Validators.email]],
    role:                ['Agent', Validators.required],
    tempPassword:        ['', Validators.required],
    primaryDepartmentId: ['', Validators.required],
  });

  readonly isAdmin = toSignal(
    this.form.controls.role.valueChanges.pipe(
      startWith(this.form.controls.role.value),
      map(r => r === 'Admin'),
    ),
    { initialValue: false },
  );

  ngOnInit(): void {
    this.loadingDepts.set(true);
    this.departmentService.list().subscribe({
      next: res => {
        this.departments.set((res.data ?? []).filter(d => d.isActive));
        this.loadingDepts.set(false);
      },
      error: () => this.loadingDepts.set(false),
    });

    this.form.controls.role.valueChanges.subscribe(role => {
      const deptCtrl = this.form.controls.primaryDepartmentId;
      if (role === 'Admin') {
        deptCtrl.clearValidators();
        deptCtrl.setValue('');
      } else {
        deptCtrl.setValidators(Validators.required);
      }
      deptCtrl.updateValueAndValidity();
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.userService.create({
      firstName:           v.firstName,
      lastName:            v.lastName,
      email:               v.email,
      role:                v.role,
      tempPassword:        v.tempPassword,
      primaryDepartmentId: v.primaryDepartmentId || undefined,
    }).subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {},
    });
  }
}
