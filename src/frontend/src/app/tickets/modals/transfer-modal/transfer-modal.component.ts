import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../ticket.service';

export interface TransferModalData {
  ticketId: string;
  departments: { id: string; name: string }[];
}

@Component({
  selector: 'app-transfer-modal',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Transfer Ticket</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="flex flex-col gap-3 pt-2">
        <mat-form-field appearance="outline">
          <mat-label>Transfer To</mat-label>
          <mat-select formControlName="departmentId">
            @for (d of data.departments; track d.id) {
              <mat-option [value]="d.id">{{ d.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Transfer Note</mat-label>
          <textarea matInput formControlName="note" rows="3"></textarea>
          @if (form.get('note')?.hasError('minlength')) {
            <mat-error>Minimum 10 characters</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">Transfer</button>
    </mat-dialog-actions>
  `,
})
export class TransferModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  form = this.fb.group({
    departmentId: ['', Validators.required],
    note: ['', [Validators.required, Validators.minLength(10)]],
  });

  constructor(
    public dialogRef: MatDialogRef<TransferModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TransferModalData,
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    const { departmentId, note } = this.form.value as { departmentId: string; note: string };
    this.ticketService.transfer(this.data.ticketId, departmentId, note).subscribe({
      next: () => this.dialogRef.close(true),
    });
  }
}
