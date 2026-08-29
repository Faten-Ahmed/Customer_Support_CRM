import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TicketService, TicketListParams, TicketPage, TicketDetail, SlaStatus } from './ticket.service';

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

  describe('attachment methods', () => {
    const TICKET_ID = 'ticket-001';
    const ATTACHMENT_ID = 'att-abc';

    const mockAttachment = {
      id: ATTACHMENT_ID,
      ticketId: TICKET_ID,
      fileName: 'report.pdf',
      contentType: 'application/pdf',
      fileSize: 204800,
      uploaderName: 'Alice Agent',
      uploadedAt: '2026-08-01T10:00:00Z',
      presignedUrl: 'https://cdn.example.com/report.pdf?sig=abc',
    };

    it('getAttachments should GET /api/v1/tickets/{id}/attachments', () => {
      service.getAttachments(TICKET_ID).subscribe(list => {
        expect(list.length).toBe(1);
        expect(list[0].fileName).toBe('report.pdf');
      });
      const req = httpMock.expectOne(`/api/v1/tickets/${TICKET_ID}/attachments`);
      expect(req.request.method).toBe('GET');
      req.flush([mockAttachment]);
    });

    it('uploadAttachment should POST multipart FormData and return Attachment', () => {
      const file = new File(['hello'], 'hello.txt', { type: 'text/plain' });
      service.uploadAttachment(TICKET_ID, file).subscribe(att => {
        expect(att.fileName).toBe('hello.txt');
      });
      const req = httpMock.expectOne(`/api/v1/tickets/${TICKET_ID}/attachments`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body instanceof FormData).toBe(true);
      req.flush({ ...mockAttachment, fileName: 'hello.txt' });
    });

    it('deleteAttachment should DELETE /api/v1/tickets/{ticketId}/attachments/{attId}', () => {
      service.deleteAttachment(TICKET_ID, ATTACHMENT_ID).subscribe(res => {
        expect(res).toBe(null);
      });
      const req = httpMock.expectOne(`/api/v1/tickets/${TICKET_ID}/attachments/${ATTACHMENT_ID}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('addMessage()', () => {
    it('should POST /api/v1/tickets/{id}/messages with body and isInternal', () => {
      service.addMessage('t1', 'Hello customer', false).subscribe();
      const req = httpMock.expectOne('/api/v1/tickets/t1/messages');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ body: 'Hello customer', isInternal: false });
      req.flush({ id: 'm5', body: 'Hello customer' });
    });
  });

  describe('getMessages()', () => {
    it('should GET /api/v1/tickets/{id}/messages with page params', () => {
      service.getMessages('t1', 1, 20).subscribe();
      const req = httpMock.expectOne(r => r.url === '/api/v1/tickets/t1/messages');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 });
    });
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
      expect(req.request.body).toEqual({ targetAgentId: 'd2', reason: 'Needs billing' });
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
        subject: 'Test', subjectAr: 'اختبار', description: 'Desc', descriptionAr: 'وصف', priority: 'High',
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

describe('TicketService — getHistory()', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  const TICKET_ID = 'ticket-42';

  const mockEntries = [
    {
      fieldChanged: 'Status',
      oldValue: 'New',
      newValue: 'InProgress',
      changedByName: 'Alice Agent',
      changedAt: '2026-08-01T09:00:00Z',
    },
    {
      fieldChanged: 'Priority',
      oldValue: 'Medium',
      newValue: 'High',
      changedByName: 'Bob Manager',
      changedAt: '2026-08-01T10:30:00Z',
    },
  ];

  const mockPage = { items: mockEntries, totalCount: 2, page: 1, pageSize: 20, totalPages: 1 };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should GET /api/v1/tickets/{id}/history with page params', () => {
    service.getHistory(TICKET_ID).subscribe(page => {
      expect(page.items.length).toBe(2);
      expect(page.items[0].fieldChanged).toBe('Status');
      expect(page.items[1].fieldChanged).toBe('Priority');
    });

    const req = httpMock.expectOne(r => r.url === `/api/v1/tickets/${TICKET_ID}/history`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush(mockPage);
  });

  it('should return empty items array when no history exists', () => {
    service.getHistory(TICKET_ID).subscribe(page => {
      expect(page.items).toEqual([]);
    });
    const req = httpMock.expectOne(r => r.url === `/api/v1/tickets/${TICKET_ID}/history`);
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 });
  });
});

describe('TicketService — getSla()', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  const TICKET_ID = 'ticket-77';

  const mockSla: SlaStatus = {
    isPaused: false,
    firstResponse: {
      dueAt: '2026-08-26T12:00:00Z',
      elapsedPercent: 45,
      breached: false,
      remainingLabel: '2h 15m',
    },
    resolution: {
      dueAt: '2026-08-28T12:00:00Z',
      elapsedPercent: 10,
      breached: false,
      remainingLabel: '46h 30m',
    },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should GET /api/v1/tickets/{id}/sla', () => {
    service.getSla(TICKET_ID).subscribe(sla => {
      expect(sla.firstResponse.elapsedPercent).toBe(45);
      expect(sla.resolution.remainingLabel).toBe('46h 30m');
      expect(sla.isPaused).toBe(false);
    });
    const req = httpMock.expectOne(`/api/v1/tickets/${TICKET_ID}/sla`);
    expect(req.request.method).toBe('GET');
    req.flush(mockSla);
  });

  it('should reflect isPaused true when ticket is OnHold', () => {
    service.getSla(TICKET_ID).subscribe(sla => {
      expect(sla.isPaused).toBe(true);
    });
    const req = httpMock.expectOne(`/api/v1/tickets/${TICKET_ID}/sla`);
    req.flush({ ...mockSla, isPaused: true });
  });
});
