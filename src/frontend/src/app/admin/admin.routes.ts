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
        path: 'branches',
        loadComponent: () =>
          import('./branches/branch-list.component').then(m => m.BranchListComponent),
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./categories/category-tree.component').then(m => m.CategoryTreeComponent),
      },
      {
        path: 'field-definitions',
        loadComponent: () =>
          import('./field-definitions/field-definition-list.component').then(m => m.FieldDefinitionListComponent),
      },
      {
        path: 'templates',
        loadComponent: () =>
          import('./templates/template-list.component').then(m => m.TemplateListComponent),
      },
      {
        path: 'channels',
        loadComponent: () =>
          import('./channels/channel-status.component').then(m => m.ChannelStatusComponent),
      },
      {
        path: 'sla-policies',
        loadComponent: () =>
          import('./sla/sla-policy-table.component').then(m => m.SlaPolicyTableComponent),
      },
      {
        path: 'business-hours',
        loadComponent: () =>
          import('./business-hours/business-hours-editor.component').then(m => m.BusinessHoursEditorComponent),
      },
    ],
  },
];
