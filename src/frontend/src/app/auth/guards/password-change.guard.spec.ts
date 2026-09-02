import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { PasswordChangeGuard } from './password-change.guard';
import { AuthService } from '../auth.service';
import { vi } from 'vitest';

describe('PasswordChangeGuard', () => {
  let guard: PasswordChangeGuard;
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  function setup(requiresPasswordChange: boolean) {
    routerMock = { navigate: vi.fn().mockResolvedValue(true) };

    const mockUser = requiresPasswordChange
      ? { id: '1', email: 'a@b.com', role: 'Agent' as const, requiresPasswordChange }
      : null;

    const authServiceStub = {
      currentUser: () => mockUser,
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        PasswordChangeGuard,
        { provide: Router, useValue: routerMock },
        { provide: AuthService, useValue: authServiceStub },
      ],
    });

    guard = TestBed.inject(PasswordChangeGuard);
  }

  it('should allow activation when requiresPasswordChange is false', () => {
    setup(false);
    expect(guard.canActivate()).toBe(true);
  });

  it('should allow activation when no user is logged in', () => {
    setup(false);
    expect(guard.canActivate()).toBe(true);
  });

  it('should redirect to /change-password when requiresPasswordChange is true', () => {
    setup(true);
    const result = guard.canActivate();
    expect(result).toBe(false);
    expect(routerMock.navigate).toHaveBeenCalledWith(['/change-password']);
  });
});
