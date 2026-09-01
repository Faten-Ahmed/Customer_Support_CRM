import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        DashboardService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getKpis() should GET /api/v1/reports/dashboard', () => {
    service.getKpis().subscribe(data => {
      expect(data.openTickets).toBeDefined();
    });
    const req = httpMock.expectOne('/api/v1/reports/dashboard');
    expect(req.request.method).toBe('GET');
    req.flush({ openTickets: 12, slaBreachRate: 5, avgFirstResponse: 30, escalationRate: 2 });
  });

  it('getKpis() should include departmentId param when provided', () => {
    service.getKpis('d1').subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/reports/dashboard');
    expect(req.request.params.get('departmentId')).toBe('d1');
    req.flush({});
  });
});
