import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { CustomerListComponent } from './customer-list.component';
import { CustomerService, CustomerPage } from '../services/customer.service';

const mockPage: CustomerPage = {
  data: [
    { id: '1', fullName: 'Ali Hassan', email: 'ali@example.com', isVip: true, isActive: true, ticketCount: 3, createdAt: '2025-01-01' },
    { id: '2', fullName: 'Sara Omar', email: 'sara@example.com', isVip: false, isActive: false, ticketCount: 0, createdAt: '2025-02-01' },
  ],
  total: 2,
  page: 1,
  pageSize: 20,
};

describe('CustomerListComponent', () => {
  let fixture: ComponentFixture<CustomerListComponent>;
  let component: CustomerListComponent;
  const mockCustomerService = {
    list: vi.fn().mockReturnValue(of(mockPage)),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockCustomerService.list.mockReturnValue(of(mockPage));

    await TestBed.configureTestingModule({
      imports: [CustomerListComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: CustomerService, useValue: mockCustomerService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load customers on init', () => {
    expect(component).toBeTruthy();
    expect(mockCustomerService.list).toHaveBeenCalled();
    expect(component.customers().length).toBe(2);
  });

  it('should toggle VIP filter chip', () => {
    component.toggleVipFilter();
    expect(component.vipOnly()).toBe(true);
    component.toggleVipFilter();
    expect(component.vipOnly()).toBe(false);
  });

  it('should show empty state when total is 0', async () => {
    mockCustomerService.list.mockReturnValue(of({ ...mockPage, data: [], total: 0 }));
    component.loadCustomers();
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('No customers found');
  });

  it('should display VIP badge for VIP customers', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('VIP');
  });
});
