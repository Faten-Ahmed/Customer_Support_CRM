import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CategoryService, Category } from './category.service';

export interface CategoryDialogData {
  parents: Category[];
  preSelectedParentId?: string;
}

@Component({
  selector: 'app-category-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.preSelectedParentId ? 'New Sub-category' : 'New Category' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;min-width:300px;padding-top:8px;">

        @if (!data.preSelectedParentId) {
          <mat-form-field>
            <mat-label>Parent Category (optional)</mat-label>
            <mat-select formControlName="parentId">
              <mat-option [value]="null">— None (root category) —</mat-option>
              @for (p of data.parents; track p.id) {
                <mat-option [value]="p.id">{{ p.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        } @else {
          <p style="margin:0;font-size:13px;color:#555;">
            Adding child under: <strong>{{ parentName }}</strong>
          </p>
        }

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
  readonly data = inject<CategoryDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CategoryFormDialogComponent>);
  private readonly categoryService = inject(CategoryService);

  readonly parentName = this.data.parents.find(p => p.id === this.data.preSelectedParentId)?.name ?? '';

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    nameAr: [''],
    parentId: [this.data.preSelectedParentId ?? null as string | null],
    sortOrder: [0],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.categoryService.create({
      name: v.name,
      nameAr: v.nameAr || undefined,
      parentId: v.parentId ?? undefined,
      sortOrder: v.sortOrder,
    }).subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {},
    });
  }
}

@Component({
  selector: 'app-category-tree',
  standalone: true,
  imports: [
    CommonModule, MatButtonModule, MatIconModule,
    MatDialogModule, MatProgressSpinnerModule, MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './category-tree.component.html',
})
export class CategoryTreeComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(MatDialog);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.categoryService.list().subscribe({
      next: res => {
        const flat = res.data ?? [];
        const map = new Map(flat.map(c => [c.id, { ...c, children: [] as Category[] }]));
        const roots: Category[] = [];
        map.forEach(cat => {
          if (cat.parentCategoryId) {
            map.get(cat.parentCategoryId)?.children?.push(cat);
          } else {
            roots.push(cat);
          }
        });
        this.categories.set(roots);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNewRootDialog(): void {
    this.dialog.open(CategoryFormDialogComponent, {
      data: { parents: this.categories() } satisfies CategoryDialogData,
    }).afterClosed().subscribe(result => { if (result) this.load(); });
  }

  openNewChildDialog(parent: Category): void {
    this.dialog.open(CategoryFormDialogComponent, {
      data: { parents: this.categories(), preSelectedParentId: parent.id } satisfies CategoryDialogData,
    }).afterClosed().subscribe(result => { if (result) this.load(); });
  }

  deactivate(cat: Category): void {
    this.categoryService.deactivate(cat.id).subscribe(() => this.load());
  }

  reactivate(cat: Category): void {
    this.categoryService.reactivate(cat.id).subscribe(() => this.load());
  }
}
