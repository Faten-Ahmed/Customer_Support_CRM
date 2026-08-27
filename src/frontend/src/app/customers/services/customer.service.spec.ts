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

describe('CustomerService — detail methods', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
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

  it('getById() should GET /api/v1/customers/{id}', () => {
    service.getById('42').subscribe(c => expect(c.id).toBe('42'));

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('GET');
    req.flush({ id: '42', fullName: 'Ali Hassan', email: 'ali@test.com', isVip: false, isActive: true, ticketCount: 0, createdAt: '2025-01-01' });
  });

  it('update() should PATCH /api/v1/customers/{id}', () => {
    service.update('42', { phone: '0501234567' }).subscribe(c => expect(c.phone).toBe('0501234567'));

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: '42', fullName: 'Ali Hassan', phone: '0501234567', isVip: false, isActive: true, ticketCount: 0, createdAt: '2025-01-01' });
  });

  it('deactivate() should DELETE /api/v1/customers/{id}', () => {
    service.deactivate('42').subscribe();

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
