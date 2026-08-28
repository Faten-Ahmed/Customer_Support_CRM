import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TicketService, TicketListParams, TicketPage, TicketDetail } from './ticket.service';

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

  describe('action methods', () => {
    it('assign() should PATCH /api/v1/tickets/{id}/assign', () => {
      service.assign('t1', 'agent-1').subscribe();
      const req = httpMock.expectOne('/api/v1/tickets/t1/assign');
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual({ agentId: 'agent-1' });
      req.flush({});
    });

    it('transfer() should PATCH /api/v1/tickets/{id}/transfer', () => {
      service.transfer('t1', 'd2', 'Needs billing').subscribe();
      const req = httpMock.expectOne('/api/v1/tickets/t1/transfer');
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual({ departmentId: 'd2', note: 'Needs billing' });
      req.flush({});
    });

    it('escalate() should PATCH /api/v1/tickets/{id}/escalate', () => {
      service.escalate('t1', 'Customer VIP and very upset').subscribe();
      const req = httpMock.expectOne('/api/v1/tickets/t1/escalate');
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual({ reason: 'Customer VIP and very upset' });
      req.flush({});
    });

    it('changeStatus() should PATCH /api/v1/tickets/{id}/status', () => {
      service.changeStatus('t1', 'OnHold', undefined).subscribe();
      const req = httpMock.expectOne('/api/v1/tickets/t1/status');
      expect(req.request.method).toBe('PATCH');
      req.flush({});
    });
  });

  describe('create()', () => {
    it('should POST to /api/v1/tickets and return the new ticket', () => {
      const payload = {
        customerId: 'c1', departmentId: 'd1', categoryId: 'cat1',
        subject: 'Test', description: 'Desc', priority: 'High',
        customFields: [{ definitionId: 'f1', value: 'val' }],
      };

      service.create(payload).subscribe(t => expect(t.id).toBeTruthy());

      const req = httpMock.expectOne('/api/v1/tickets');
      expect(req.request.method).toBe('POST');
      expect(req.request.body.subject).toBe('Test');
      req.flush({ id: 'ticket-1', ...payload });
    });
  });

  describe('getById()', () => {
    it('should GET /api/v1/tickets/:id and return a TicketDetail', () => {
      const mock: TicketDetail = {
        id: 't-1',
        ticketNumber: 'TK-0001',
        subject: 'Login issue',
        description: 'Cannot log in.',
        status: 'New',
        priority: 'High',
        channel: 'Email',
        customerId: 'c-1',
        customerName: 'Alice',
        departmentName: 'Support',
        createdAt: '2025-01-01T10:00:00Z',
        updatedAt: '2025-01-01T10:00:00Z',
      };

      service.getById('t-1').subscribe(res => expect(res).toEqual(mock));
      const req = httpMock.expectOne('/api/v1/tickets/t-1');
      expect(req.request.method).toBe('GET');
      req.flush(mock);
    });
  });
});
