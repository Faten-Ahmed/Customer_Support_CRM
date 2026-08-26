import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService, LoginResponse } from './auth.service';
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
