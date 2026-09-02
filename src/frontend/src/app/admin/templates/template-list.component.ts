import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { TemplateService, QuickReplyTemplate } from './template.service';

// ── Create dialog ────────────────────────────────────────────────────────────

@Component({
  selector: 'app-template-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>New Global Template</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;min-width:420px;padding-top:8px;">

        <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;">
          <mat-form-field>
            <mat-label>Title</mat-label>
            <input matInput formControlName="title" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>العنوان (Arabic)</mat-label>
            <input matInput formControlName="titleAr" dir="rtl" />
          </mat-form-field>
        </div>

        <mat-form-field>
          <mat-label>Content</mat-label>
          <textarea matInput formControlName="content" rows="4"></textarea>
        </mat-form-field>

        <mat-form-field>
          <mat-label>المحتوى (Arabic)</mat-label>
          <textarea matInput formControlName="contentAr" rows="4" dir="rtl"></textarea>
        </mat-form-field>

      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Create</button>
    </mat-dialog-actions>
  `,
})
export class TemplateFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TemplateFormDialogComponent>);
  private readonly templateService = inject(TemplateService);

  readonly form = this.fb.nonNullable.group({
    title:     ['', Validators.required],
    titleAr:   ['', Validators.required],
    content:   ['', Validators.required],
    contentAr: ['', Validators.required],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.templateService.create({
      title: v.title, titleAr: v.titleAr,
      content: v.content, contentAr: v.contentAr,
    }).subscribe({ next: result => this.dialogRef.close(result), error: () => {} });
  }
}

// ── Edit dialog ──────────────────────────────────────────────────────────────

@Component({
  selector: 'app-template-edit-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Edit Template</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;min-width:420px;padding-top:8px;">

        <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;">
          <mat-form-field>
            <mat-label>Title</mat-label>
            <input matInput formControlName="title" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>العنوان (Arabic)</mat-label>
            <input matInput formControlName="titleAr" dir="rtl" />
          </mat-form-field>
        </div>

        <mat-form-field>
          <mat-label>Content</mat-label>
          <textarea matInput formControlName="content" rows="4"></textarea>
        </mat-form-field>

        <mat-form-field>
          <mat-label>المحتوى (Arabic)</mat-label>
          <textarea matInput formControlName="contentAr" rows="4" dir="rtl"></textarea>
        </mat-form-field>

      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid">Save</button>
    </mat-dialog-actions>
  `,
})
export class TemplateEditDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TemplateEditDialogComponent>);
  private readonly templateService = inject(TemplateService);
  readonly data = inject<QuickReplyTemplate>(MAT_DIALOG_DATA);

  readonly form = this.fb.nonNullable.group({
    title:     [this.data.title,     Validators.required],
    titleAr:   [this.data.titleAr,   Validators.required],
    content:   [this.data.content,   Validators.required],
    contentAr: [this.data.contentAr, Validators.required],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.templateService.update(this.data.id, {
      title: v.title, titleAr: v.titleAr,
      content: v.content, contentAr: v.contentAr,
    }).subscribe({ next: result => this.dialogRef.close(result), error: () => {} });
  }
}

// ── Detail dialog ────────────────────────────────────────────────────────────

@Component({
  selector: 'app-template-detail-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatDividerModule],
  template: `
    <h2 mat-dialog-title>Template Details</h2>
    <mat-dialog-content style="min-width:480px;padding-top:8px;">

      <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:16px;">
        <div>
          <p style="margin:0;font-size:12px;color:#666;">Title</p>
          <p style="margin:4px 0 0;">{{ t.title }}</p>
        </div>
        <div dir="rtl">
          <p style="margin:0;font-size:12px;color:#666;">العنوان</p>
          <p style="margin:4px 0 0;">{{ t.titleAr }}</p>
        </div>
      </div>

      <mat-divider style="margin-bottom:16px;" />

      <div style="margin-bottom:16px;">
        <p style="margin:0;font-size:12px;color:#666;">Content</p>
        <p style="margin:4px 0 0;white-space:pre-wrap;">{{ t.content }}</p>
      </div>

      <mat-divider style="margin-bottom:16px;" />

      <div dir="rtl" style="margin-bottom:16px;">
        <p style="margin:0;font-size:12px;color:#666;">المحتوى</p>
        <p style="margin:4px 0 0;white-space:pre-wrap;">{{ t.contentAr }}</p>
      </div>

      @if (t.category) {
        <mat-divider style="margin-bottom:16px;" />
        <div>
          <p style="margin:0;font-size:12px;color:#666;">Category</p>
          <p style="margin:4px 0 0;">{{ t.category }}</p>
        </div>
      }

      <mat-divider style="margin:16px 0 12px;" />

      <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:8px;font-size:13px;color:#555;">
        <div><span style="color:#999;">Scope</span><br />{{ t.scope }}</div>
        <div><span style="color:#999;">Status</span><br />{{ t.isActive ? 'Active' : 'Inactive' }}</div>
        <div><span style="color:#999;">Created</span><br />{{ t.createdAt | date:'mediumDate' }}</div>
      </div>

    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
})
export class TemplateDetailDialogComponent {
  readonly t = inject<QuickReplyTemplate>(MAT_DIALOG_DATA);
}

// ── List component ───────────────────────────────────────────────────────────

@Component({
  selector: 'app-template-list',
  standalone: true,
  imports: [
    CommonModule, MatTableModule, MatButtonModule, MatIconModule,
    MatDialogModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  template: `
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
      <h2 style="margin:0;">Quick Reply Templates</h2>
      <button mat-flat-button color="primary" (click)="openNewDialog()">
        <mat-icon>add</mat-icon> New Template
      </button>
    </div>

    @if (loading()) {
      <div style="display:flex;justify-content:center;padding:40px;">
        <mat-spinner diameter="40" />
      </div>
    } @else {
      <mat-table [dataSource]="templates()">

        <ng-container matColumnDef="title">
          <mat-header-cell *matHeaderCellDef>Title</mat-header-cell>
          <mat-cell *matCellDef="let t">{{ t.title }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="content">
          <mat-header-cell *matHeaderCellDef>Content</mat-header-cell>
          <mat-cell *matCellDef="let t"
            style="max-width:360px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">
            {{ t.content }}
          </mat-cell>
        </ng-container>

        <ng-container matColumnDef="actions">
          <mat-header-cell *matHeaderCellDef></mat-header-cell>
          <mat-cell *matCellDef="let t">
            <button mat-icon-button (click)="openDetailDialog(t)" matTooltip="Details">
              <mat-icon>visibility</mat-icon>
            </button>
            <button mat-icon-button color="primary" (click)="openEditDialog(t)" matTooltip="Edit">
              <mat-icon>edit</mat-icon>
            </button>
            <button mat-icon-button color="warn" (click)="delete(t)" matTooltip="Delete">
              <mat-icon>delete</mat-icon>
            </button>
          </mat-cell>
        </ng-container>

        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
      </mat-table>

      @if (templates().length === 0) {
        <p style="text-align:center;color:#666;padding:32px;">No global templates yet.</p>
      }
    }
  `,
})
export class TemplateListComponent implements OnInit {
  private readonly templateService = inject(TemplateService);
  private readonly dialog = inject(MatDialog);

  readonly templates = signal<QuickReplyTemplate[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['title', 'content', 'actions'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.templateService.list().subscribe({
      next: res => { this.templates.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openNewDialog(): void {
    this.dialog.open(TemplateFormDialogComponent).afterClosed()
      .subscribe(result => { if (result) this.load(); });
  }

  openEditDialog(t: QuickReplyTemplate): void {
    this.dialog.open(TemplateEditDialogComponent, { data: t }).afterClosed()
      .subscribe(result => { if (result) this.load(); });
  }

  openDetailDialog(t: QuickReplyTemplate): void {
    this.dialog.open(TemplateDetailDialogComponent, { data: t });
  }

  delete(t: QuickReplyTemplate): void {
    if (!confirm(`Delete template "${t.title}"?`)) return;
    this.templateService.delete(t.id).subscribe(() => this.load());
  }
}
