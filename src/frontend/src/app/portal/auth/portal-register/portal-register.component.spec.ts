import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PortalRegisterComponent } from './portal-register.component';
import { AuthService } from '../../../auth/auth.service';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('PortalRegisterComponent', () => {
  let component: PortalRegisterComponent;
  let fixture: ComponentFixture<PortalRegisterComponent>;
  let authServiceMock: { portalRegister: ReturnType<typeof vi.fn> };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authServiceMock = { portalRegister: vi.fn() };
    routerMock = { navigate: vi.fn().mockResolvedValue(true) };

    await TestBed.configureTestingModule({
      imports: [
        PortalRegisterComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalRegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should be invalid when empty', () => {
    expect(component.registerForm.valid).toBe(false);
  });

  it('fullName should be required', () => {
    component.registerForm.controls['fullName'].setValue('');
    expect(component.registerForm.controls['fullName'].valid).toBe(false);
  });

  it('password must be at least 8 characters', () => {
    component.registerForm.controls['password'].setValue('short');
    expect(component.registerForm.controls['password'].valid).toBe(false);
  });

  it('form should be invalid when passwords do not match', () => {
    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Different1!',
    });
    expect(component.registerForm.hasError('passwordMismatch')).toBe(true);
  });

  it('form should be valid when all fields are correct and passwords match', () => {
    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    expect(component.registerForm.valid).toBe(true);
  });

  it('should call portalRegister on valid submit', async () => {
    authServiceMock.portalRegister.mockReturnValue(
      of({ message: 'Check your email to activate your account' })
    );

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    component.onSubmit();
    await fixture.whenStable();

    expect(authServiceMock.portalRegister).toHaveBeenCalledWith({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
  });

  it('should navigate to verify-email after registration', async () => {
    authServiceMock.portalRegister.mockReturnValue(
      of({ message: 'Check your email to activate your account' })
    );

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    component.onSubmit();
    await fixture.whenStable();

    expect(routerMock.navigate).toHaveBeenCalledWith(
      ['/portal/verify-email'],
      { queryParams: { email: 'jane@example.com' } }
    );
  });

  it('should show error on duplicate email (409)', async () => {
    authServiceMock.portalRegister.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'EMAIL_ALREADY_EXISTS' } }))
    );

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'existing@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('EMAIL_ALREADY_EXISTS');
  });

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    expect(authServiceMock.portalRegister).not.toHaveBeenCalled();
  });
});
