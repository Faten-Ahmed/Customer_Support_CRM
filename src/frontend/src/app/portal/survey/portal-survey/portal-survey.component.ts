import {
  Component,
  OnInit,
  signal,
  inject,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { HttpErrorResponse } from '@angular/common/http';

import { PortalSurveyService, SurveyDetail } from '../portal-survey.service';

@Component({
  selector: 'app-portal-survey',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatCardModule,
  ],
  template: `
    <div class="survey-container">
      @if (errorCode() === 'SURVEY_EXPIRED') {
        <mat-card data-testid="expired-msg" class="status-card">
          <mat-card-content>
            <mat-icon color="warn">schedule</mat-icon>
            <p>This survey has expired</p>
          </mat-card-content>
        </mat-card>
      } @else if (errorCode() === 'SURVEY_ALREADY_SUBMITTED') {
        <mat-card data-testid="already-submitted-msg" class="status-card">
          <mat-card-content>
            <mat-icon color="primary">check_circle</mat-icon>
            <p>Thank you — you already submitted feedback</p>
          </mat-card-content>
        </mat-card>
      } @else if (submitted()) {
        <mat-card data-testid="thank-you" class="status-card">
          <mat-card-content>
            <mat-icon color="primary">sentiment_satisfied</mat-icon>
            <h2>Thank you for your feedback!</h2>
            <a data-testid="view-tickets-link" routerLink="/portal/tickets" mat-stroked-button color="primary">
              View my tickets
            </a>
          </mat-card-content>
        </mat-card>
      } @else if (survey()) {
        <mat-card data-testid="survey-form">
          <mat-card-header>
            <mat-card-title>How did we do?</mat-card-title>
            <mat-card-subtitle>
              Ticket #{{ survey()!.ticketNumber }} — {{ survey()!.ticketSubject }}
            </mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="surveyForm" (ngSubmit)="onSubmit()">
              <div class="stars-row">
                @for (star of [1, 2, 3, 4, 5]; track star) {
                  <button
                    type="button"
                    mat-icon-button
                    data-testid="star-btn"
                    [color]="(surveyForm.get('rating')!.value ?? 0) >= star ? 'accent' : ''"
                    (click)="selectRating(star)"
                    [attr.aria-label]="star + ' star'">
                    <mat-icon>{{ (surveyForm.get('rating')!.value ?? 0) >= star ? 'star' : 'star_border' }}</mat-icon>
                  </button>
                }
              </div>

              <mat-form-field appearance="outline" class="comment-field">
                <mat-label>Comments (optional)</mat-label>
                <textarea
                  matInput
                  formControlName="comment"
                  rows="4"
                  maxlength="1000"
                  placeholder="Tell us more about your experience…"></textarea>
                <mat-hint align="end">
                  <span data-testid="char-counter">
                    {{ (surveyForm.get('comment')!.value?.length ?? 0) }}/1000
                  </span>
                </mat-hint>
                @if (surveyForm.get('comment')!.hasError('maxlength')) {
                  <mat-error>Comment cannot exceed 1000 characters.</mat-error>
                }
              </mat-form-field>

              <div class="actions">
                <button
                  mat-raised-button
                  color="primary"
                  type="submit"
                  [disabled]="surveyForm.invalid || submitting()">
                  @if (submitting()) {
                    <mat-spinner data-testid="submit-spinner" diameter="20"></mat-spinner>
                  } @else {
                    Submit Feedback
                  }
                </button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .survey-container { max-width: 600px; margin: 40px auto; padding: 0 16px; }
    .status-card mat-card-content { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 32px; text-align: center; }
    .stars-row { display: flex; justify-content: center; margin: 16px 0; }
    .comment-field { width: 100%; margin-top: 16px; }
    .actions { margin-top: 24px; display: flex; justify-content: flex-end; }
  `],
})
export class PortalSurveyComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly surveyService = inject(PortalSurveyService);
  private readonly fb = inject(FormBuilder);

  readonly survey = signal<SurveyDetail | null>(null);
  readonly errorCode = signal<string | null>(null);
  readonly submitted = signal(false);
  readonly submitting = signal(false);

  surveyForm: FormGroup = this.fb.group({
    rating: [null, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: [null, [Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.surveyService.get(id).subscribe({
      next: data => this.survey.set(data),
      error: (err: HttpErrorResponse) => {
        const code = err.error?.code as string | undefined;
        this.errorCode.set(code ?? 'UNKNOWN');
      },
    });
  }

  selectRating(star: number): void {
    this.surveyForm.get('rating')!.setValue(star);
  }

  onSubmit(): void {
    if (this.surveyForm.invalid) return;
    const id = this.route.snapshot.paramMap.get('id')!;
    const { rating, comment } = this.surveyForm.value as { rating: number; comment: string | null };
    this.submitting.set(true);
    this.surveyService.submit(id, rating, comment ?? null).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: () => this.submitting.set(false),
    });
  }
}
