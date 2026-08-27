import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CustomerService } from './customer.service';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        CustomerService,
      ],
    });
    service = TestBed.inject(CustomerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/customers with query params', () => {
    service.list({ page: 1, pageSize: 20, search: 'Ali', vipOnly: true, activeOnly: false }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('search')).toBe('Ali');
    expect(req.request.params.get('vipOnly')).toBe('true');
    expect(req.request.params.get('activeOnly')).toBe('false');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ data: [], total: 0, page: 1, pageSize: 20 });
  });

  it('list() should include vipOnly=false and activeOnly=false when explicitly passed', () => {
    service.list({ page: 1, pageSize: 10, vipOnly: false, activeOnly: false }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.params.get('vipOnly')).toBe('false');
    expect(req.request.params.get('activeOnly')).toBe('false');
    req.flush({ data: [], total: 0, page: 1, pageSize: 10 });
  });

  it('list() should omit undefined search param', () => {
    service.list({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ data: [], total: 0, page: 1, pageSize: 20 });
  });
});
