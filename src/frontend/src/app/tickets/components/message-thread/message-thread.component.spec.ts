import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { MessageThreadComponent } from './message-thread.component';
import { TicketService, TicketMessage, TicketMessagePage } from '../../ticket.service';
import { SignalRService } from '../../../shared/services/signalr.service';

const mockMessages: TicketMessage[] = [
  {
    id: 'm1', ticketId: 't1', body: 'Hello', isInternal: false,
    authorCustomerId: 'cust-1', authorName: 'Ali', createdAt: '2025-01-01T10:00:00Z',
  },
  {
    id: 'm2', ticketId: 't1', body: 'Hi there', isInternal: false,
    authorUserId: 'agent-1', authorName: 'Omar', createdAt: '2025-01-01T10:01:00Z',
  },
  {
    id: 'm3', ticketId: 't1', body: 'Internal note', isInternal: true,
    authorUserId: 'agent-1', authorName: 'Omar', createdAt: '2025-01-01T10:02:00Z',
  },
];

const mockPage: TicketMessagePage = {
  items: mockMessages, totalCount: 3, page: 1, pageSize: 20, totalPages: 1,
};

describe('MessageThreadComponent', () => {
  let fixture: ComponentFixture<MessageThreadComponent>;
  let component: MessageThreadComponent;

  const mockTicketService = { getMessages: vi.fn() };
  const mockConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    off: vi.fn(),
    stop: vi.fn(),
  };
  const mockSignalRService = { getConnection: vi.fn().mockReturnValue(mockConnection) };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.getMessages.mockReturnValue(of(mockPage));
    mockConnection.start.mockResolvedValue(undefined);
    mockSignalRService.getConnection.mockReturnValue(mockConnection);

    await TestBed.configureTestingModule({
      imports: [MessageThreadComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: SignalRService, useValue: mockSignalRService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MessageThreadComponent);
    component = fixture.componentInstance;
    component.ticketId = 't1';
    fixture.detectChanges();
  });

  it('should create and load messages', () => {
    expect(component).toBeTruthy();
    expect(component.messages().length).toBe(3);
  });

  it('should connect to SignalR hub', () => {
    expect(mockSignalRService.getConnection).toHaveBeenCalledWith('/hubs/notifications');
    expect(mockConnection.start).toHaveBeenCalled();
  });

  it('should show "Load earlier messages" button when more messages exist', () => {
    component.totalCount.set(10);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Load earlier messages');
  });

  it('should not show load-more when all messages loaded', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Load earlier messages');
  });
});
