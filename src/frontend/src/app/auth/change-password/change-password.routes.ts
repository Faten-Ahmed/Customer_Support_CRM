import { Routes } from '@angular/router';

export const CHANGE_PASSWORD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./change-password.component').then(
        m => m.ChangePasswordComponent
      ),
  },
];
