import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { FieldDefinitionService, FieldDefinition, FieldType } from './field-definition.service';

const FIELD_TYPES: FieldType[] = ['Text', 'Number', 'Date', 'Dropdown', 'Checkbox'];

@Component({
  selector: 'app-field-definition-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatCheckboxModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>New Field Definition</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;min-width:320px;padding-top:8px;">
        <mat-form-field>
          <mat-label>Department ID</mat-label>
          <input matInput formControlName="departmentId" placeholder="Paste department UUID" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Field Name</mat-label>
          <input matInput formControlName="fieldName" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Field Name (Arabic)</mat-label>
          <input matInput formControlName="fieldNameAr" dir="rtl" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Field Type</mat-label>
          <mat-select formControlName="fieldType">
            @for (t of fieldTypes; track t) {
              <mat-option [value]="t">{{ t }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        @if (form.value.fieldType === 'Dropdown') {
          <mat-form-field>
            <mat-label>Options (comma-separated)</mat-label>
            <input matInput formControlName="optionsRaw" />
          </mat-form-field>
        }
        <mat-checkbox formControlName="isRequired">Required</mat-checkbox>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class FieldDefinitionFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<FieldDefinitionFormDialogComponent>);
  private readonly fieldDefService = inject(FieldDefinitionService);

  readonly fieldTypes = FIELD_TYPES;

  readonly form = this.fb.nonNullable.group({
    departmentId: ['', Validators.required],
    fieldName: ['', Validators.required],
    fieldNameAr: [''],
    fieldType: ['Text' as FieldType, Validators.required],
    optionsRaw: [''],
    isRequired: [false],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const options = v.fieldType === 'Dropdown' && v.optionsRaw
      ? v.optionsRaw.split(',').map(s => s.trim()).filter(Boolean)
      : undefined;
    this.fieldDefService.create({
      departmentId: v.departmentId,
      fieldName: v.fieldName,
      fieldNameAr: v.fieldNameAr || undefined,
      fieldType: v.fieldType,
      options,
      isRequired: v.isRequired,
      sortOrder: 0,
    }).subscribe({ next: result => this.dialogRef.close(result), error: () => {} });
  }
}

@Component({
  selector: 'app-field-definition-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, MatChipsModule, MatProgressSpinnerModule],
  template: `
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
      <h2 style="margin:0;">Field Definitions</h2>
      <button mat-flat-button color="primary" (click)="openNewDialog()">
        <mat-icon>add</mat-icon> New Field
      </button>
    </div>
    @if (loading()) {
      <div style="display:flex;justify-content:center;padding:40px;">
        <mat-spinner diameter="40" />
      </div>
    } @else {
      <mat-table [dataSource]="fields()">
        <ng-container matColumnDef="fieldName">
          <mat-header-cell *matHeaderCellDef>Field Name</mat-header-cell>
          <mat-cell *matCellDef="let f">{{ f.fieldName }}</mat-cell>
        </ng-container>
        <ng-container matColumnDef="fieldNameAr">
          <mat-header-cell *matHeaderCellDef>Name (AR)</mat-header-cell>
          <mat-cell *matCellDef="let f" dir="rtl">{{ f.fieldNameAr || '—' }}</mat-cell>
        </ng-container>
        <ng-container matColumnDef="fieldType">
          <mat-header-cell *matHeaderCellDef>Type</mat-header-cell>
          <mat-cell *matCellDef="let f">{{ f.fieldType }}</mat-cell>
        </ng-container>
        <ng-container matColumnDef="isRequired">
          <mat-header-cell *matHeaderCellDef>Required</mat-header-cell>
          <mat-cell *matCellDef="let f">{{ f.isRequired ? 'Yes' : 'No' }}</mat-cell>
        </ng-container>
        <ng-container matColumnDef="status">
          <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
          <mat-cell *matCellDef="let f">
            <mat-chip [color]="f.isActive ? 'primary' : 'warn'" highlighted>
              {{ f.isActive ? 'Active' : 'Inactive' }}
            </mat-chip>
          </mat-cell>
        </ng-container>
        <ng-container matColumnDef="actions">
          <mat-header-cell *matHeaderCellDef></mat-header-cell>
          <mat-cell *matCellDef="let f">
            @if (f.isActive) {
              <button mat-icon-button color="warn" (click)="deactivate(f)" matTooltip="Deactivate">
                <mat-icon>block</mat-icon>
              </button>
            }
          </mat-cell>
        </ng-container>
        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
      </mat-table>
      @if (fields().length === 0) {
        <p style="text-align:center;color:#666;padding:32px;">No field definitions yet. Use filters or create one.</p>
      }
    }
  `,
})
export class FieldDefinitionListComponent implements OnInit {
  private readonly fieldDefService = inject(FieldDefinitionService);
  private readonly dialog = inject(MatDialog);

  readonly fields = signal<FieldDefinition[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['fieldName', 'fieldNameAr', 'fieldType', 'isRequired', 'status', 'actions'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.fieldDefService.list().subscribe({
      next: res => { this.fields.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openNewDialog(): void {
    this.dialog.open(FieldDefinitionFormDialogComponent).afterClosed()
      .subscribe(result => { if (result) this.load(); });
  }

  deactivate(f: FieldDefinition): void {
    this.fieldDefService.deactivate(f.id).subscribe(() => this.load());
  }
}
