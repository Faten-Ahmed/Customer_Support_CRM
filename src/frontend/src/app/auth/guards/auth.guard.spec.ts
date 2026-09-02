import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthStore } from '../auth.store';
import { vi } from 'vitest';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  function setup(isAuthenticated: boolean) {
    routerMock = { navigate: vi.fn().mockResolvedValue(true) };

    const authStoreMock = {
      isAuthenticated: vi.fn().mockReturnValue(isAuthenticated),
      user: vi.fn().mockReturnValue(isAuthenticated ? { role: 'Agent', passwordMustChange: false } : null),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: Router, useValue: routerMock },
        { provide: AuthStore, useValue: authStoreMock },
      ],
    });

    guard = TestBed.inject(AuthGuard);
  }

  it('should block unauthenticated users and redirect to /login', () => {
    setup(false);
    const result = guard.canActivate();
    expect(result).toBe(false);
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should allow authenticated users', () => {
    setup(true);
    expect(guard.canActivate()).toBe(true);
  });
});
