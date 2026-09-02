import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { KbService } from '../services/kb.service';

@Component({
  selector: 'app-reject-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
  ],
  template: `
    <h2 mat-dialog-title i18n="@@kb.reject.title">Reject Article</h2>
    <mat-dialog-content>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label i18n="@@kb.reject.noteLabel">Rejection note (min 10 characters)</mat-label>
        <textarea matInput [formControl]="noteControl" rows="4"></textarea>
        @if (noteControl.hasError('minlength')) {
          <mat-error i18n="@@kb.reject.noteMinLength">Minimum 10 characters required</mat-error>
        }
        @if (noteControl.hasError('required')) {
          <mat-error i18n="@@kb.reject.noteRequired">Note is required</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close i18n="@@common.cancel">Cancel</button>
      <button mat-raised-button color="warn" [disabled]="noteControl.invalid || submitting" (click)="onReject()" i18n="@@kb.reject.confirm">
        Reject
      </button>
    </mat-dialog-actions>
  `,
})
export class RejectDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly kbService = inject(KbService);
  private readonly snackBar = inject(MatSnackBar);

  readonly noteControl = this.fb.control('', [Validators.required, Validators.minLength(10)]);
  submitting = false;

  constructor(
    public readonly dialogRef: MatDialogRef<RejectDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public readonly data: { articleId: string }
  ) {}

  onReject(): void {
    if (this.noteControl.invalid) return;
    this.submitting = true;
    this.kbService.reject(this.data.articleId, this.noteControl.value!).subscribe({
      next: () => {
        this.snackBar.open('Article rejected and returned to draft', 'OK', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: () => { this.submitting = false; },
    });
  }
}
