import { Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthStore } from '../auth.store';

@Injectable({ providedIn: 'root' })
export class RoleGuard {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const user = this.authStore.user();
    const allowedRoles: string[] = route.data['roles'] ?? [];
    if (!user || !allowedRoles.includes(user.role)) {
      this.router.navigate(['/403']);
      return false;
    }
    return true;
  }
}
