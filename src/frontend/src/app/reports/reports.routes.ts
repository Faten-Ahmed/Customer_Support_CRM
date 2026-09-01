import { Routes } from '@angular/router';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./management-dashboard/management-dashboard.component').then(
        m => m.ManagementDashboardComponent
      ),
  },
  {
    path: 'tickets',
    loadComponent: () =>
      import('./ticket-report/ticket-report.component').then(
        m => m.TicketReportComponent
      ),
  },
  {
    path: 'sla',
    loadComponent: () =>
      import('./sla-report/sla-report.component').then(
        m => m.SlaReportComponent
      ),
  },
  {
    path: 'agents',
    loadComponent: () =>
      import('./agent-report/agent-report.component').then(
        m => m.AgentReportComponent
      ),
  },
  {
    path: 'csat',
    loadComponent: () =>
      import('./csat-report/csat-report.component').then(
        m => m.CsatReportComponent
      ),
  },
];
