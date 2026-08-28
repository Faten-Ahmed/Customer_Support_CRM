import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PortalTicketDetail {
  id: string;
  ticketNumber: string;
  subject: string;
  description: string;
  status: string;
  priority: string;
  channel: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
  closedAt: string | null;
  assignedAgentName: string | null;
}

export interface PortalTicketMessage {
  id: string;
  ticketId: string;
  body: string;
  isInternal: boolean;
  authorUserId: string | null;
  authorName: string | null;
  authorCustomerId: string | null;
  createdAt: string;
}

export interface PortalTicketPage {
  items: Array<{
    id: string;
    ticketNumber: string;
    subject: string;
    status: string;
    priority: string;
    createdAt: string;
    category: string | null;
  }>;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PortalMessagePage {
  items: PortalTicketMessage[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PortalAttachment {
  id: string;
  ticketId: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploaderName: string | null;
  uploadedAt: string;
  presignedUrl: string | null;
}

@Injectable({ providedIn: 'root' })
export class PortalTicketService {
  private readonly http = inject(HttpClient);

  list(status?: string): Observable<PortalTicketPage> {
    const url = status
      ? `/api/v1/portal/tickets?status=${encodeURIComponent(status)}`
      : '/api/v1/portal/tickets';
    return this.http.get<PortalTicketPage>(url);
  }

  getById(id: string): Observable<PortalTicketDetail> {
    return this.http.get<PortalTicketDetail>(`/api/v1/portal/tickets/${id}`);
  }

  getMessages(id: string): Observable<PortalMessagePage> {
    return this.http.get<PortalMessagePage>(`/api/v1/portal/tickets/${id}/messages`);
  }

  addMessage(id: string, body: string): Observable<PortalTicketMessage> {
    return this.http.post<PortalTicketMessage>(
      `/api/v1/portal/tickets/${id}/messages`, { body });
  }

  getAttachments(id: string): Observable<PortalAttachment[]> {
    return this.http.get<PortalAttachment[]>(`/api/v1/portal/tickets/${id}/attachments`);
  }

  uploadAttachment(id: string, file: File): Observable<PortalAttachment> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<PortalAttachment>(`/api/v1/portal/tickets/${id}/attachments`, form);
  }

  close(id: string): Observable<{ id: string; status: string; surveyUrl?: string }> {
    return this.http.post<{ id: string; status: string; surveyUrl?: string }>(
      `/api/v1/portal/tickets/${id}/close`, {});
  }
}
