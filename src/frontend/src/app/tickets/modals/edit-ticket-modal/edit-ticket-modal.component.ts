import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin } from 'rxjs';
import { TicketService, TicketDetail } from '../../ticket.service';
import { Department, DepartmentService } from '../../../admin/departments/department.service';
import { Category, CategoryService } from '../../../admin/categories/category.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

export interface EditTicketModalData {
  ticket: TicketDetail;
}

@Component({
  selector: 'app-edit-ticket-modal',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  template: `
    <h2 mat-dialog-title>{{ 'modal.editTicketTitle' | translate }}</h2>
    <mat-dialog-content>
      @if (loading()) {
        <div style="display:flex;justify-content:center;padding:40px">
          <mat-spinner diameter="36" />
        </div>
      } @else {
        <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;padding-top:8px;min-width:480px">

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'modal.subjectLabel' | translate }}</mat-label>
              <input matInput formControlName="subject" />
              <mat-error>{{ 'modal.subjectRequired' | translate }}</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'modal.subjectArLabel' | translate }}</mat-label>
              <input matInput formControlName="subjectAr" dir="rtl" />
              <mat-error>{{ 'modal.subjectArRequired' | translate }}</mat-error>
            </mat-form-field>
          </div>

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'modal.descriptionLabel' | translate }}</mat-label>
              <textarea matInput formControlName="description" rows="4"></textarea>
              <mat-error>{{ 'modal.descriptionRequired' | translate }}</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'modal.descriptionArLabel' | translate }}</mat-label>
              <textarea matInput formControlName="descriptionAr" rows="4" dir="rtl"></textarea>
              <mat-error>{{ 'modal.descriptionArRequired' | translate }}</mat-error>
            </mat-form-field>
          </div>

          <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'common.department' | translate }}</mat-label>
              <mat-select formControlName="departmentId">
                <mat-option value="">{{ 'modal.deptNone' | translate }}</mat-option>
                @for (d of departments; track d.id) {
                  <mat-option [value]="d.id">{{ d.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'modal.categoryLabel' | translate }}</mat-label>
              <mat-select formControlName="categoryId">
                <mat-option value="">{{ 'modal.deptNone' | translate }}</mat-option>
                @for (c of categories; track c.id) {
                  <mat-option [value]="c.id">{{ c.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>{{ 'modal.priorityLabel' | translate }}</mat-label>
            <mat-select formControlName="priority">
              <mat-option value="Low">{{ 'ticket.priorityLow' | translate }}</mat-option>
              <mat-option value="Medium">{{ 'ticket.priorityMedium' | translate }}</mat-option>
              <mat-option value="High">{{ 'ticket.priorityHigh' | translate }}</mat-option>
              <mat-option value="Critical">{{ 'ticket.priorityCritical' | translate }}</mat-option>
            </mat-select>
          </mat-form-field>

        </form>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || saving() || loading()" (click)="onSubmit()">
        {{ saving() ? ('modal.saving' | translate) : ('modal.saveChanges' | translate) }}
      </button>
    </mat-dialog-actions>
  `,
})
export class EditTicketModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);
  private readonly departmentService = inject(DepartmentService);
  private readonly categoryService = inject(CategoryService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  departments: Department[] = [];
  categories: Category[] = [];

  form = this.fb.group({
    subject:       ['', [Validators.required, Validators.maxLength(500)]],
    subjectAr:     ['', [Validators.required, Validators.maxLength(500)]],
    description:   ['', Validators.required],
    descriptionAr: ['', Validators.required],
    departmentId:  [''],
    categoryId:    [''],
    priority:      ['', Validators.required],
  });

  constructor(
    public dialogRef: MatDialogRef<EditTicketModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EditTicketModalData,
  ) {}

  ngOnInit(): void {
    forkJoin({
      depts: this.departmentService.list(),
      cats: this.categoryService.list(),
    }).subscribe({
      next: ({ depts, cats }) => {
        this.departments = (depts.data ?? []).filter(d => d.isActive);
        this.categories = this.flattenCategories(cats.data ?? []).filter(c => c.isActive);
        const t = this.data.ticket;
        this.form.patchValue({
          subject:       t.subject,
          subjectAr:     t.subjectAr ?? '',
          description:   t.description,
          descriptionAr: t.descriptionAr ?? '',
          departmentId:  t.departmentId ?? '',
          categoryId:    t.categoryId ?? '',
          priority:      t.priority,
        });
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const { subject, subjectAr, description, descriptionAr, departmentId, categoryId, priority } = this.form.value;
    this.ticketService.update(this.data.ticket.id, {
      subject: subject!,
      subjectAr: subjectAr!,
      description: description!,
      descriptionAr: descriptionAr!,
      departmentId: departmentId || undefined,
      categoryId: categoryId || undefined,
      priority: priority!,
    }).subscribe({
      next: (updated) => this.dialogRef.close(updated),
      error: () => { this.saving.set(false); },
    });
  }

  private flattenCategories(cats: Category[]): Category[] {
    return cats.flatMap(c => [c, ...(c.children ? this.flattenCategories(c.children) : [])]);
  }
}
