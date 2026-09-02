import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { TicketListComponent } from './ticket-list.component';
import { TicketService, TicketPage, TicketSummary } from '../ticket.service';

const mockTickets: TicketSummary[] = [
  {
    id: 't-1',
    ticketNumber: 'TK-0001',
    subject: 'Cannot login',
    customerName: 'Alice',
    customerId: 'c-1',
    status: 'New',
    priority: 'High',
    channel: 'Email',
    assignedToName: undefined,
    createdAt: '2025-01-01T10:00:00Z',
    updatedAt: '2025-01-01T10:00:00Z',
  },
  {
    id: 't-2',
    ticketNumber: 'TK-0002',
    subject: 'Billing question',
    customerName: 'Bob',
    customerId: 'c-2',
    status: 'InProgress',
    priority: 'Medium',
    channel: 'Portal',
    assignedToName: 'Carol',
    createdAt: '2025-01-02T09:00:00Z',
    updatedAt: '2025-01-02T09:00:00Z',
  },
];

const mockPage: TicketPage = { items: mockTickets, totalCount: 2, page: 1, pageSize: 20, totalPages: 1 };

describe('TicketListComponent', () => {
  let fixture: ComponentFixture<TicketListComponent>;
  let component: TicketListComponent;
  const mockTicketService = { list: vi.fn().mockReturnValue(of(mockPage)) };
  const mockRouter = { navigate: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.list.mockReturnValue(of(mockPage));

    await TestBed.configureTestingModule({
      imports: [TicketListComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: TicketService, useValue: mockTicketService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketListComponent);
    component = fixture.componentInstance;
    // Override router after creation
    (component as any).router = mockRouter;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call TicketService.list on init', () => {
    expect(mockTicketService.list).toHaveBeenCalled();
  });

  it('should populate tickets signal with response items', () => {
    expect(component.tickets().length).toBe(2);
    expect(component.tickets()[0].ticketNumber).toBe('TK-0001');
  });

  it('should set totalCount signal from response totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('should set loading to false after response', () => {
    expect(component.loading()).toBe(false);
  });

  it('should navigate to /app/tickets/new when onNewTicket() is called', () => {
    component.onNewTicket();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/tickets', 'new']);
  });

  it('should navigate to ticket detail when onRowClick() is called', () => {
    component.onRowClick('t-1');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/tickets', 't-1']);
  });

  it('should reload tickets when page changes', () => {
    mockTicketService.list.mockClear();
    component.onPageChange({ pageIndex: 1, pageSize: 20, length: 2 });
    expect(mockTicketService.list).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2, pageSize: 20 }),
    );
  });

  it('should reload tickets when status filter changes', () => {
    mockTicketService.list.mockClear();
    component.onStatusFilterChange(['New', 'InProgress']);
    expect(mockTicketService.list).toHaveBeenCalledWith(
      expect.objectContaining({ status: ['New', 'InProgress'] }),
    );
  });

  it('should reload tickets when priority filter changes', () => {
    mockTicketService.list.mockClear();
    component.onPriorityFilterChange('High');
    expect(mockTicketService.list).toHaveBeenCalledWith(
      expect.objectContaining({ priority: 'High' }),
    );
  });

  it('should return correct badge class for New status', () => {
    expect(component.statusBadgeClass('New')).toBe('badge-blue');
  });

  it('should return badge-red for Escalated status', () => {
    expect(component.statusBadgeClass('Escalated')).toBe('badge-red');
  });
});
