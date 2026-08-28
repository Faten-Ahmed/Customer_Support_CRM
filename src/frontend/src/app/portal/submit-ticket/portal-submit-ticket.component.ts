import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-portal-submit-ticket',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatCardModule,
    RouterLink,
  ],
  template: `
    <div class="page-header">
      <a mat-button routerLink="/portal/dashboard">
        <mat-icon>arrow_back</mat-icon> Back
      </a>
      <h1>Submit a Support Ticket</h1>
    </div>

    <mat-card>
      <mat-card-content>
        @if (successTicketNumber()) {
          <div class="success-banner">
            <mat-icon>check_circle</mat-icon>
            <div>
              <strong>Ticket {{ successTicketNumber() }} submitted successfully.</strong>
              <p>We'll be in touch soon. <a routerLink="/portal/dashboard">View my tickets</a></p>
            </div>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()">
            @if (errorMsg()) {
              <div class="error-banner">{{ errorMsg() }}</div>
            }

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Subject</mat-label>
              <input matInput formControlName="subject" placeholder="Briefly describe the issue" />
              @if (form.get('subject')?.hasError('required') && form.get('subject')?.touched) {
                <mat-error>Subject is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Priority</mat-label>
              <mat-select formControlName="priority">
                <mat-option value="Low">Low</mat-option>
                <mat-option value="Medium">Medium</mat-option>
                <mat-option value="High">High</mat-option>
                <mat-option value="Critical">Critical</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="6"
                placeholder="Please describe your issue in detail"></textarea>
              @if (form.get('description')?.hasError('required') && form.get('description')?.touched) {
                <mat-error>Description is required</mat-error>
              }
            </mat-form-field>

            <div class="form-actions">
              <button mat-flat-button color="primary" type="submit"
                      [disabled]="submitting()">
                @if (submitting()) {
                  <mat-spinner diameter="18" />
                } @else {
                  Submit Ticket
                }
              </button>
            </div>
          </form>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .page-header { display: flex; align-items: center; gap: 8px; margin-bottom: 16px; }
    h1 { margin: 0; font-size: 20px; }
    .full-width { width: 100%; margin-bottom: 8px; }
    .form-actions { text-align: right; margin-top: 8px; }
    .error-banner {
      background: #fdecea; color: #c62828; border-radius: 4px;
      padding: 12px 16px; margin-bottom: 16px; font-size: 14px;
    }
    .success-banner {
      display: flex; align-items: flex-start; gap: 12px;
      background: #e8f5e9; color: #2e7d32; border-radius: 4px;
      padding: 16px; font-size: 14px;
    }
    .success-banner mat-icon { color: #2e7d32; }
    .success-banner p { margin: 4px 0 0; }
  `],
})
export class PortalSubmitTicketComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly errorMsg = signal<string | null>(null);
  readonly successTicketNumber = signal<string | null>(null);

  form = this.fb.group({
    subject: ['', [Validators.required, Validators.maxLength(500)]],
    priority: ['Medium', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(10000)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errorMsg.set(null);

    const { subject, priority, description } = this.form.value;

    this.http.post<{ id: string; ticketNumber: string }>('/api/v1/portal/tickets', {
      subject, description, priority,
    }).subscribe({
      next: ticket => {
        this.submitting.set(false);
        this.successTicketNumber.set(ticket.ticketNumber);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        if (err.status === 409) {
          this.errorMsg.set('You already have an open ticket. Please wait for it to be resolved.');
        } else if (err.status === 403) {
          this.errorMsg.set('Please verify your email address before submitting a ticket.');
        } else {
          this.errorMsg.set('Something went wrong. Please try again.');
        }
      },
    });
  }
}
