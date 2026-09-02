import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BranchService, Branch } from './branch.service';

@Component({
  selector: 'app-branch-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>New Branch</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;min-width:280px;padding-top:8px;">
        <mat-form-field>
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Name (Arabic)</mat-label>
          <input matInput formControlName="nameAr" dir="rtl" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class BranchFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<BranchFormDialogComponent>);
  private readonly branchService = inject(BranchService);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    nameAr: [''],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.branchService.create({ name: v.name, nameAr: v.nameAr || undefined })
      .subscribe({ next: result => this.dialogRef.close(result), error: () => {} });
  }
}

@Component({
  selector: 'app-branch-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, MatChipsModule, MatProgressSpinnerModule],
  template: `
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
      <h2 style="margin:0;">Branches</h2>
      <button mat-flat-button color="primary" (click)="openNewDialog()">
        <mat-icon>add</mat-icon> New Branch
      </button>
    </div>
    @if (loading()) {
      <div style="display:flex;justify-content:center;padding:40px;">
        <mat-spinner diameter="40" />
      </div>
    } @else {
      <mat-table [dataSource]="branches()">
        <ng-container matColumnDef="name">
          <mat-header-cell *matHeaderCellDef>Name</mat-header-cell>
          <mat-cell *matCellDef="let b">{{ b.name }}</mat-cell>
        </ng-container>
        <ng-container matColumnDef="nameAr">
          <mat-header-cell *matHeaderCellDef>Name (AR)</mat-header-cell>
          <mat-cell *matCellDef="let b" dir="rtl">{{ b.nameAr || '—' }}</mat-cell>
        </ng-container>
        <ng-container matColumnDef="status">
          <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
          <mat-cell *matCellDef="let b">
            <mat-chip [color]="b.isActive ? 'primary' : 'warn'" highlighted>
              {{ b.isActive ? 'Active' : 'Inactive' }}
            </mat-chip>
          </mat-cell>
        </ng-container>
        <ng-container matColumnDef="actions">
          <mat-header-cell *matHeaderCellDef></mat-header-cell>
          <mat-cell *matCellDef="let b">
            @if (b.isActive) {
              <button mat-icon-button color="warn" (click)="deactivate(b)" matTooltip="Deactivate">
                <mat-icon>block</mat-icon>
              </button>
            } @else {
              <button mat-icon-button color="primary" (click)="reactivate(b)" matTooltip="Reactivate">
                <mat-icon>check_circle</mat-icon>
              </button>
            }
          </mat-cell>
        </ng-container>
        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
      </mat-table>
    }
  `,
})
export class BranchListComponent implements OnInit {
  private readonly branchService = inject(BranchService);
  private readonly dialog = inject(MatDialog);

  readonly branches = signal<Branch[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'nameAr', 'status', 'actions'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.branchService.list().subscribe({
      next: res => { this.branches.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openNewDialog(): void {
    this.dialog.open(BranchFormDialogComponent).afterClosed()
      .subscribe(result => { if (result) this.load(); });
  }

  deactivate(b: Branch): void {
    this.branchService.deactivate(b.id).subscribe(() => this.load());
  }

  reactivate(b: Branch): void {
    this.branchService.reactivate(b.id).subscribe(() => this.load());
  }
}
