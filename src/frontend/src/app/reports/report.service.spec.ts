import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ReportService } from './report.service';

describe('ReportService', () => {
  let service: ReportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ReportService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ReportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getTicketReport() should GET /api/v1/reports/tickets with date params', () => {
    service.getTicketReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/tickets');
    expect(req.request.params.get('dateFrom')).toBe('2025-01-01');
    req.flush({ summary: {}, byStatus: [], trend: [] });
  });

  it('getSlaReport() should GET /api/v1/reports/sla', () => {
    service.getSlaReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/sla');
    req.flush({ complianceRate: 90, byPriority: [] });
  });

  it('getAgentReport() should GET /api/v1/reports/agents', () => {
    service.getAgentReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/agents');
    req.flush([]);
  });

  it('getCsatReport() should GET /api/v1/reports/csat', () => {
    service.getCsatReport({ dateFrom: '2025-01-01', dateTo: '2025-01-31' }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/csat');
    req.flush({ avgRating: 4.2, distribution: [], comments: [] });
  });
});
