import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../ticket.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

export interface EscalateModalData {
  ticketId: string;
}

@Component({
  selector: 'app-escalate-modal',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, TranslatePipe],
  template: `
    <h2 mat-dialog-title>{{ 'modal.escalateTitle' | translate }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="padding-top:8px;min-width:400px">
        <mat-form-field appearance="outline" style="width:100%">
          <mat-label>{{ 'modal.escalateReason' | translate }}</mat-label>
          <textarea matInput formControlName="reason" rows="4" [placeholder]="'modal.escalatePlaceholder' | translate"></textarea>
          <mat-error>{{ 'modal.reasonRequired' | translate }}</mat-error>
        </mat-form-field>
        @if (errorMessage) {
          <p style="color:#c62828;font-size:13px;margin:8px 0 0">{{ errorMessage }}</p>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="warn" [disabled]="form.invalid || saving" (click)="onSubmit()">
        {{ saving ? ('modal.escalating' | translate) : ('modal.escalate' | translate) }}
      </button>
    </mat-dialog-actions>
  `,
})
export class EscalateModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  saving = false;
  errorMessage = '';

  form = this.fb.group({ reason: ['', Validators.required] });

  constructor(
    public dialogRef: MatDialogRef<EscalateModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EscalateModalData,
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving = true;
    this.errorMessage = '';
    this.ticketService.escalate(this.data.ticketId, this.form.value.reason!).subscribe({
      next: () => this.dialogRef.close(true),
      error: (err) => {
        this.saving = false;
        this.errorMessage = err?.error?.error ?? 'Escalation failed. Only InProgress tickets can be escalated.';
      },
    });
  }
}
