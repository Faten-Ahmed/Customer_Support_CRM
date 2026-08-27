import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PortalLoginComponent } from './portal-login.component';
import { AuthService } from '../../../auth/auth.service';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('PortalLoginComponent', () => {
  let component: PortalLoginComponent;
  let fixture: ComponentFixture<PortalLoginComponent>;
  let authServiceMock: { portalLogin: ReturnType<typeof vi.fn>; resendVerificationEmail: ReturnType<typeof vi.fn> };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authServiceMock = {
      portalLogin: vi.fn(),
      resendVerificationEmail: vi.fn(),
    };
    routerMock = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        PortalLoginComponent,
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

    fixture = TestBed.createComponent(PortalLoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have invalid form when empty', () => {
    expect(component.loginForm.valid).toBe(false);
  });

  it('should require valid email format', () => {
    component.loginForm.controls['email'].setValue('bademail');
    expect(component.loginForm.controls['email'].valid).toBe(false);
  });

  it('should require password of at least 8 characters', () => {
    component.loginForm.controls['password'].setValue('short');
    expect(component.loginForm.controls['password'].valid).toBe(false);
  });

  it('should accept password of 8+ characters', () => {
    component.loginForm.controls['password'].setValue('ValidPass1');
    expect(component.loginForm.controls['password'].valid).toBe(true);
  });

  it('should call portalLogin on valid submit and navigate to /portal/dashboard', async () => {
    authServiceMock.portalLogin.mockReturnValue(
      of({ accessToken: 'tok', user: { id: '2', email: 'c@c.com', role: 'PortalUser', passwordMustChange: false } })
    );

    component.loginForm.setValue({ email: 'customer@example.com', password: 'Password1!' });
    component.onSubmit();
    await fixture.whenStable();

    expect(authServiceMock.portalLogin).toHaveBeenCalledWith('customer@example.com', 'Password1!');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/portal/dashboard']);
  });

  it('should show EMAIL_NOT_VERIFIED message on 401 with that code', async () => {
    authServiceMock.portalLogin.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 401, error: { code: 'EMAIL_NOT_VERIFIED' } }))
    );

    component.loginForm.setValue({ email: 'unverified@example.com', password: 'Password1!' });
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('EMAIL_NOT_VERIFIED');
    const msg = fixture.nativeElement.querySelector('[data-testid="email-not-verified-msg"]');
    expect(msg).toBeTruthy();
    expect(msg.textContent).toContain('verify your email');
  });

  it('should call resendVerificationEmail when resend button is clicked', async () => {
    authServiceMock.resendVerificationEmail.mockReturnValue(of({ message: 'Sent' }));
    component.unverifiedEmail.set('unverified@example.com');
    component.errorCode.set('EMAIL_NOT_VERIFIED');
    fixture.detectChanges();

    const resendBtn = fixture.nativeElement.querySelector('[data-testid="resend-verification-btn"]');
    resendBtn.click();
    await fixture.whenStable();

    expect(authServiceMock.resendVerificationEmail).toHaveBeenCalledWith('unverified@example.com');
  });

  it('should toggle password visibility', () => {
    expect(component.hidePassword()).toBe(true);
    component.hidePassword.set(false);
    expect(component.hidePassword()).toBe(false);
  });
});
