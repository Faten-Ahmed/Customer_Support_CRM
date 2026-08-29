import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { of } from 'rxjs';
import { DepartmentService, Department } from './department.service';

@Component({
  selector: 'app-department-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>New Department</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display: flex; flex-direction: column; gap: 12px; min-width: 280px; padding-top: 8px;">
        <mat-form-field>
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Name (Arabic)</mat-label>
          <input matInput formControlName="nameAr" dir="rtl" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" rows="3"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class DepartmentFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<DepartmentFormDialogComponent>);
  private readonly deptService = inject(DepartmentService);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    nameAr: [''],
    description: [''],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.deptService
      .create({ name: v.name, nameAr: v.nameAr || undefined, description: v.description || undefined })
      .subscribe({
        next: result => this.dialogRef.close(result),
        error: () => {},
      });
  }
}

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './department-list.component.html',
})
export class DepartmentListComponent implements OnInit {
  private readonly deptService = inject(DepartmentService);
  private readonly dialog = inject(MatDialog);

  readonly departments = signal<Department[]>([]);
  readonly loading = signal(false);

  readonly displayedColumns = ['name', 'nameAr', 'isActive', 'actions'];

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.deptService.list().subscribe({
      next: res => {
        this.departments.set(res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNewDepartmentDialog(): void {
    const ref = this.dialog.open(DepartmentFormDialogComponent);
    ref.afterClosed().subscribe(result => {
      if (result) this.loadDepartments();
    });
  }

  deactivate(dept: Department): void {
    this.deptService.deactivate(dept.id).subscribe(() => this.loadDepartments());
  }

  reactivate(dept: Department): void {
    this.deptService.reactivate(dept.id).subscribe(() => this.loadDepartments());
  }
}
