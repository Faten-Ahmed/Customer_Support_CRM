import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { SlaPolicyService, SlaPolicy } from './sla-policy.service';

const mockPolicies: SlaPolicy[] = [
  {
    id: 'pol-1',
    departmentId: null,
    priority: 'Critical',
    firstResponseMinutes: 15,
    resolutionMinutes: 240,
    warningThresholdPercent: 80,
    breachThresholdPercent: 100,
    criticalBreachThresholdPercent: 200,
  },
  {
    id: 'pol-2',
    departmentId: null,
    priority: 'High',
    firstResponseMinutes: 120,
    resolutionMinutes: 480,
    warningThresholdPercent: 80,
    breachThresholdPercent: 100,
    criticalBreachThresholdPercent: 200,
  },
];

describe('SlaPolicyService', () => {
  let service: SlaPolicyService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SlaPolicyService],
    });
    service = TestBed.inject(SlaPolicyService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('list() GETs /api/v1/admin/sla/policies', () => {
    service.list().subscribe(policies => {
      expect(policies.length).toBe(2);
      expect(policies[0].priority).toBe('Critical');
    });
    const req = http.expectOne('/api/v1/admin/sla/policies');
    expect(req.request.method).toBe('GET');
    req.flush(mockPolicies);
  });

  it('update() PUTs /api/v1/admin/sla/policies/{id}', () => {
    const payload = {
      firstResponseMinutes: 20,
      resolutionMinutes: 300,
      warningThresholdPercent: 75,
      breachThresholdPercent: 100,
      criticalBreachThresholdPercent: 200,
    };
    service.update('pol-1', payload).subscribe();
    const req = http.expectOne('/api/v1/admin/sla/policies/pol-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.firstResponseMinutes).toBe(20);
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
