import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type AvailabilityStatus = 'Available' | 'Busy' | 'Away' | 'Offline';
export type SlaStatus = 'ok' | 'warning' | 'breach' | 'criticalBreach';

export interface MyTicketDto {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerFullName: string;
  subject: string;
  status: string;
  priority: string;
  channel: string;
  departmentId?: string;
  categoryId?: string;
  createdAt: string;
  resolutionDue?: string;
  slaStatus: SlaStatus;
  resolutionRemainingMinutes?: number;
}

export interface MyTicketsPage {
  items: MyTicketDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface MyTicketsParams {
  status?: string;
  priority?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
}

export interface AvailabilityResponse {
  status: AvailabilityStatus;
  changedAt: string;
}

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/agents/me';

  getMyTickets(params: MyTicketsParams = {}): Observable<MyTicketsPage> {
    let httpParams = new HttpParams();
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.priority) httpParams = httpParams.set('priority', params.priority);
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);

    return this.http.get<MyTicketsPage>(`${this.baseUrl}/tickets`, { params: httpParams });
  }

  updateAvailability(status: AvailabilityStatus): Observable<AvailabilityResponse> {
    return this.http.put<AvailabilityResponse>(`${this.baseUrl}/availability`, { status });
  }
}
