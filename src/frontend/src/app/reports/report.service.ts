import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReportFilter {
  dateFrom: string;
  dateTo: string;
  departmentId?: string;
  exportFormat?: 'csv' | 'excel' | 'pdf';
}

export interface TicketReport {
  summary: Record<string, number>;
  byStatus: { status: string; count: number }[];
  byPriority: { priority: string; count: number }[];
  trend: { date: string; count: number }[];
}

export interface SlaReport {
  complianceRate: number;
  byPriority: { priority: string; compliant: number; breached: number }[];
  breachReasons: { reason: string; count: number }[];
}

export interface AgentReportRow {
  agentId: string;
  agentName: string;
  ticketsHandled: number;
  avgResponseTime: number;
  slaComplianceRate: number;
  csatAvg?: number;
}

export interface CsatReport {
  avgRating: number;
  distribution: { rating: number; count: number }[];
  byDepartment: { department: string; avg: number }[];
  comments: { content: string; rating: number; agentName: string; date: string }[];
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);

  private buildParams(filter: ReportFilter): HttpParams {
    let params = new HttpParams().set('dateFrom', filter.dateFrom).set('dateTo', filter.dateTo);
    if (filter.departmentId) params = params.set('departmentId', filter.departmentId);
    return params;
  }

  getTicketReport(filter: ReportFilter): Observable<TicketReport> {
    return this.http.get<TicketReport>('/api/v1/reports/tickets', { params: this.buildParams(filter) });
  }

  getSlaReport(filter: ReportFilter): Observable<SlaReport> {
    return this.http.get<SlaReport>('/api/v1/reports/sla', { params: this.buildParams(filter) });
  }

  getAgentReport(filter: ReportFilter): Observable<AgentReportRow[]> {
    return this.http.get<AgentReportRow[]>('/api/v1/reports/agents', { params: this.buildParams(filter) });
  }

  getCsatReport(filter: ReportFilter): Observable<CsatReport> {
    return this.http.get<CsatReport>('/api/v1/reports/csat', { params: this.buildParams(filter) });
  }
}
