import { Routes } from '@angular/router';

export const CUSTOMERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./customer-list/customer-list.component').then(m => m.CustomerListComponent),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./create-customer-form/create-customer-form.component').then(m => m.CreateCustomerFormComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./customer-detail/customer-detail.component').then(m => m.CustomerDetailComponent),
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./edit-customer-form/edit-customer-form.component').then(m => m.EditCustomerFormComponent),
  },
];
