import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../ticket.service';

export interface StatusChangeModalData {
  ticketId: string;
  availableStatuses: string[];
}

@Component({
  selector: 'app-status-change-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Change Status</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="flex flex-col gap-3 pt-2">
        <mat-form-field appearance="outline">
          <mat-label>New Status</mat-label>
          <mat-select formControlName="status">
            @for (s of data.availableStatuses; track s) {
              <mat-option [value]="s">{{ s }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        @if (form.get('status')?.value === 'Resolved') {
          <mat-form-field appearance="outline">
            <mat-label>Resolution Notes</mat-label>
            <textarea matInput formControlName="resolutionText" rows="3"></textarea>
          </mat-form-field>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">Confirm</button>
    </mat-dialog-actions>
  `,
})
export class StatusChangeModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  form = this.fb.group({
    status: ['', Validators.required],
    resolutionText: [''],
  });

  constructor(
    public dialogRef: MatDialogRef<StatusChangeModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: StatusChangeModalData,
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    const { status, resolutionText } = this.form.value;
    this.ticketService.changeStatus(
      this.data.ticketId,
      status!,
      resolutionText || undefined,
    ).subscribe({
      next: () => this.dialogRef.close(true),
    });
  }
}
