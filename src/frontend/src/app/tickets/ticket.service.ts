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

export interface Attachment {
  id: string;
  ticketId: string;
  messageId?: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  presignedUrl?: string;
  uploaderName?: string;
  uploadedAt: string;
}

export interface TicketMessage {
  id: string;
  ticketId: string;
  body: string;
  isInternal: boolean;
  authorUserId?: string;
  authorName?: string;
  authorCustomerId?: string;
  createdAt: string;
}

export interface TicketMessagePage {
  items: TicketMessage[];
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
  subjectAr: string;
  description: string;
  descriptionAr: string;
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
  subjectAr?: string;
  description: string;
  descriptionAr?: string;
  departmentId?: string;
  departmentName?: string;
  categoryId?: string;
  categoryName?: string;
  customFieldValues?: string;
  sla?: SlaInfo;
  resolvedAt?: string;
  closedAt?: string;
}

export interface TicketHistoryEntry {
  fieldChanged: string;
  oldValue?: string | null;
  newValue?: string | null;
  changedByName: string;
  changedAt: string;
}

export interface TicketHistoryPage {
  items: TicketHistoryEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SlaClock {
  dueAt: string;
  elapsedPercent: number;
  breached: boolean;
  remainingLabel: string;
}

export interface SlaStatus {
  isPaused: boolean;
  firstResponse: SlaClock;
  resolution: SlaClock;
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

  update(ticketId: string, payload: { subject: string; subjectAr: string; description: string; descriptionAr: string; priority: string; categoryId?: string; departmentId?: string; customFieldValues?: string }): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${this.baseUrl}/${ticketId}`, payload);
  }

  assign(ticketId: string, agentId: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${ticketId}/assign`, { agentId });
  }

  getAgents(): Observable<{ id: string; name: string; role: string }[]> {
    return this.http.get<{ id: string; name: string; role: string }[]>('/api/v1/users/agents');
  }

  transfer(ticketId: string, departmentId: string, transferNote: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${ticketId}/transfer`, { departmentId, transferNote });
  }

  escalate(ticketId: string, reason: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${ticketId}/escalate`, { reason });
  }

  changeStatus(ticketId: string, status: string, resolutionText: string | undefined): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${ticketId}/status`, { status, resolutionText });
  }

  getAttachments(ticketId: string): Observable<Attachment[]> {
    return this.http.get<Attachment[]>(`${this.baseUrl}/${ticketId}/attachments`);
  }

  uploadAttachment(ticketId: string, file: File): Observable<Attachment> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<Attachment>(`${this.baseUrl}/${ticketId}/attachments`, form);
  }

  deleteAttachment(ticketId: string, attachmentId: string): Observable<null> {
    return this.http.delete<null>(`${this.baseUrl}/${ticketId}/attachments/${attachmentId}`);
  }

  addMessage(ticketId: string, body: string, isInternal: boolean): Observable<TicketMessage> {
    return this.http.post<TicketMessage>(`${this.baseUrl}/${ticketId}/messages`, { body, isInternal });
  }

  getMessages(ticketId: string, page: number, pageSize: number): Observable<TicketMessagePage> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.http.get<TicketMessagePage>(`${this.baseUrl}/${ticketId}/messages`, { params });
  }

  getHistory(ticketId: string, page = 1, pageSize = 20): Observable<TicketHistoryPage> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.http.get<TicketHistoryPage>(`${this.baseUrl}/${ticketId}/history`, { params });
  }

  getSla(ticketId: string): Observable<SlaStatus> {
    return this.http.get<SlaStatus>(`${this.baseUrl}/${ticketId}/sla`);
  }
}
