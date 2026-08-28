import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../ticket.service';

export interface AssignModalData {
  ticketId: string;
  agents: { id: string; name: string }[];
}

@Component({
  selector: 'app-assign-modal',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Assign Ticket</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Agent</mat-label>
          <mat-select formControlName="agentId">
            @for (a of data.agents; track a.id) {
              <mat-option [value]="a.id">{{ a.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">Assign</button>
    </mat-dialog-actions>
  `,
})
export class AssignModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  form = this.fb.group({ agentId: ['', Validators.required] });

  constructor(
    public dialogRef: MatDialogRef<AssignModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AssignModalData,
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    this.ticketService.assign(this.data.ticketId, this.form.value.agentId!).subscribe({
      next: () => this.dialogRef.close(true),
    });
  }
}
