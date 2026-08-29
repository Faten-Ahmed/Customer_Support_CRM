import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService, LoginResponse, PortalRegisterPayload } from './auth.service';
import { AuthStore } from './auth.store';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

function makeJwt(payload: object): string {
  return `header.${btoa(JSON.stringify(payload))}.sig`;
}

describe('AuthService', () => {
  let service: AuthService;
  let authStore: AuthStore;
  let httpMock: HttpTestingController;
  let routerSpy: { navigate: ReturnType<typeof vi.fn>; navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    routerSpy = { navigate: vi.fn(), navigateByUrl: vi.fn() };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthService,
        { provide: Router, useValue: routerSpy },
      ],
    });

    service = TestBed.inject(AuthService);
    authStore = TestBed.inject(AuthStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should authenticate and persist token on successful login', () => {
    const token = makeJwt({ sub: '1', role: 'Agent', passwordMustChange: false });
    const mockResponse: LoginResponse = {
      accessToken: token,
      refreshToken: '',
      requiresPasswordChange: false,
      userId: '1',
      email: 'staff@azmsquad.com',
      firstName: 'Staff',
      lastName: 'User',
      role: 'Agent',
    };

    service.login('staff@azmsquad.com', 'Password1!').subscribe(res => {
      expect(res.accessToken).toBe(token);
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'staff@azmsquad.com', password: 'Password1!' });
    req.flush(mockResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(authStore.getToken()).toBe(token);
    expect(service.currentUser()).toEqual(mockResponse);
  });

  it('should store token in localStorage on successful login', () => {
    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    expect(localStorage.getItem('access_token')).toBe('tok');
  });

  it('should pass through 401 error so component can handle it', () => {
    let errorStatus = 0;
    service.login('x@x.com', 'wrong').subscribe({
      error: err => (errorStatus = err.status),
    });

    httpMock.expectOne('/api/v1/auth/login').flush(
      { code: 'ACCOUNT_INACTIVE' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(errorStatus).toBe(401);
  });

  it('should pass through 423 error so component can redirect to /change-password', () => {
    let errorStatus = 0;
    service.login('x@x.com', 'pass').subscribe({
      error: err => (errorStatus = err.status),
    });

    httpMock.expectOne('/api/v1/auth/login').flush(
      { code: 'PASSWORD_CHANGE_REQUIRED' },
      { status: 423, statusText: 'Locked' }
    );

    expect(errorStatus).toBe(423);
  });

  it('isAuthenticated() should return true when token is set', () => {
    expect(service.isAuthenticated()).toBe(false);

    const token = makeJwt({ sub: '1', role: 'Agent', passwordMustChange: false });
    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: token,
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    expect(service.isAuthenticated()).toBe(true);
  });

  it('logout() should clear authentication state', () => {
    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    service.logout();
    httpMock.expectOne('/api/v1/auth/logout').flush({});

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(authStore.getToken()).toBeNull();
  });
});

describe('AuthService — portal methods', () => {
  let service: AuthService;
  let authStore: AuthStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthService,
      ],
    });
    service = TestBed.inject(AuthService);
    authStore = TestBed.inject(AuthStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('portalLogin() should POST to /api/v1/auth/login', () => {
    service.portalLogin('customer@example.com', 'pass123').subscribe(res => {
      expect(res.accessToken).toBe('portal.jwt');
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ accessToken: 'portal.jwt', user: { id: '2', email: 'customer@example.com', role: 'PortalUser', passwordMustChange: false } });
  });

  it('portalLogin() should store token on success', () => {
    const token = makeJwt({ sub: '2', role: 'Customer', passwordMustChange: false });
    service.portalLogin('customer@example.com', 'pass123').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: token,
      user: { id: '2', email: 'customer@example.com', role: 'PortalUser', passwordMustChange: false },
    });

    expect(service.isAuthenticated()).toBe(true);
    expect(authStore.getToken()).toBe(token);
  });

  it('portalLogin() should pass through 401 EMAIL_NOT_VERIFIED error', () => {
    let errorCode = '';
    service.portalLogin('unverified@example.com', 'pass123').subscribe({
      error: err => (errorCode = err.error?.code),
    });

    httpMock.expectOne('/api/v1/auth/login').flush(
      { code: 'EMAIL_NOT_VERIFIED' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(errorCode).toBe('EMAIL_NOT_VERIFIED');
  });

  it('portalRegister() should POST to /api/v1/auth/portal/register', () => {
    const payload: PortalRegisterPayload = {
      fullName: 'Jane Doe',
      fullNameAr: 'جين دو',
      email: 'jane@example.com',
      password: 'Secure123!',
      confirmPassword: 'Secure123!',
    };

    service.portalRegister(payload).subscribe(res => {
      expect(res.message).toBe('Check your email to activate your account');
    });

    const req = httpMock.expectOne('/api/v1/auth/portal/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ message: 'Check your email to activate your account' });
  });

  it('resendVerificationEmail() should POST to /api/v1/auth/portal/resend-verification', () => {
    service.resendVerificationEmail('user@example.com').subscribe();
    const req = httpMock.expectOne('/api/v1/auth/portal/resend-verification');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush({ message: 'Sent' });
  });
});

describe('AuthService — changePassword', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthService,
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should POST to /api/v1/auth/change-password-first-login with correct body', () => {
    service.changePassword('OldPass1!', 'NewPass2@', 'NewPass2@').subscribe(res => {
      expect(res.message).toContain('changed');
    });

    const req = httpMock.expectOne('/api/v1/auth/change-password-first-login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      currentPassword: 'OldPass1!',
      newPassword: 'NewPass2@',
      confirmPassword: 'NewPass2@',
    });
    req.flush({ message: 'Password changed successfully.' });
  });

  it('should propagate 422 INVALID_CURRENT_PASSWORD error', () => {
    let errorCode = '';
    service.changePassword('wrong', 'NewPass2@', 'NewPass2@').subscribe({
      error: (err: { error?: { code?: string } }) => (errorCode = err.error?.code ?? ''),
    });

    const req = httpMock.expectOne('/api/v1/auth/change-password-first-login');
    req.flush({ code: 'INVALID_CURRENT_PASSWORD' }, { status: 422, statusText: 'Unprocessable Entity' });
    expect(errorCode).toBe('INVALID_CURRENT_PASSWORD');
  });
});

describe('AuthService — password reset methods', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthService,
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('forgotPassword() should POST to /api/v1/auth/forgot-password', () => {
    service.forgotPassword('user@example.com').subscribe(res => {
      expect(res.message).toBeTruthy();
    });

    const req = httpMock.expectOne('/api/v1/auth/forgot-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush({ message: 'If that address is registered, an email has been sent.' });
  });

  it('resetPassword() should POST to /api/v1/auth/reset-password with token and new password', () => {
    service.resetPassword('reset-token-abc', 'NewPassword1!', 'NewPassword1!').subscribe(res => {
      expect(res.message).toContain('reset');
    });

    const req = httpMock.expectOne('/api/v1/auth/reset-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      token: 'reset-token-abc',
      password: 'NewPassword1!',
      confirmPassword: 'NewPassword1!',
    });
    req.flush({ message: 'Password reset successfully.' });
  });

  it('resetPassword() should pass through 400 INVALID_TOKEN error', () => {
    let errorCode = '';
    service.resetPassword('bad-token', 'Pass1!pass', 'Pass1!pass').subscribe({
      error: (err: { error?: { code?: string } }) => (errorCode = err.error?.code ?? ''),
    });

    httpMock.expectOne('/api/v1/auth/reset-password').flush(
      { code: 'INVALID_TOKEN' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(errorCode).toBe('INVALID_TOKEN');
  });

  it('resetPassword() should pass through 400 TOKEN_EXPIRED error', () => {
    let errorCode = '';
    service.resetPassword('expired-token', 'Pass1!pass', 'Pass1!pass').subscribe({
      error: (err: { error?: { code?: string } }) => (errorCode = err.error?.code ?? ''),
    });

    httpMock.expectOne('/api/v1/auth/reset-password').flush(
      { code: 'TOKEN_EXPIRED' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(errorCode).toBe('TOKEN_EXPIRED');
  });
});
