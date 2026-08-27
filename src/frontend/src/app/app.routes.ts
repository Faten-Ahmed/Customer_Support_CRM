import { Routes } from '@angular/router';
import { AUTH_ROUTES } from './auth/auth.routes';
import { AuthGuard } from './auth/guards/auth.guard';
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
    path: 'app',
    loadComponent: () =>
      import('./shell/app-shell.component').then(m => m.AppShellComponent),
    canActivate: [AuthGuard],
    children: [],
  },

  { path: '403', component: ForbiddenComponent },
  { path: '**', component: NotFoundComponent },
];
