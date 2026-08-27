import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'forgot-password',
    loadChildren: () =>
      import('./forgot-password/forgot-password.routes').then(
        m => m.FORGOT_PASSWORD_ROUTES
      ),
  },
  {
    path: 'reset-password',
    loadChildren: () =>
      import('./reset-password/reset-password.routes').then(
        m => m.RESET_PASSWORD_ROUTES
      ),
  },
  {
    path: 'change-password',
    loadChildren: () =>
      import('./change-password/change-password.routes').then(
        m => m.CHANGE_PASSWORD_ROUTES
      ),
  },
];
