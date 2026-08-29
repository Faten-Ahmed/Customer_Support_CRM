import { Component, OnInit, NgZone, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin, of } from 'rxjs';
import { UserService, UserSummary } from './user.service';
import { DepartmentService, Department } from '../departments/department.service';
import { CategoryService, Category } from '../categories/category.service';

@Component({
  selector: 'app-user-edit-dialog',
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
    <h2 mat-dialog-title>Edit User</h2>

    @if (loading()) {
      <mat-dialog-content style="min-width:400px;display:flex;justify-content:center;padding:40px;">
        <mat-spinner diameter="36" />
      </mat-dialog-content>
    } @else {
      <mat-dialog-content>
        <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;min-width:360px;padding-top:8px;">
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
            <input matInput formControlName="email" readonly />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Role</mat-label>
            <input matInput formControlName="role" readonly />
          </mat-form-field>
          @if (!isAdmin()) {
            <mat-form-field>
              <mat-label>Primary Department</mat-label>
              <mat-select formControlName="primaryDepartmentId">
                <mat-option value="">— None —</mat-option>
                @for (dept of departments(); track dept.id) {
                  <mat-option [value]="dept.id">{{ dept.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <mat-form-field>
              <mat-label>Skills (Ticket Categories)</mat-label>
              <mat-select formControlName="skillCategoryIds" multiple>
                @for (cat of categories(); track cat.id) {
                  <mat-option [value]="cat.id">{{ cat.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          }
        </form>
      </mat-dialog-content>
    }

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary"
              (click)="submit()" [disabled]="form.invalid || loading()">Save</button>
    </mat-dialog-actions>
  `,
})
export class UserEditDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<UserEditDialogComponent>);
  private readonly userService = inject(UserService);
  private readonly departmentService = inject(DepartmentService);
  private readonly categoryService = inject(CategoryService);
  private readonly zone = inject(NgZone);
  readonly user = inject<UserSummary>(MAT_DIALOG_DATA);

  readonly loading = signal(true);
  readonly isAdmin = signal(false);
  readonly departments = signal<Department[]>([]);
  readonly categories = signal<Category[]>([]);

  readonly form = this.fb.nonNullable.group({
    firstName:           ['', Validators.required],
    lastName:            ['', Validators.required],
    email:               [{ value: '', disabled: true }],
    role:                [{ value: '', disabled: true }],
    primaryDepartmentId: [''],
    skillCategoryIds:    [[] as string[]],
  });

  ngOnInit(): void {
    forkJoin({
      detail: this.userService.getById(this.user.id),
      depts:  this.departmentService.list(),
      cats:   this.categoryService.list(),
    }).subscribe({
      next: ({ detail, depts, cats }) => {
        const d = detail.data;
        const admin = d.role === 'Admin';
        const primaryId = d.departments.find(dep => dep.isPrimary)?.departmentId ?? '';
        const skillIds = d.skills.map(s => s.categoryId);
        const allCats = this.flattenCategories(cats.data ?? []).filter(c => c.isActive);

        this.zone.run(() => {
          this.departments.set((depts.data ?? []).filter(dep => dep.isActive));
          this.categories.set(allCats);
          this.form.patchValue({
            firstName:           d.firstName,
            lastName:            d.lastName,
            email:               d.email,
            role:                d.role,
            primaryDepartmentId: primaryId,
            skillCategoryIds:    skillIds,
          });
          this.isAdmin.set(admin);
          this.loading.set(false);
        });
      },
      error: () => this.zone.run(() => this.loading.set(false)),
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();

    const profile$ = this.userService.update(this.user.id, {
      firstName: v.firstName,
      lastName:  v.lastName,
    });

    const dept$ = !this.isAdmin() && v.primaryDepartmentId
      ? this.userService.updateDepartments(this.user.id, [{ departmentId: v.primaryDepartmentId, isPrimary: true }])
      : of(null);

    const skills$ = !this.isAdmin()
      ? this.userService.updateSkills(this.user.id, v.skillCategoryIds)
      : of(null);

    forkJoin([profile$, dept$, skills$]).subscribe({
      next: () => this.dialogRef.close(true),
      error: () => {},
    });
  }

  private flattenCategories(cats: Category[]): Category[] {
    return cats.flatMap(c => [c, ...(c.children ? this.flattenCategories(c.children) : [])]);
  }
}
