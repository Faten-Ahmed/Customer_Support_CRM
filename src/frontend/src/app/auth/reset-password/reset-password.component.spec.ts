import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from '../auth.service';
import { Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { vi } from 'vitest';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let authServiceSpy: { resetPassword: ReturnType<typeof vi.fn> };
  let routerSpy: { navigate: ReturnType<typeof vi.fn> };

  function createComponent(token: string = 'valid-token') {
    const activatedRouteStub = {
      snapshot: { queryParamMap: { get: (key: string) => (key === 'token' ? token : null) } },
    };

    TestBed.configureTestingModule({
      imports: [
        ResetPasswordComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ],
    });

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    authServiceSpy = { resetPassword: vi.fn() };
    routerSpy = { navigate: vi.fn() };
  });

  it('should create', () => {
    createComponent();
    expect(component).toBeTruthy();
  });

  it('should read token from query params on init', () => {
    createComponent('my-reset-token');
    expect(component.token()).toBe('my-reset-token');
  });

  it('should be invalid when passwords are empty', () => {
    createComponent();
    expect(component.resetForm.valid).toBe(false);
  });

  it('should enforce minimum 8 character password', () => {
    createComponent();
    component.resetForm.controls['password'].setValue('short');
    expect(component.resetForm.controls['password'].valid).toBe(false);
  });

  it('should fail validation when passwords do not match', () => {
    createComponent();
    component.resetForm.setValue({ password: 'Password1!', confirmPassword: 'Different1!' });
    expect(component.resetForm.hasError('passwordMismatch')).toBe(true);
  });

  it('should be valid when passwords match and meet requirements', () => {
    createComponent();
    component.resetForm.setValue({ password: 'Password1!', confirmPassword: 'Password1!' });
    expect(component.resetForm.valid).toBe(true);
  });

  it('should call resetPassword with token and new password on valid submit', async () => {
    createComponent('valid-token');
    authServiceSpy.resetPassword.mockReturnValue(of({ message: 'Password reset successfully.' }));

    component.resetForm.setValue({ password: 'NewPassword1!', confirmPassword: 'NewPassword1!' });
    component.onSubmit();
    await fixture.whenStable();

    expect(authServiceSpy.resetPassword).toHaveBeenCalledWith('valid-token', 'NewPassword1!', 'NewPassword1!');
  });

  it('should show success message after successful reset', async () => {
    createComponent('valid-token');
    authServiceSpy.resetPassword.mockReturnValue(of({ message: 'Password reset successfully.' }));

    component.resetForm.setValue({ password: 'NewPassword1!', confirmPassword: 'NewPassword1!' });
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.successMessage()).toContain('reset');
    const el = fixture.nativeElement.querySelector('[data-testid="reset-success"]');
    expect(el).toBeTruthy();
  });

  it('should set errorCode to TOKEN_EXPIRED on 400 TOKEN_EXPIRED', async () => {
    createComponent('expired-token');
    authServiceSpy.resetPassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { code: 'TOKEN_EXPIRED' } }))
    );

    component.resetForm.setValue({ password: 'Password1!', confirmPassword: 'Password1!' });
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('TOKEN_EXPIRED');
    const el = fixture.nativeElement.querySelector('[data-testid="token-error"]');
    expect(el).toBeTruthy();
  });

  it('should compute password strength as weak for short passwords', () => {
    createComponent();
    expect(component.getPasswordStrength('abc')).toBe('weak');
  });

  it('should compute password strength as medium for 8+ chars no special', () => {
    createComponent();
    expect(component.getPasswordStrength('Password1')).toBe('medium');
  });

  it('should compute password strength as strong for complex passwords', () => {
    createComponent();
    expect(component.getPasswordStrength('P@ssword1!')).toBe('strong');
  });
});
