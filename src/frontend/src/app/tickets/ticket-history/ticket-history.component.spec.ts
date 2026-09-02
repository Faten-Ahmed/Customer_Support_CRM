import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TicketHistoryComponent } from './ticket-history.component';
import { TicketService, TicketHistoryEntry } from '../ticket.service';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

const MOCK_ENTRIES: TicketHistoryEntry[] = [
  {
    fieldChanged: 'Status',
    oldValue: 'New',
    newValue: 'InProgress',
    changedByName: 'Alice Agent',
    changedAt: '2026-07-01T09:15:00Z',
  },
  {
    fieldChanged: 'Priority',
    oldValue: 'Medium',
    newValue: 'High',
    changedByName: 'Bob Manager',
    changedAt: '2026-07-01T10:00:00Z',
  },
  {
    fieldChanged: 'AssignedTo',
    oldValue: null,
    newValue: 'Alice Agent',
    changedByName: 'Bob Manager',
    changedAt: '2026-07-01T10:30:00Z',
  },
];

const mockPage = { items: MOCK_ENTRIES, totalCount: 3, page: 1, pageSize: 20, totalPages: 1 };
const emptyPage = { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };

describe('TicketHistoryComponent', () => {
  let fixture: ComponentFixture<TicketHistoryComponent>;
  let component: TicketHistoryComponent;
  const mockTicketService = { getHistory: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.getHistory.mockReturnValue(of(mockPage));

    await TestBed.configureTestingModule({
      imports: [TicketHistoryComponent, NoopAnimationsModule],
      providers: [{ provide: TicketService, useValue: mockTicketService }],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketHistoryComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-42';
    fixture.detectChanges();
  });

  it('should create and call getHistory on init', () => {
    expect(component).toBeTruthy();
    expect(mockTicketService.getHistory).toHaveBeenCalledWith('ticket-42');
  });

  it('should render all history entries', () => {
    const entries = fixture.debugElement.queryAll(By.css('[data-testid="history-entry"]'));
    expect(entries.length).toBe(3);
  });

  it('should display derived label and changedByName in each entry', () => {
    const entries = fixture.debugElement.queryAll(By.css('[data-testid="history-entry"]'));
    expect(entries[0].nativeElement.textContent).toContain('Status');
    expect(entries[0].nativeElement.textContent).toContain('New');
    expect(entries[0].nativeElement.textContent).toContain('InProgress');
    expect(entries[0].nativeElement.textContent).toContain('Alice Agent');
    expect(entries[1].nativeElement.textContent).toContain('Priority');
    expect(entries[1].nativeElement.textContent).toContain('Bob Manager');
  });

  it('should display entries in DOM order (oldest first as returned by API)', () => {
    const labels = fixture.debugElement
      .queryAll(By.css('[data-testid="history-label"]'))
      .map(el => el.nativeElement.textContent.trim());
    expect(labels[0]).toContain('Status');
    expect(labels[2]).toContain('AssignedTo');
  });

  it('should apply distinct CSS class per fieldChanged value', () => {
    const icons = fixture.debugElement.queryAll(By.css('[data-testid="action-icon"]'));
    expect(icons[0].classes['action-Status']).toBe(true);
    expect(icons[1].classes['action-Priority']).toBe(true);
    expect(icons[2].classes['action-AssignedTo']).toBe(true);
  });

  it('should show correct icon name per fieldChanged value', () => {
    expect(component.iconFor('Status')).toBe('swap_horiz');
    expect(component.iconFor('Priority')).toBe('low_priority');
    expect(component.iconFor('AssignedTo')).toBe('person');
    expect(component.iconFor('EscalationReason')).toBe('warning');
    expect(component.iconFor('Transfer')).toBe('transfer_within_a_station');
    expect(component.iconFor('unknown')).toBe('history');
  });

  it('should show empty state when history is empty', async () => {
    mockTicketService.getHistory.mockReturnValue(of(emptyPage));
    fixture = TestBed.createComponent(TicketHistoryComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-42';
    fixture.detectChanges();
    const empty = fixture.debugElement.query(By.css('[data-testid="empty-state"]'));
    expect(empty).not.toBeNull();
    expect(empty.nativeElement.textContent).toContain('No history');
  });
});
