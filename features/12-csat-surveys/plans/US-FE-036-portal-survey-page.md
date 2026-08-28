# Portal Survey Page — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-036  
**Goal:** Render a CSAT survey page at `/portal/surveys/{id}` that lets customers rate their support experience with a 1–5 star rating and optional comment, handling expired and already-submitted states gracefully.

**Architecture:** `PortalSurveyComponent` is a standalone Angular 21 component routed at `/portal/surveys/:id`. It delegates all HTTP calls to `PortalSurveyService` which wraps `GET /api/portal/surveys/{id}` and `POST /api/portal/surveys/{id}/submit`. The component uses Angular Signals for local UI state (loading, error code, submitted flag) and Angular Reactive Forms for the rating + comment form.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/survey/portal-survey/portal-survey.component.ts` |
| Create | `src/app/portal/survey/portal-survey/portal-survey.component.spec.ts` |
| Create | `src/app/portal/survey/portal-survey.service.ts` |
| Create | `src/app/portal/survey/portal-survey.service.spec.ts` |

---

## Task 1: PortalSurveyService

### 1.1 — Write the failing service tests

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/survey/portal-survey.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PortalSurveyService, SurveyDetail, SurveySubmitResponse } from './portal-survey.service';

describe('PortalSurveyService', () => {
  let service: PortalSurveyService;
  let httpMock: HttpTestingController;

  const mockSurvey: SurveyDetail = {
    id: 'survey-abc',
    ticketNumber: 'TKT-1001',
    ticketSubject: 'Cannot log into account',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PortalSurveyService],
    });
    service = TestBed.inject(PortalSurveyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('get()', () => {
    it('should GET /api/portal/surveys/{id} and return survey detail', () => {
      let result: SurveyDetail | undefined;
      service.get('survey-abc').subscribe(s => (result = s));

      const req = httpMock.expectOne('/api/portal/surveys/survey-abc');
      expect(req.request.method).toBe('GET');
      req.flush(mockSurvey);

      expect(result).toEqual(mockSurvey);
    });

    it('should propagate 422 SURVEY_EXPIRED error', () => {
      let errorCode: string | undefined;
      service.get('survey-abc').subscribe({
        error: err => (errorCode = err.error?.code),
      });

      const req = httpMock.expectOne('/api/portal/surveys/survey-abc');
      req.flush({ code: 'SURVEY_EXPIRED' }, { status: 422, statusText: 'Unprocessable Entity' });

      expect(errorCode).toBe('SURVEY_EXPIRED');
    });

    it('should propagate 422 SURVEY_ALREADY_SUBMITTED error', () => {
      let errorCode: string | undefined;
      service.get('survey-abc').subscribe({
        error: err => (errorCode = err.error?.code),
      });

      const req = httpMock.expectOne('/api/portal/surveys/survey-abc');
      req.flush({ code: 'SURVEY_ALREADY_SUBMITTED' }, { status: 422, statusText: 'Unprocessable Entity' });

      expect(errorCode).toBe('SURVEY_ALREADY_SUBMITTED');
    });
  });

  describe('submit()', () => {
    it('should POST /api/portal/surveys/{id}/submit with rating and comment', () => {
      let result: SurveySubmitResponse | undefined;
      service.submit('survey-abc', 4, 'Great support!').subscribe(r => (result = r));

      const req = httpMock.expectOne('/api/portal/surveys/survey-abc/submit');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ rating: 4, comment: 'Great support!' });
      req.flush({ success: true });

      expect(result).toEqual({ success: true });
    });

    it('should POST with null comment when not provided', () => {
      service.submit('survey-abc', 5, null).subscribe();

      const req = httpMock.expectOne('/api/portal/surveys/survey-abc/submit');
      expect(req.request.body).toEqual({ rating: 5, comment: null });
      req.flush({ success: true });
    });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/survey/portal-survey.service.spec.ts --watch=false
```

Expected: FAIL — `PortalSurveyService` does not exist yet.

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/survey/portal-survey.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SurveyDetail {
  id: string;
  ticketNumber: string;
  ticketSubject: string;
}

export interface SurveySubmitResponse {
  success: boolean;
}

@Injectable({ providedIn: 'root' })
export class PortalSurveyService {
  private readonly http = inject(HttpClient);

  get(id: string): Observable<SurveyDetail> {
    return this.http.get<SurveyDetail>(`/api/portal/surveys/${id}`);
  }

  submit(id: string, rating: number, comment: string | null): Observable<SurveySubmitResponse> {
    return this.http.post<SurveySubmitResponse>(
      `/api/portal/surveys/${id}/submit`,
      { rating, comment }
    );
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/survey/portal-survey.service.spec.ts --watch=false
```

Expected: 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(portal-survey): add PortalSurveyService with get and submit methods"
```

---

## Task 2: PortalSurveyComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/survey/portal-survey/portal-survey.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { PortalSurveyComponent } from './portal-survey.component';
import { PortalSurveyService, SurveyDetail } from '../portal-survey.service';

const mockSurvey: SurveyDetail = {
  id: 'survey-abc',
  ticketNumber: 'TKT-1001',
  ticketSubject: 'Cannot log into account',
};

function makeError(code: string): HttpErrorResponse {
  return new HttpErrorResponse({
    error: { code },
    status: 422,
    statusText: 'Unprocessable Entity',
  });
}

describe('PortalSurveyComponent', () => {
  let fixture: ComponentFixture<PortalSurveyComponent>;
  let component: PortalSurveyComponent;
  let serviceSpy: jasmine.SpyObj<PortalSurveyService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('PortalSurveyService', ['get', 'submit']);
    serviceSpy.get.and.returnValue(of(mockSurvey));
    serviceSpy.submit.and.returnValue(of({ success: true }));

    await TestBed.configureTestingModule({
      imports: [
        PortalSurveyComponent,
        RouterTestingModule,
        NoopAnimationsModule,
      ],
      providers: [
        { provide: PortalSurveyService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'survey-abc' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalSurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display ticket number and subject', () => {
    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.textContent).toContain('TKT-1001');
    expect(compiled.textContent).toContain('Cannot log into account');
  });

  it('should render 5 star buttons', () => {
    const stars: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('[data-testid="star-btn"]');
    expect(stars.length).toBe(5);
  });

  it('should mark form invalid when no star is selected', () => {
    expect(component.surveyForm.get('rating')!.valid).toBeFalse();
  });

  it('should mark form valid after selecting a star rating', () => {
    component.selectRating(4);
    fixture.detectChanges();
    expect(component.surveyForm.get('rating')!.value).toBe(4);
    expect(component.surveyForm.valid).toBeTrue();
  });

  it('should show character counter for comment textarea', () => {
    const counter: HTMLElement = fixture.nativeElement.querySelector('[data-testid="char-counter"]');
    expect(counter).toBeTruthy();
  });

  it('should enforce max 1000 characters on comment', () => {
    const control = component.surveyForm.get('comment')!;
    control.setValue('a'.repeat(1001));
    expect(control.valid).toBeFalse();
  });

  it('should call submit service and show thank-you on success', fakeAsync(() => {
    component.selectRating(5);
    component.surveyForm.get('comment')!.setValue('Excellent!');
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(serviceSpy.submit).toHaveBeenCalledWith('survey-abc', 5, 'Excellent!');
    const thankYou: HTMLElement = fixture.nativeElement.querySelector('[data-testid="thank-you"]');
    expect(thankYou).toBeTruthy();
  }));

  it('should show "View my tickets" link after submission', fakeAsync(() => {
    component.selectRating(3);
    component.onSubmit();
    tick();
    fixture.detectChanges();

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('[data-testid="view-tickets-link"]');
    expect(link).toBeTruthy();
    expect(link.getAttribute('routerLink') || link.getAttribute('href')).toContain('/portal/tickets');
  }));

  it('should show SURVEY_EXPIRED message and hide form', fakeAsync(() => {
    serviceSpy.get.and.returnValue(throwError(() => makeError('SURVEY_EXPIRED')));
    fixture = TestBed.createComponent(PortalSurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const msg: HTMLElement = fixture.nativeElement.querySelector('[data-testid="expired-msg"]');
    expect(msg).toBeTruthy();
    expect(msg.textContent).toContain('This survey has expired');
    const form: HTMLElement = fixture.nativeElement.querySelector('[data-testid="survey-form"]');
    expect(form).toBeNull();
  }));

  it('should show SURVEY_ALREADY_SUBMITTED message and hide form', fakeAsync(() => {
    serviceSpy.get.and.returnValue(throwError(() => makeError('SURVEY_ALREADY_SUBMITTED')));
    fixture = TestBed.createComponent(PortalSurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const msg: HTMLElement = fixture.nativeElement.querySelector('[data-testid="already-submitted-msg"]');
    expect(msg).toBeTruthy();
    expect(msg.textContent).toContain('Thank you — you already submitted feedback');
    const form: HTMLElement = fixture.nativeElement.querySelector('[data-testid="survey-form"]');
    expect(form).toBeNull();
  }));

  it('should show loading spinner while submitting', fakeAsync(() => {
    const { Subject } = require('rxjs');
    const subject = new Subject<{ success: boolean }>();
    serviceSpy.submit.and.returnValue(subject.asObservable());

    component.selectRating(5);
    component.onSubmit();
    fixture.detectChanges();

    const spinner: HTMLElement = fixture.nativeElement.querySelector('[data-testid="submit-spinner"]');
    expect(spinner).toBeTruthy();

    subject.next({ success: true });
    subject.complete();
    tick();
    fixture.detectChanges();

    const spinnerAfter: HTMLElement = fixture.nativeElement.querySelector('[data-testid="submit-spinner"]');
    expect(spinnerAfter).toBeNull();
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/survey/portal-survey/portal-survey.component.spec.ts --watch=false
```

Expected: FAIL — component does not exist yet.

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/survey/portal-survey/portal-survey.component.ts
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/survey/portal-survey/portal-survey.component.spec.ts --watch=false
```

Expected: 11 tests PASS.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(portal-survey): implement PortalSurveyComponent with star rating, expiry, and submission states"
```
