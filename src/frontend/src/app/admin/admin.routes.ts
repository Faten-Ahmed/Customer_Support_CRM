import { Routes } from '@angular/router';
import { AdminShellComponent } from './admin-shell/admin-shell.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      {
        path: 'users',
        loadComponent: () =>
          import('./user-management/user-list.component').then(m => m.UserListComponent),
      },
      {
        path: 'departments',
        loadComponent: () =>
          import('./departments/department-list.component').then(m => m.DepartmentListComponent),
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./categories/category-tree.component').then(m => m.CategoryTreeComponent),
      },
    ],
  },
];
