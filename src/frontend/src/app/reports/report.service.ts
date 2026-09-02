import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ReportFilter {
  dateFrom: string;
  dateTo: string;
  departmentId?: string;
  groupBy?: string;
}

export interface TicketSummary {
  totalCreated: number;
  totalResolved: number;
  totalClosed: number;
  openAtEndOfPeriod: number;
}

export interface TrendPoint {
  date: string;
  created: number;
  resolved: number;
}

export interface TicketVolumeReport {
  summary: TicketSummary;
  byStatus: Record<string, number>;
  byPriority: Record<string, number>;
  byChannel: Record<string, number>;
  trend: TrendPoint[];
}

export interface SlaComplianceByPriority {
  firstResponseComplianceRate: number;
  resolutionComplianceRate: number;
  totalTickets: number;
}

export interface SlaBreachReasons {
  warningCount: number;
  breachCount: number;
  criticalBreachCount: number;
}

export interface SlaComplianceReport {
  firstResponseComplianceRate: number;
  resolutionComplianceRate: number;
  avgFirstResponseMinutes: number;
  avgResolutionMinutes: number;
  byPriority: Record<string, SlaComplianceByPriority>;
  breachReasons: SlaBreachReasons;
}

export interface AgentPerformanceRow {
  agentId: string;
  agentName: string;
  ticketsHandled: number;
  ticketsResolved: number;
  avgFirstResponseMinutes: number;
  avgResolutionMinutes: number;
  slaComplianceRate: number;
  csatScore?: number;
  csatResponseCount: number;
  escalationRate: number;
}

export interface CsatReport {
  overall: { avgRating?: number; totalSent: number; totalSubmitted: number; responseRate: number };
  distribution: Record<number, number>;
  byDepartment: { departmentId: string; departmentName: string; avgRating?: number; totalSubmitted: number }[];
  byAgent: { agentId: string; agentName: string; avgRating?: number; totalSubmitted: number }[];
  recentComments: string[];
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);

  private buildParams(filter: ReportFilter): HttpParams {
    let params = new HttpParams()
      .set('dateFrom', filter.dateFrom)
      .set('dateTo', filter.dateTo);
    if (filter.departmentId) params = params.set('departmentId', filter.departmentId);
    if (filter.groupBy) params = params.set('groupBy', filter.groupBy);
    return params;
  }

  getTicketReport(filter: ReportFilter): Observable<TicketVolumeReport> {
    return this.http
      .get<{ data: TicketVolumeReport }>('/api/v1/reports/tickets', { params: this.buildParams(filter) })
      .pipe(map(r => r.data));
  }

  getSlaReport(filter: ReportFilter): Observable<SlaComplianceReport> {
    return this.http
      .get<{ data: SlaComplianceReport }>('/api/v1/reports/sla', { params: this.buildParams(filter) })
      .pipe(map(r => r.data));
  }

  getAgentReport(filter: ReportFilter): Observable<AgentPerformanceRow[]> {
    return this.http
      .get<{ data: AgentPerformanceRow[] }>('/api/v1/reports/agents', { params: this.buildParams(filter) })
      .pipe(map(r => r.data));
  }

  getCsatReport(filter: ReportFilter): Observable<CsatReport> {
    return this.http
      .get<{ data: CsatReport }>('/api/v1/reports/csat', { params: this.buildParams(filter) })
      .pipe(map(r => r.data));
  }
}
