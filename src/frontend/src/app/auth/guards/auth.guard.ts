import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../auth.store';

@Injectable({ providedIn: 'root' })
export class AuthGuard {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  canActivate(): boolean {
    if (!this.authStore.isAuthenticated()) {
      this.router.navigate(['/login']);
      return false;
    }
    return true;
  }
}
