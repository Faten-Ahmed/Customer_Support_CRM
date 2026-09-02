import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthStore } from '../auth.store';
import { AuthService } from '../auth.service';
import { vi } from 'vitest';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authStoreMock: {
    getToken: ReturnType<typeof vi.fn>;
    setToken: ReturnType<typeof vi.fn>;
    clearToken: ReturnType<typeof vi.fn>;
  };
  let authServiceMock: { refresh: ReturnType<typeof vi.fn> };
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    authStoreMock = {
      getToken: vi.fn().mockReturnValue(null),
      setToken: vi.fn(),
      clearToken: vi.fn(),
    };
    authServiceMock = { refresh: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthStore, useValue: authStoreMock },
        { provide: AuthService, useValue: authServiceMock },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigate') as unknown as ReturnType<typeof vi.fn>;
    navigateSpy.mockResolvedValue(true);
  });

  afterEach(() => httpMock.verify());

  it('should attach Authorization header when token exists', () => {
    authStoreMock.getToken.mockReturnValue('fake-jwt');

    http.get('/api/v1/tickets').subscribe();

    const req = httpMock.expectOne('/api/v1/tickets');
    expect(req.request.headers.get('Authorization')).toBe('Bearer fake-jwt');
    req.flush([]);
  });

  it('should not attach Authorization header when no token', () => {
    authStoreMock.getToken.mockReturnValue(null);

    http.get('/api/v1/tickets').subscribe();

    const req = httpMock.expectOne('/api/v1/tickets');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('should attempt refresh on 401 and retry with new token', () => {
    authStoreMock.getToken.mockReturnValue('expired-jwt');
    authServiceMock.refresh.mockReturnValue(of({ accessToken: 'new-jwt' }));

    http.get('/api/v1/tickets').subscribe();

    const firstReq = httpMock.expectOne('/api/v1/tickets');
    firstReq.flush({}, { status: 401, statusText: 'Unauthorized' });

    const retryReq = httpMock.expectOne('/api/v1/tickets');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-jwt');
    retryReq.flush([]);
  });

  it('should call authStore.setToken with new token after successful refresh', () => {
    authStoreMock.getToken.mockReturnValue('expired-jwt');
    authServiceMock.refresh.mockReturnValue(of({ accessToken: 'new-jwt' }));

    http.get('/api/v1/tickets').subscribe();
    httpMock.expectOne('/api/v1/tickets').flush({}, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/v1/tickets').flush([]);

    expect(authStoreMock.setToken).toHaveBeenCalledWith('new-jwt');
  });

  it('should navigate to /change-password on 423', () => {
    authStoreMock.getToken.mockReturnValue('fake-jwt');

    http.get('/api/v1/tickets').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/v1/tickets');
    req.flush({ code: 'PASSWORD_CHANGE_REQUIRED' }, { status: 423, statusText: 'Locked' });

    expect(navigateSpy).toHaveBeenCalledWith(['/change-password']);
  });
});
