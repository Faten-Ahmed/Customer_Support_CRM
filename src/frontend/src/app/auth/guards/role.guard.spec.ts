import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { RoleGuard } from './role.guard';
import { AuthStore } from '../auth.store';
import { vi } from 'vitest';

describe('RoleGuard', () => {
  let guard: RoleGuard;
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.fn>;
  let authStoreMock: { user: ReturnType<typeof vi.fn> };

  const makeRoute = (roles: string[]): ActivatedRouteSnapshot => {
    const snap = new ActivatedRouteSnapshot();
    (snap as unknown as { data: Record<string, unknown> }).data = { roles };
    return snap;
  };

  beforeEach(() => {
    authStoreMock = { user: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        RoleGuard,
        { provide: AuthStore, useValue: authStoreMock },
      ],
    });

    guard = TestBed.inject(RoleGuard);
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigate') as unknown as ReturnType<typeof vi.fn>;
    navigateSpy.mockResolvedValue(true);
  });

  it('should allow when user role is in the route roles list', () => {
    authStoreMock.user.mockReturnValue({ role: 'Admin' });
    expect(guard.canActivate(makeRoute(['Admin', 'Manager']))).toBe(true);
  });

  it('should block and navigate to /403 when role not permitted', () => {
    authStoreMock.user.mockReturnValue({ role: 'Agent' });
    const result = guard.canActivate(makeRoute(['Admin']));
    expect(result).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/403']);
  });

  it('should block when no user', () => {
    authStoreMock.user.mockReturnValue(null);
    expect(guard.canActivate(makeRoute(['Admin']))).toBe(false);
  });
});
