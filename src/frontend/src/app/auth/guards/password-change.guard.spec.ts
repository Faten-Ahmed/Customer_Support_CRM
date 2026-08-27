import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { PasswordChangeGuard } from './password-change.guard';
import { AuthService } from '../auth.service';
import { vi } from 'vitest';

describe('PasswordChangeGuard', () => {
  let guard: PasswordChangeGuard;
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.fn>;

  function setup(passwordMustChange: boolean) {
    const mockUser = passwordMustChange
      ? { id: '1', email: 'a@b.com', role: 'Agent' as const, passwordMustChange }
      : null;

    const authServiceStub = {
      currentUser: () => mockUser,
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        PasswordChangeGuard,
        { provide: AuthService, useValue: authServiceStub },
      ],
    });

    guard = TestBed.inject(PasswordChangeGuard);
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigate') as unknown as ReturnType<typeof vi.fn>;
    navigateSpy.mockResolvedValue(true);
  }

  it('should allow activation when passwordMustChange is false', () => {
    setup(false);
    expect(guard.canActivate()).toBe(true);
  });

  it('should allow activation when no user is logged in', () => {
    setup(false);
    expect(guard.canActivate()).toBe(true);
  });

  it('should redirect to /change-password when passwordMustChange is true', () => {
    setup(true);
    const result = guard.canActivate();
    expect(result).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/change-password']);
  });
});
