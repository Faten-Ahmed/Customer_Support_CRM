// src/app/customers/create-customer-form/create-customer-form.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CreateCustomerFormComponent } from './create-customer-form.component';
import { CustomerService } from '../services/customer.service';

describe('CreateCustomerFormComponent', () => {
  let fixture: ComponentFixture<CreateCustomerFormComponent>;
  let component: CreateCustomerFormComponent;

  const mockCustomerService = { create: vi.fn() };
  const mockRouter = { navigate: vi.fn() };
  const mockSnackBar = { open: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [CreateCustomerFormComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: Router, useValue: mockRouter },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    })
    .overrideProvider(MatSnackBar, { useValue: mockSnackBar })
    .compileComponents();

    fixture = TestBed.createComponent(CreateCustomerFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should have an invalid form when required fields are empty', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('should mark fullName as required', () => {
    const ctrl = component.form.get('fullName')!;
    ctrl.setValue('');
    ctrl.markAsTouched();
    expect(ctrl.hasError('required')).toBe(true);
  });

  it('should mark email as invalid for bad format', () => {
    const ctrl = component.form.get('email')!;
    ctrl.setValue('not-an-email');
    ctrl.markAsTouched();
    expect(ctrl.hasError('email')).toBe(true);
  });

  it('should not call service when form is invalid on submit', () => {
    component.onSubmit();
    expect(mockCustomerService.create).not.toHaveBeenCalled();
  });

  it('should call CustomerService.create with form values on valid submit', async () => {
    mockCustomerService.create.mockReturnValue(of({ id: 'c-1', fullName: 'Alice', email: 'alice@example.com' }));

    component.form.setValue({
      fullName: 'Alice',
      email: 'alice@example.com',
      phone: '',
      companyName: '',
      country: '',
      city: '',
    });

    component.onSubmit();
    await fixture.whenStable();

    expect(mockCustomerService.create).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/customers', 'c-1']);
    expect(mockSnackBar.open).toHaveBeenCalledWith('Customer created successfully.', 'Close', expect.any(Object));
  });

  it('should set emailAlreadyExists error on 409', async () => {
    mockCustomerService.create.mockReturnValue(
      throwError(() => ({ code: 'EMAIL_ALREADY_EXISTS' }))
    );

    component.form.setValue({
      fullName: 'Carol', email: 'carol@example.com',
      phone: '', companyName: '', country: '', city: '',
    });
    component.onSubmit();
    await fixture.whenStable();

    expect(component.form.get('email')!.hasError('emailAlreadyExists')).toBe(true);
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should show submitting = false initially', () => {
    expect(component.submitting()).toBe(false);
  });
});
