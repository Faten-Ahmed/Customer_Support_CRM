import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CategoryService, Category } from './category.service';

@Component({
  selector: 'app-category-form-dialog',
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
    <h2 mat-dialog-title>New Category</h2>
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
          <mat-label>Sort Order</mat-label>
          <input matInput formControlName="sortOrder" type="number" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class CategoryFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CategoryFormDialogComponent>);
  private readonly categoryService = inject(CategoryService);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    nameAr: [''],
    sortOrder: [0, Validators.required],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.categoryService
      .create({ name: v.name, nameAr: v.nameAr || undefined, sortOrder: v.sortOrder })
      .subscribe({
        next: result => this.dialogRef.close(result),
        error: () => {},
      });
  }
}

@Component({
  selector: 'app-category-tree',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './category-tree.component.html',
})
export class CategoryTreeComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(MatDialog);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.categoryService.list().subscribe({
      next: res => {
        this.categories.set(this.buildTree(res.data ?? []));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private buildTree(flat: Category[]): Category[] {
    const map = new Map<string, Category>();
    const roots: Category[] = [];
    flat.forEach(c => map.set(c.id, { ...c, children: [] }));
    map.forEach(c => {
      if (c.parentCategoryId) {
        const parent = map.get(c.parentCategoryId);
        if (parent) {
          parent.children = parent.children ?? [];
          parent.children.push(c);
        } else {
          roots.push(c);
        }
      } else {
        roots.push(c);
      }
    });
    return roots;
  }

  openNewCategoryDialog(): void {
    const ref = this.dialog.open(CategoryFormDialogComponent);
    ref.afterClosed().subscribe(result => {
      if (result) this.loadCategories();
    });
  }

  deactivate(cat: Category): void {
    this.categoryService.deactivate(cat.id).subscribe(() => this.loadCategories());
  }

  reactivate(cat: Category): void {
    this.categoryService.reactivate(cat.id).subscribe(() => this.loadCategories());
  }
}
