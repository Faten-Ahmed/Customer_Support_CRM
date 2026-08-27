// src/app/customers/customer-detail/customer-detail.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { CustomerDetailComponent } from './customer-detail.component';
import { CustomerService, Customer } from '../services/customer.service';
import { AuthStore } from '../../auth/auth.store';

const mockCustomer: Customer = {
  id: '42',
  fullName: 'Ali Hassan',
  email: 'ali@example.com',
  phone: '050-111-2222',
  isVip: true,
  isActive: true,
  createdAt: '2025-01-01',
};

describe('CustomerDetailComponent', () => {
  let fixture: ComponentFixture<CustomerDetailComponent>;
  let component: CustomerDetailComponent;

  const mockCustomerService = {
    getById: vi.fn().mockReturnValue(of(mockCustomer)),
    update: vi.fn(),
    deactivate: vi.fn(),
  };

  const mockAuthStore = {
    user: () => ({ sub: 'u1', fullName: 'Admin User', role: 'Admin' }),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockCustomerService.getById.mockReturnValue(of(mockCustomer));

    await TestBed.configureTestingModule({
      imports: [CustomerDetailComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: ActivatedRoute, useValue: { params: of({ id: '42' }) } },
        { provide: AuthStore, useValue: mockAuthStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load customer', () => {
    expect(component).toBeTruthy();
    expect(mockCustomerService.getById).toHaveBeenCalledWith('42');
    expect(component.customer()?.fullName).toBe('Ali Hassan');
  });

  it('should display VIP badge', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('VIP');
  });

  it('should enter edit mode on enterEditMode() call', () => {
    component.enterEditMode();
    expect(component.editing()).toBe(true);
  });

  it('should call update() on saveChanges and exit edit mode', async () => {
    mockCustomerService.update.mockReturnValue(of({ ...mockCustomer, phone: '0501112233' }));
    component.enterEditMode();
    component.editForm.patchValue({ phone: '0501112233' });
    component.saveChanges();
    await fixture.whenStable();
    expect(mockCustomerService.update).toHaveBeenCalled();
    expect(component.editing()).toBe(false);
  });

  it('should show deactivate button for Admin role', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Deactivate');
  });
});
