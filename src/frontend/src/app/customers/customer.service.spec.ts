// src/app/customers/customer.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CustomerService, CreateCustomerDto, UpdateCustomerDto } from './customer.service';

describe('CustomerService (FE-008)', () => {
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

  describe('create()', () => {
    const dto: CreateCustomerDto = {
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      phone: '555-0100',
      companyName: 'Acme',
      country: 'US',
      city: 'New York',
    };

    it('should POST to /api/v1/customers and return created customer', () => {
      const mockResponse = { id: 'c-1', ...dto };
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
        { code: 'EMAIL_ALREADY_EXISTS', message: 'Email already exists' },
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

  describe('update()', () => {
    it('should PATCH to /api/v1/customers/:id', () => {
      const dto: UpdateCustomerDto = { fullName: 'Jane Smith' };
      const mockResponse = { id: 'c-1', email: 'jane@example.com', ...dto };
      service.update('c-1', dto).subscribe(res => expect(res).toEqual(mockResponse));
      const req = httpMock.expectOne('/api/v1/customers/c-1');
      expect(req.request.method).toBe('PATCH');
      req.flush(mockResponse);
    });
  });
});
