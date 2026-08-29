import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TicketService } from '../../ticket.service';
import { Department, DepartmentService } from '../../../admin/departments/department.service';

export interface TransferModalData {
  ticketId: string;
}

@Component({
  selector: 'app-transfer-modal',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Transfer Ticket to Department</h2>
    <mat-dialog-content>
      @if (loading()) {
        <div style="display:flex;justify-content:center;padding:40px">
          <mat-spinner diameter="36" />
        </div>
      } @else {
        <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;padding-top:8px;min-width:400px">
          <mat-form-field appearance="outline">
            <mat-label>Target Department</mat-label>
            <mat-select formControlName="departmentId">
              <mat-option value="">— Select department —</mat-option>
              @for (d of departments; track d.id) {
                <mat-option [value]="d.id">{{ d.name }}</mat-option>
              }
            </mat-select>
            <mat-error>Department is required</mat-error>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Transfer Note</mat-label>
            <textarea matInput formControlName="transferNote" rows="3" placeholder="Min 10 characters"></textarea>
            <mat-error>{{ form.get('transferNote')?.hasError('minlength') ? 'Minimum 10 characters' : 'Transfer note is required' }}</mat-error>
          </mat-form-field>
          @if (errorMessage()) {
            <p style="color:#c62828;font-size:13px;margin:0">{{ errorMessage() }}</p>
          }
        </form>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || saving() || loading()" (click)="onSubmit()">
        {{ saving() ? 'Transferring…' : 'Transfer' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class TransferModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);
  private readonly departmentService = inject(DepartmentService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal('');

  departments: Department[] = [];

  form = this.fb.group({
    departmentId: ['', Validators.required],
    transferNote: ['', [Validators.required, Validators.minLength(10)]],
  });

  constructor(
    public dialogRef: MatDialogRef<TransferModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TransferModalData,
  ) {}

  ngOnInit(): void {
    this.departmentService.list().subscribe({
      next: res => {
        this.departments = (res.data ?? []).filter(d => d.isActive);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMessage.set('');
    const { departmentId, transferNote } = this.form.value;
    this.ticketService.transfer(this.data.ticketId, departmentId!, transferNote!).subscribe({
      next: () => this.dialogRef.close(true),
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err?.error?.error ?? 'Transfer failed. Please try again.');
      },
    });
  }
}
