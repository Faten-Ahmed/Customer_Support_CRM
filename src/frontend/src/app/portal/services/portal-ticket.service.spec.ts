import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PortalTicketService } from './portal-ticket.service';

describe('PortalTicketService', () => {
  let service: PortalTicketService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PortalTicketService,
      ],
    });
    service = TestBed.inject(PortalTicketService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() GETs /api/v1/portal/tickets', () => {
    service.list().subscribe();
    const req = http.expectOne('/api/v1/portal/tickets');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 });
  });

  it('list(status) appends status query param', () => {
    service.list('New').subscribe();
    const req = http.expectOne('/api/v1/portal/tickets?status=New');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 });
  });

  it('getById() GETs /api/v1/portal/tickets/{id}', () => {
    service.getById('abc-123').subscribe();
    const req = http.expectOne('/api/v1/portal/tickets/abc-123');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'abc-123', subject: 'Help', status: 'New' });
  });

  it('getMessages() GETs /api/v1/portal/tickets/{id}/messages', () => {
    service.getMessages('abc-123').subscribe();
    const req = http.expectOne('/api/v1/portal/tickets/abc-123/messages');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 });
  });

  it('addMessage() POSTs body to /api/v1/portal/tickets/{id}/messages', () => {
    service.addMessage('abc-123', 'Still waiting').subscribe();
    const req = http.expectOne('/api/v1/portal/tickets/abc-123/messages');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ body: 'Still waiting' });
    req.flush({ id: 'm1' });
  });
});
