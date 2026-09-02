import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface KpiData {
  openTickets: number;
  openByPriority?: Record<string, number>;
  slaBreachRate: number;
  avgFirstResponseMinutes7Day: number;
  avgResolutionMinutes7Day?: number;
  csatScore30Day?: number;
  agentUtilization?: number;
  ticketsTodayCreated?: number;
  ticketsTodayResolved?: number;
  escalationRate?: number;
  unassignedTickets?: number;
  agentWorkload?: AgentWorkload[];
  calculatedAt?: string;
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
    return this.http
      .get<{ data: KpiData }>('/api/v1/dashboard/kpis', { params })
      .pipe(map(r => r.data));
  }
}
