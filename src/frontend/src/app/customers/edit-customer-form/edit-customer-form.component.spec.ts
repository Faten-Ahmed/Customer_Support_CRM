// src/app/customers/edit-customer-form/edit-customer-form.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { EditCustomerFormComponent } from './edit-customer-form.component';
import { CustomerService, Customer } from '../services/customer.service';

const mockCustomer: Customer = {
  id: 'c-1',
  fullName: 'Alice',
  email: 'alice@example.com',
  phone: '555-0100',
  companyName: 'Acme',
  isVip: false,
  isActive: true,
  createdAt: '2025-01-01',
};

describe('EditCustomerFormComponent', () => {
  let fixture: ComponentFixture<EditCustomerFormComponent>;
  let component: EditCustomerFormComponent;

  const mockCustomerService = { update: vi.fn(), getById: vi.fn() };
  const mockRouter = { navigate: vi.fn() };
  const mockSnackBar = { open: vi.fn() };
  const mockActivatedRoute = { snapshot: { paramMap: { get: vi.fn().mockReturnValue(null) } } };

  beforeEach(async () => {
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [EditCustomerFormComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: Router, useValue: mockRouter },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
      ],
    })
    .overrideProvider(MatSnackBar, { useValue: mockSnackBar })
    .compileComponents();

    fixture = TestBed.createComponent(EditCustomerFormComponent);
    component = fixture.componentInstance;
    component.customer = mockCustomer;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should populate form with customer data', () => {
    expect(component.form.get('fullName')?.value).toBe('Alice');
    expect(component.form.get('phone')?.value).toBe('555-0100');
  });

  it('should disable the email field (BR-CUST-002)', () => {
    expect(component.form.get('email')?.disabled).toBe(true);
  });

  it('should not include email in the PUT payload', async () => {
    mockCustomerService.update.mockReturnValue(of(mockCustomer));
    component.form.patchValue({ fullName: 'Alice Updated' });
    component.onSubmit();
    await fixture.whenStable();

    const callArg = (mockCustomerService.update as ReturnType<typeof vi.fn>).mock.calls[0][1];
    expect(callArg).not.toHaveProperty('email');
  });

  it('should call update() with customer id and navigate on success', async () => {
    mockCustomerService.update.mockReturnValue(of(mockCustomer));
    component.onSubmit();
    await fixture.whenStable();
    expect(mockCustomerService.update).toHaveBeenCalledWith('c-1', expect.any(Object));
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/customers', 'c-1']);
    expect(mockSnackBar.open).toHaveBeenCalledWith('Customer updated successfully.', 'Close', expect.any(Object));
  });

  it('should show error snackbar on unexpected error', async () => {
    mockCustomerService.update.mockReturnValue(throwError(() => new Error('Server error')));
    component.onSubmit();
    await fixture.whenStable();
    expect(mockSnackBar.open).toHaveBeenCalledWith('An error occurred. Please try again.', 'Close', expect.any(Object));
  });
});
