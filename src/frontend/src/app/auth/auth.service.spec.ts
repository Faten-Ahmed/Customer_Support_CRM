import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService, LoginResponse, PortalRegisterPayload } from './auth.service';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: { navigate: ReturnType<typeof vi.fn>; navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    routerSpy = { navigate: vi.fn(), navigateByUrl: vi.fn() };

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
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should store access token in memory on successful login', () => {
    const mockResponse: LoginResponse = {
      accessToken: 'jwt.token.here',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    };

    service.login('staff@azmsquad.com', 'Password1!').subscribe(res => {
      expect(res.accessToken).toBe('jwt.token.here');
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'staff@azmsquad.com', password: 'Password1!' });
    req.flush(mockResponse);

    expect(service.accessToken()).toBe('jwt.token.here');
    expect(service.currentUser()).toEqual(mockResponse.user);
  });

  it('should NOT store token in localStorage on successful login', () => {
    const setItemSpy = vi.spyOn(Storage.prototype, 'setItem');

    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    expect(setItemSpy).not.toHaveBeenCalled();
    setItemSpy.mockRestore();
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

    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    expect(service.isAuthenticated()).toBe(true);
  });

  it('logout() should clear the access token signal', () => {
    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    service.logout();
    // Flush the logout HTTP request to satisfy httpMock.verify()
    httpMock.expectOne('/api/v1/auth/logout').flush({});

    expect(service.accessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
  });
});

describe('AuthService — portal methods', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
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

  afterEach(() => httpMock.verify());

  it('portalLogin() should POST to /api/v1/portal/auth/login', () => {
    service.portalLogin('customer@example.com', 'pass123').subscribe(res => {
      expect(res.accessToken).toBe('portal.jwt');
    });

    const req = httpMock.expectOne('/api/v1/portal/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ accessToken: 'portal.jwt', user: { id: '2', email: 'customer@example.com', role: 'PortalUser', passwordMustChange: false } });
  });

  it('portalLogin() should store token on success', () => {
    service.portalLogin('customer@example.com', 'pass123').subscribe();
    httpMock.expectOne('/api/v1/portal/auth/login').flush({
      accessToken: 'portal.jwt',
      user: { id: '2', email: 'customer@example.com', role: 'PortalUser', passwordMustChange: false },
    });

    expect(service.accessToken()).toBe('portal.jwt');
  });

  it('portalLogin() should pass through 401 EMAIL_NOT_VERIFIED error', () => {
    let errorCode = '';
    service.portalLogin('unverified@example.com', 'pass123').subscribe({
      error: err => (errorCode = err.error?.code),
    });

    httpMock.expectOne('/api/v1/portal/auth/login').flush(
      { code: 'EMAIL_NOT_VERIFIED' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(errorCode).toBe('EMAIL_NOT_VERIFIED');
  });

  it('portalRegister() should POST to /api/v1/portal/auth/register', () => {
    const payload: PortalRegisterPayload = {
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Secure123!',
      confirmPassword: 'Secure123!',
    };

    service.portalRegister(payload).subscribe(res => {
      expect(res.message).toBe('Check your email to activate your account');
    });

    const req = httpMock.expectOne('/api/v1/portal/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ message: 'Check your email to activate your account' });
  });

  it('resendVerificationEmail() should POST to /api/v1/portal/auth/resend-verification', () => {
    service.resendVerificationEmail('user@example.com').subscribe();
    const req = httpMock.expectOne('/api/v1/portal/auth/resend-verification');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush({ message: 'Sent' });
  });
});

describe('AuthService — password reset methods', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
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

  afterEach(() => httpMock.verify());

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
