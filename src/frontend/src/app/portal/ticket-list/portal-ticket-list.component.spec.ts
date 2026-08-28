import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalTicketListComponent } from './portal-ticket-list.component';
import { PortalTicketService, PortalTicketPage } from '../services/portal-ticket.service';

const mockPage: PortalTicketPage = {
  items: [
    { id: 't1', ticketNumber: 'TKT-001', subject: 'Cannot login', status: 'New', priority: 'High', createdAt: '2026-01-01T10:00:00Z', category: null },
    { id: 't2', ticketNumber: 'TKT-002', subject: 'Billing error', status: 'Resolved', priority: 'Low', createdAt: '2026-01-02T09:00:00Z', category: 'Billing' },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

describe('PortalTicketListComponent', () => {
  let fixture: ComponentFixture<PortalTicketListComponent>;
  let component: PortalTicketListComponent;
  let service: { list: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    service = { list: vi.fn().mockReturnValue(of(mockPage)) };

    await TestBed.configureTestingModule({
      imports: [PortalTicketListComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PortalTicketService, useValue: service },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalTicketListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create and load tickets', () => {
    expect(component).toBeTruthy();
    expect(component.tickets().length).toBe(2);
  });

  it('should render ticket subjects', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Cannot login');
    expect(el.textContent).toContain('Billing error');
  });

  it('should show ticket status chips', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('New');
    expect(el.textContent).toContain('Resolved');
  });

  it('should call list() with status filter when filter changes', async () => {
    component.statusFilter.setValue('Resolved');
    await fixture.whenStable();
    expect(service.list).toHaveBeenCalledWith('Resolved');
  });

  it('shows empty state when no tickets', async () => {
    service.list.mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }));
    component['load']();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.tickets().length).toBe(0);
  });
});
