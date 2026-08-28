import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PortalTicketDetailComponent } from './portal-ticket-detail.component';
import { PortalTicketService, PortalTicketDetail, PortalTicketMessage } from '../services/portal-ticket.service';

const mockTicket: PortalTicketDetail = {
  id: 'tid-1',
  ticketNumber: 'TKT-001',
  subject: 'Cannot login',
  description: 'Login page is broken.',
  status: 'New',
  priority: 'High',
  channel: 'Portal',
  createdAt: '2026-01-01T10:00:00Z',
  updatedAt: '2026-01-01T10:00:00Z',
  resolvedAt: null,
  closedAt: null,
  assignedAgentName: null,
};

const mockMessages: PortalTicketMessage[] = [
  {
    id: 'msg-1',
    ticketId: 'tid-1',
    body: 'Please help me',
    isInternal: false,
    authorUserId: null,
    authorName: 'customer',
    authorCustomerId: 'cust-1',
    createdAt: '2026-01-01T10:01:00Z',
  },
];

const mockMessagePage = {
  items: mockMessages,
  totalCount: 1,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

function buildService(overrides: Partial<{
  getById: ReturnType<typeof vi.fn>;
  getMessages: ReturnType<typeof vi.fn>;
  addMessage: ReturnType<typeof vi.fn>;
  getAttachments: ReturnType<typeof vi.fn>;
  close: ReturnType<typeof vi.fn>;
}> = {}) {
  return {
    getById: overrides.getById ?? vi.fn().mockReturnValue(of(mockTicket)),
    getMessages: overrides.getMessages ?? vi.fn().mockReturnValue(of(mockMessagePage)),
    getAttachments: overrides.getAttachments ?? vi.fn().mockReturnValue(of([])),
    close: overrides.close ?? vi.fn().mockReturnValue(of({ id: 'tid-1', status: 'Closed' })),
    addMessage: overrides.addMessage ?? vi.fn().mockReturnValue(of({
      id: 'msg-2',
      ticketId: 'tid-1',
      body: 'Follow-up reply',
      isInternal: false,
      authorUserId: null,
      authorName: 'customer',
      authorCustomerId: 'cust-1',
      createdAt: '2026-01-01T11:00:00Z',
    } as PortalTicketMessage)),
    uploadAttachment: vi.fn().mockReturnValue(of({})),
  };
}

describe('PortalTicketDetailComponent', () => {
  let fixture: ComponentFixture<PortalTicketDetailComponent>;
  let component: PortalTicketDetailComponent;
  let service: ReturnType<typeof buildService>;

  async function setup(serviceOverrides?: Parameters<typeof buildService>[0]) {
    service = buildService(serviceOverrides);
    await TestBed.configureTestingModule({
      imports: [PortalTicketDetailComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PortalTicketService, useValue: service },
        {
          provide: ActivatedRoute,
          useValue: { params: of({ id: 'tid-1' }) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalTicketDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('loads and displays ticket subject and status', async () => {
    await setup();
    expect(component.ticket()).not.toBeNull();
    expect(component.ticket()!.subject).toBe('Cannot login');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Cannot login');
    expect(el.textContent).toContain('New');
  });

  it('loads and renders messages', async () => {
    await setup();
    expect(component.messages().length).toBe(1);
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="message-row"]');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('Please help me');
  });

  it('sends a reply and appends it to messages', async () => {
    await setup();
    component.replyControl.setValue('Follow-up reply');
    component.sendReply();
    await fixture.whenStable();
    expect(service.addMessage).toHaveBeenCalledWith('tid-1', 'Follow-up reply');
    expect(component.messages().length).toBe(2);
    expect(component.replyControl.value).toBe('');
  });

  it('shows closed banner for a closed ticket', async () => {
    const closedTicket = { ...mockTicket, status: 'Closed', closedAt: '2026-01-02T00:00:00Z' };
    await setup({
      getById: vi.fn().mockReturnValue(of(closedTicket)),
    });
    const banner = fixture.nativeElement.querySelector('[data-testid="closed-banner"]');
    expect(banner).not.toBeNull();
    expect(banner.textContent).toContain('Ticket Closed');
  });

  it('shows send error when addMessage fails', async () => {
    await setup({
      addMessage: vi.fn().mockReturnValue(throwError(() => new Error('Network error'))),
    });
    component.replyControl.setValue('test reply');
    component.sendReply();
    await fixture.whenStable();
    expect(component.sendError()).toBe('Failed to send reply. Please try again.');
    expect(component.sending()).toBe(false);
  });

  it('does not send when reply is empty', async () => {
    await setup();
    component.replyControl.setValue('');
    component.sendReply();
    expect(service.addMessage).not.toHaveBeenCalled();
  });
});
