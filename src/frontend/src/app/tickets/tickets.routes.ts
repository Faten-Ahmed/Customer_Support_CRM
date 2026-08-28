import { Routes } from '@angular/router';

export const TICKETS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./ticket-list/ticket-list.component').then(m => m.TicketListComponent),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./create-ticket/create-ticket-form.component').then(m => m.CreateTicketFormComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./ticket-detail/ticket-detail.component').then(m => m.TicketDetailComponent),
  },
];
