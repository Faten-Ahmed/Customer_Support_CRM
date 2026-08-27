import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { LoginComponent } from './login/login.component';
import { AuthService } from './auth.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { vi } from 'vitest';

describe('Auth routing', () => {
  it('should load LoginComponent at /login', async () => {
    const mockAuthService = {
      isAuthenticated: vi.fn().mockReturnValue(false),
      login: vi.fn(),
      logout: vi.fn(),
      refreshToken: vi.fn(),
      accessToken: vi.fn().mockReturnValue(null),
      currentUser: vi.fn().mockReturnValue(null),
    };

    await TestBed.configureTestingModule({
      imports: [NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService },
        provideRouter([{ path: 'login', component: LoginComponent }]),
      ],
    }).compileComponents();

    const harness = await RouterTestingHarness.create('/login');
    expect(harness.routeNativeElement).toBeTruthy();
  });

  it('should have auth routes configuration exported', async () => {
    const { AUTH_ROUTES } = await import('./auth.routes');
    expect(AUTH_ROUTES).toBeDefined();
    expect(Array.isArray(AUTH_ROUTES)).toBe(true);
  });

  it('should have login route in AUTH_ROUTES', async () => {
    const { AUTH_ROUTES } = await import('./auth.routes');
    const loginRoute = AUTH_ROUTES.find((r: any) => r.path === 'login');
    expect(loginRoute).toBeDefined();
  });

  it('should have forgot-password route in AUTH_ROUTES', async () => {
    const { AUTH_ROUTES } = await import('./auth.routes');
    const route = AUTH_ROUTES.find((r: any) => r.path === 'forgot-password');
    expect(route).toBeDefined();
  });

  it('should have reset-password route in AUTH_ROUTES', async () => {
    const { AUTH_ROUTES } = await import('./auth.routes');
    const route = AUTH_ROUTES.find((r: any) => r.path === 'reset-password');
    expect(route).toBeDefined();
  });

  it('should have change-password route in AUTH_ROUTES', async () => {
    const { AUTH_ROUTES } = await import('./auth.routes');
    const route = AUTH_ROUTES.find((r: any) => r.path === 'change-password');
    expect(route).toBeDefined();
  });
});
