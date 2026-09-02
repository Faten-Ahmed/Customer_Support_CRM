import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { TicketDetailComponent } from './ticket-detail.component';
import { TicketService, TicketDetail } from '../ticket.service';
import { SignalRService } from '../../shared/services/signalr.service';

const mockTicket: TicketDetail = {
  id: 't-1',
  ticketNumber: 'TK-0001',
  subject: 'Login issue',
  description: 'Cannot log in.',
  customerName: 'Alice',
  customerId: 'c-1',
  status: 'New',
  priority: 'High',
  channel: 'Email',
  departmentName: 'Support',
  categoryName: 'Authentication',
  createdAt: '2025-01-01T10:00:00Z',
  updatedAt: '2025-01-01T10:00:00Z',
};

const emptyPage = { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };

describe('TicketDetailComponent', () => {
  let fixture: ComponentFixture<TicketDetailComponent>;
  let component: TicketDetailComponent;
  const mockTicketService = {
    getById: vi.fn().mockReturnValue(of(mockTicket)),
    getMessages: vi.fn().mockReturnValue(of(emptyPage)),
    getHistory: vi.fn().mockReturnValue(of(emptyPage)),
    getAttachments: vi.fn().mockReturnValue(of([])),
    addMessage: vi.fn().mockReturnValue(of({})),
    uploadAttachment: vi.fn().mockReturnValue(of({})),
    deleteAttachment: vi.fn().mockReturnValue(of(null)),
    getAgents: vi.fn().mockReturnValue(of([])),
    list: vi.fn().mockReturnValue(of(emptyPage)),
    getSla: vi.fn().mockReturnValue(of({
      isPaused: false,
      firstResponse: { dueAt: '', elapsedPercent: 0, breached: false, remainingLabel: '' },
      resolution: { dueAt: '', elapsedPercent: 0, breached: false, remainingLabel: '' },
    })),
  };

  const mockHubConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    invoke: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    off: vi.fn(),
    state: 'Disconnected',
  };

  const mockSignalRService = {
    getConnection: vi.fn().mockReturnValue(mockHubConnection),
  };

  const activatedRouteStub = {
    snapshot: { paramMap: { get: (k: string) => (k === 'id' ? 't-1' : null) } },
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.getById.mockReturnValue(of(mockTicket));
    mockTicketService.getMessages.mockReturnValue(of(emptyPage));
    mockTicketService.getHistory.mockReturnValue(of(emptyPage));
    mockTicketService.getAttachments.mockReturnValue(of([]));
    mockTicketService.getById.mockReturnValue(of(mockTicket));

    await TestBed.configureTestingModule({
      imports: [TicketDetailComponent, NoopAnimationsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
        { provide: SignalRService, useValue: mockSignalRService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call TicketService.getById with the route id on init', () => {
    expect(mockTicketService.getById).toHaveBeenCalledWith('t-1');
  });

  it('should populate the ticket signal with API response', () => {
    expect(component.ticket()).toEqual(mockTicket);
  });

  it('should set loading to false after data loads', () => {
    expect(component.loading()).toBe(false);
  });

  it('should expose the ticket subject', () => {
    expect(component.ticket()?.subject).toBe('Login issue');
  });

  it('should expose the ticket status', () => {
    expect(component.ticket()?.status).toBe('New');
  });

  it('should expose the ticket priority', () => {
    expect(component.ticket()?.priority).toBe('High');
  });

  it('should expose the customer name', () => {
    expect(component.ticket()?.customerName).toBe('Alice');
  });

  it('should expose department name', () => {
    expect(component.ticket()?.departmentName).toBe('Support');
  });

  it('should expose active tab defaulting to messages', () => {
    expect(component.activeTab()).toBe('messages');
  });

  it('should switch active tab when setActiveTab is called', () => {
    component.setActiveTab('history');
    expect(component.activeTab()).toBe('history');
  });

  it('should expose aiPanelOpen signal defaulting to false', () => {
    expect(component.aiPanelOpen()).toBe(false);
  });

  it('should toggle aiPanelOpen when toggleAiPanel() is called', () => {
    component.toggleAiPanel();
    expect(component.aiPanelOpen()).toBe(true);
    component.toggleAiPanel();
    expect(component.aiPanelOpen()).toBe(false);
  });
});
