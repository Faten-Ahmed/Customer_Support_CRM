import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

const portalAuthGuard = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.user()?.role === 'Customer') return true;
  return router.createUrlTree(['/portal/login']);
};

export const PORTAL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./portal-shell/portal-shell.component').then(m => m.PortalShellComponent),
    canActivate: [portalAuthGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/portal-dashboard.component').then(m => m.PortalDashboardComponent),
      },
      {
        path: 'tickets/new',
        loadComponent: () =>
          import('./submit-ticket/portal-submit-ticket.component').then(m => m.PortalSubmitTicketComponent),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
];
