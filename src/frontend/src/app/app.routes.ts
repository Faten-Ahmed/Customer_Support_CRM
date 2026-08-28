import { Routes } from '@angular/router';
import { AUTH_ROUTES } from './auth/auth.routes';
import { AuthGuard } from './auth/guards/auth.guard';
import { CUSTOMERS_ROUTES } from './customers/customers.routes';
import { TICKETS_ROUTES } from './tickets/tickets.routes';
import { NotFoundComponent } from './shell/not-found.component';
import { ForbiddenComponent } from './shell/forbidden.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  ...AUTH_ROUTES,

  {
    path: 'portal/login',
    loadComponent: () =>
      import('./portal/auth/portal-auth-shell/portal-auth-shell.component').then(
        m => m.PortalAuthShellComponent
      ),
  },

  {
    path: 'portal/verify-email',
    loadComponent: () =>
      import('./portal/auth/verify-email/verify-email.component').then(
        m => m.VerifyEmailComponent
      ),
  },

  {
    path: 'app',
    loadComponent: () =>
      import('./shell/app-shell.component').then(m => m.AppShellComponent),
    canActivate: [AuthGuard],
    children: [
      { path: 'customers', children: CUSTOMERS_ROUTES },
      { path: 'tickets', children: TICKETS_ROUTES },
    ],
  },

  { path: '403', component: ForbiddenComponent },
  { path: '**', component: NotFoundComponent },
];
