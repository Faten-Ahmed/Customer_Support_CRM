import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthStore } from '../auth.store';
import { vi } from 'vitest';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.fn>;

  function setup(isAuthenticated: boolean) {
    const authStoreMock = {
      isAuthenticated: vi.fn().mockReturnValue(isAuthenticated),
      user: vi.fn().mockReturnValue(isAuthenticated ? { role: 'Agent', passwordMustChange: false } : null),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        AuthGuard,
        { provide: AuthStore, useValue: authStoreMock },
      ],
    });

    guard = TestBed.inject(AuthGuard);
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigate') as unknown as ReturnType<typeof vi.fn>;
    navigateSpy.mockResolvedValue(true);
  }

  it('should block unauthenticated users and redirect to /login', () => {
    setup(false);
    const result = guard.canActivate();
    expect(result).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('should allow authenticated users', () => {
    setup(true);
    expect(guard.canActivate()).toBe(true);
  });
});
