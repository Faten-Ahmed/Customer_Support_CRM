import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TicketService, TicketListParams, TicketPage } from './ticket.service';

const emptyPage: TicketPage = { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };

describe('TicketService', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        TicketService,
      ],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should GET /api/v1/tickets with no params', () => {
    service.list({}).subscribe(res => expect(res).toEqual(emptyPage));

    const req = httpMock.expectOne(r => r.url === '/api/v1/tickets');
    expect(req.request.method).toBe('GET');
    req.flush(emptyPage);
  });

  it('should pass status filter as repeated query params', () => {
    const params: TicketListParams = { status: ['New', 'InProgress'], page: 1, pageSize: 20 };
    service.list(params).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/tickets');
    expect(req.request.params.getAll('status')).toEqual(['New', 'InProgress']);
    req.flush(emptyPage);
  });

  it('should pass search, priority, and pagination params', () => {
    const params: TicketListParams = {
      search: 'login issue',
      priority: 'High',
      page: 2,
      pageSize: 20,
    };
    service.list(params).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/tickets');
    expect(req.request.params.get('search')).toBe('login issue');
    expect(req.request.params.get('priority')).toBe('High');
    expect(req.request.params.get('page')).toBe('2');
    req.flush(emptyPage);
  });

  it('should omit undefined params', () => {
    service.list({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/tickets');
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('priority')).toBe(false);
    req.flush(emptyPage);
  });
});
