import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CustomerService, CreateCustomerDto } from './customer.service';

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
    service.list({ page: 1, pageSize: 20, search: 'Ali', isVip: true, isActive: false }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('search')).toBe('Ali');
    expect(req.request.params.get('isVip')).toBe('true');
    expect(req.request.params.get('isActive')).toBe('false');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ items: [], meta: { page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } });
  });

  it('list() should include isVip=false and isActive=false when explicitly passed', () => {
    service.list({ page: 1, pageSize: 10, isVip: false, isActive: false }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.params.get('isVip')).toBe('false');
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush({ items: [], meta: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
  });

  it('list() should omit undefined search param', () => {
    service.list({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/customers');
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ items: [], meta: { page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } });
  });

  describe('create()', () => {
    const dto: CreateCustomerDto = {
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      phone: '555-0100',
      companyName: 'Acme',
    };

    it('should POST to /api/v1/customers and return created customer', () => {
      const mockResponse = { id: 'c-1', ...dto, isVip: false, isActive: true, createdAt: '2025-01-01' };
      service.create(dto).subscribe(res => expect(res).toEqual(mockResponse));
      const req = httpMock.expectOne('/api/v1/customers');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(dto);
      req.flush(mockResponse);
    });

    it('should throw with code EMAIL_ALREADY_EXISTS on 409', () => {
      let thrownError: any;
      service.create(dto).subscribe({ error: err => (thrownError = err) });
      const req = httpMock.expectOne('/api/v1/customers');
      req.flush(
        { errors: [{ code: 'EMAIL_ALREADY_EXISTS', message: 'Email already exists' }] },
        { status: 409, statusText: 'Conflict' }
      );
      expect(thrownError?.code).toBe('EMAIL_ALREADY_EXISTS');
    });

    it('should rethrow non-409 errors as-is', () => {
      let thrownError: any;
      service.create(dto).subscribe({ error: err => (thrownError = err) });
      const req = httpMock.expectOne('/api/v1/customers');
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      expect(thrownError).toBeTruthy();
      expect(thrownError?.code).not.toBe('EMAIL_ALREADY_EXISTS');
    });
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
    req.flush({ id: '42', fullName: 'Ali Hassan', email: 'ali@test.com', isVip: false, isActive: true, createdAt: '2025-01-01' });
  });

  it('update() should PUT /api/v1/customers/{id}', () => {
    service.update('42', { phone: '0501234567' }).subscribe(c => expect(c.phone).toBe('0501234567'));

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('PUT');
    req.flush({ id: '42', fullName: 'Ali Hassan', phone: '0501234567', isVip: false, isActive: true, createdAt: '2025-01-01' });
  });

  it('deactivate() should DELETE /api/v1/customers/{id}', () => {
    service.deactivate('42').subscribe();

    const req = httpMock.expectOne('/api/v1/customers/42');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
