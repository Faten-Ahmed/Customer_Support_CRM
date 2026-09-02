import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterModule } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { vi } from 'vitest';

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
  let serviceSpy: { get: ReturnType<typeof vi.fn>; submit: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    serviceSpy = {
      get: vi.fn().mockReturnValue(of(mockSurvey)),
      submit: vi.fn().mockReturnValue(of({ success: true })),
    };

    await TestBed.configureTestingModule({
      imports: [
        PortalSurveyComponent,
        RouterModule.forRoot([]),
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
    await fixture.whenStable();
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
    expect(component.surveyForm.get('rating')!.valid).toBe(false);
  });

  it('should mark form valid after selecting a star rating', () => {
    component.selectRating(4);
    fixture.detectChanges();
    expect(component.surveyForm.get('rating')!.value).toBe(4);
    expect(component.surveyForm.valid).toBe(true);
  });

  it('should show character counter for comment textarea', () => {
    const counter: HTMLElement = fixture.nativeElement.querySelector('[data-testid="char-counter"]');
    expect(counter).toBeTruthy();
  });

  it('should enforce max 1000 characters on comment', () => {
    const control = component.surveyForm.get('comment')!;
    control.setValue('a'.repeat(1001));
    expect(control.valid).toBe(false);
  });

  it('should call submit service and show thank-you on success', async () => {
    component.selectRating(5);
    component.surveyForm.get('comment')!.setValue('Excellent!');
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(serviceSpy.submit).toHaveBeenCalledWith('survey-abc', 5, 'Excellent!');
    const thankYou: HTMLElement = fixture.nativeElement.querySelector('[data-testid="thank-you"]');
    expect(thankYou).toBeTruthy();
  });

  it('should show "View my tickets" link after submission', async () => {
    component.selectRating(3);
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    const link: HTMLElement = fixture.nativeElement.querySelector('[data-testid="view-tickets-link"]');
    expect(link).toBeTruthy();
  });

  it('should show SURVEY_EXPIRED message and hide form', async () => {
    serviceSpy.get.mockReturnValue(throwError(() => makeError('SURVEY_EXPIRED')));
    fixture = TestBed.createComponent(PortalSurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const msg: HTMLElement = fixture.nativeElement.querySelector('[data-testid="expired-msg"]');
    expect(msg).toBeTruthy();
    expect(msg.textContent).toContain('This survey has expired');
    const form: HTMLElement = fixture.nativeElement.querySelector('[data-testid="survey-form"]');
    expect(form).toBeNull();
  });

  it('should show SURVEY_ALREADY_SUBMITTED message and hide form', async () => {
    serviceSpy.get.mockReturnValue(throwError(() => makeError('SURVEY_ALREADY_SUBMITTED')));
    fixture = TestBed.createComponent(PortalSurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const msg: HTMLElement = fixture.nativeElement.querySelector('[data-testid="already-submitted-msg"]');
    expect(msg).toBeTruthy();
    expect(msg.textContent).toContain('Thank you — you already submitted feedback');
    const form: HTMLElement = fixture.nativeElement.querySelector('[data-testid="survey-form"]');
    expect(form).toBeNull();
  });

  it('should show loading spinner while submitting and hide after', async () => {
    const subject = new Subject<{ success: boolean }>();
    serviceSpy.submit.mockReturnValue(subject.asObservable());

    component.selectRating(5);
    component.onSubmit();
    fixture.detectChanges();

    const spinner: HTMLElement = fixture.nativeElement.querySelector('[data-testid="submit-spinner"]');
    expect(spinner).toBeTruthy();

    subject.next({ success: true });
    subject.complete();
    await fixture.whenStable();
    fixture.detectChanges();

    const spinnerAfter: HTMLElement = fixture.nativeElement.querySelector('[data-testid="submit-spinner"]');
    expect(spinnerAfter).toBeNull();
  });
});
