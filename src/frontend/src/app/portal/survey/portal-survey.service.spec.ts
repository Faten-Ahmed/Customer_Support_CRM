import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
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
      providers: [
        PortalSurveyService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(PortalSurveyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('get()', () => {
    it('should GET /api/v1/portal/surveys/{id} and return survey detail', () => {
      let result: SurveyDetail | undefined;
      service.get('survey-abc').subscribe(s => (result = s));

      const req = httpMock.expectOne('/api/v1/portal/surveys/survey-abc');
      expect(req.request.method).toBe('GET');
      req.flush(mockSurvey);

      expect(result).toEqual(mockSurvey);
    });

    it('should propagate 422 SURVEY_EXPIRED error', () => {
      let errorCode: string | undefined;
      service.get('survey-abc').subscribe({
        error: err => (errorCode = err.error?.code),
      });

      const req = httpMock.expectOne('/api/v1/portal/surveys/survey-abc');
      req.flush({ code: 'SURVEY_EXPIRED' }, { status: 422, statusText: 'Unprocessable Entity' });

      expect(errorCode).toBe('SURVEY_EXPIRED');
    });

    it('should propagate 422 SURVEY_ALREADY_SUBMITTED error', () => {
      let errorCode: string | undefined;
      service.get('survey-abc').subscribe({
        error: err => (errorCode = err.error?.code),
      });

      const req = httpMock.expectOne('/api/v1/portal/surveys/survey-abc');
      req.flush({ code: 'SURVEY_ALREADY_SUBMITTED' }, { status: 422, statusText: 'Unprocessable Entity' });

      expect(errorCode).toBe('SURVEY_ALREADY_SUBMITTED');
    });
  });

  describe('submit()', () => {
    it('should POST /api/v1/portal/surveys/{id}/submit with rating and comment', () => {
      let result: SurveySubmitResponse | undefined;
      service.submit('survey-abc', 4, 'Great support!').subscribe(r => (result = r));

      const req = httpMock.expectOne('/api/v1/portal/surveys/survey-abc/submit');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ rating: 4, comment: 'Great support!' });
      req.flush({ success: true });

      expect(result).toEqual({ success: true });
    });

    it('should POST with null comment when not provided', () => {
      service.submit('survey-abc', 5, null).subscribe();

      const req = httpMock.expectOne('/api/v1/portal/surveys/survey-abc/submit');
      expect(req.request.body).toEqual({ rating: 5, comment: null });
      req.flush({ success: true });
    });
  });
});
