import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type TicketStatus =
  | 'New' | 'Assigned' | 'InProgress' | 'OnHold'
  | 'Escalated' | 'Resolved' | 'Reopened' | 'Closed';

export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TicketChannel = 'Email' | 'Phone' | 'Chat' | 'Portal' | 'Internal';

export interface TicketSummary {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerName: string;
  subject: string;
  status: TicketStatus;
  priority: TicketPriority;
  channel: TicketChannel;
  assignedToUserId?: string;
  assignedToName?: string;
  createdAt: string;
  updatedAt: string;
}

export interface TicketListParams {
  search?: string;
  status?: TicketStatus[];
  priority?: TicketPriority;
  assignedToUserId?: string;
  categoryId?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDesc?: boolean;
}

export interface TicketPage {
  items: TicketSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateTicketPayload {
  customerId: string;
  departmentId: string;
  categoryId?: string;
  subject: string;
  description: string;
  priority: TicketPriority | string;
  channel?: TicketChannel;
  customFields?: { definitionId: string; value: string }[];
}

export interface SlaInfo {
  firstResponseDue?: string;
  resolutionDue?: string;
  firstResponseBreached: boolean;
  resolutionBreached: boolean;
  breachTier: string;
}

export interface TicketDetail extends TicketSummary {
  description: string;
  categoryName?: string;
  departmentName?: string;
  customFieldValues?: string;
  sla?: SlaInfo;
  resolvedAt?: string;
  closedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/tickets';

  list(params: TicketListParams): Observable<TicketPage> {
    let httpParams = new HttpParams();

    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.priority) httpParams = httpParams.set('priority', params.priority);
    if (params.assignedToUserId) httpParams = httpParams.set('assignedToUserId', params.assignedToUserId);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDesc != null) httpParams = httpParams.set('sortDesc', String(params.sortDesc));

    if (params.status?.length) {
      params.status.forEach(s => {
        httpParams = httpParams.append('status', s);
      });
    }

    return this.http.get<TicketPage>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.baseUrl}/${id}`);
  }

  create(payload: CreateTicketPayload): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(this.baseUrl, payload);
  }
}
