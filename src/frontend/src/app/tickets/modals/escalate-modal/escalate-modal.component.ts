import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../ticket.service';

export interface EscalateModalData {
  ticketId: string;
}

@Component({
  selector: 'app-escalate-modal',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Escalate Ticket</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="pt-2">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Escalation Reason</mat-label>
          <textarea matInput formControlName="reason" rows="4"></textarea>
          @if (form.get('reason')?.hasError('required')) {
            <mat-error>Reason is required</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="warn" [disabled]="form.invalid" (click)="onSubmit()">Escalate</button>
    </mat-dialog-actions>
  `,
})
export class EscalateModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  form = this.fb.group({ reason: ['', Validators.required] });

  constructor(
    public dialogRef: MatDialogRef<EscalateModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EscalateModalData,
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    this.ticketService.escalate(this.data.ticketId, this.form.value.reason!).subscribe({
      next: () => this.dialogRef.close(true),
    });
  }
}
