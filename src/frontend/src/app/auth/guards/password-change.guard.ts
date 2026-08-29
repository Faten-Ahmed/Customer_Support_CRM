import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Injectable({ providedIn: 'root' })
export class PasswordChangeGuard {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  canActivate(): boolean {
    if (this.authService.currentUser()?.requiresPasswordChange) {
      this.router.navigate(['/change-password']);
      return false;
    }
    return true;
  }
}
