import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface KpiData {
  openTickets: number;
  slaBreachRate: number;
  avgFirstResponse: number;
  avgResolution?: number;
  csatScore?: number;
  agentUtilization?: number;
  unassignedTickets?: number;
  escalationRate?: number;
  createdToday?: number;
  resolvedToday?: number;
}

export interface AgentWorkload {
  agentId: string;
  agentName: string;
  openTickets: number;
  availabilityStatus: string;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getKpis(departmentId?: string): Observable<KpiData> {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<KpiData>('/api/v1/reports/dashboard', { params });
  }

  getAgentWorkload(departmentId?: string): Observable<AgentWorkload[]> {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<AgentWorkload[]>('/api/v1/reports/dashboard/agents', { params });
  }
}
