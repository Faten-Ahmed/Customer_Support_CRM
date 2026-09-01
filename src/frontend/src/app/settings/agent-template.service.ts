import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TemplateDto {
  id: string;
  title: string;
  titleAr: string;
  content: string;
  contentAr: string;
  category?: string;
  scope: 'Personal' | 'Global';
  isActive: boolean;
  createdByUserId: string;
  createdAt: string;
}

export interface TemplateDtoPage {
  items: TemplateDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateTemplatePayload {
  title: string;
  titleAr: string;
  content: string;
  contentAr: string;
  category?: string;
}

export interface UpdateTemplatePayload {
  title?: string;
  titleAr?: string;
  content?: string;
  contentAr?: string;
  category?: string;
}

@Injectable({ providedIn: 'root' })
export class AgentTemplateService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/agents/me/templates';

  listMyTemplates(search?: string, page = 1, pageSize = 20): Observable<TemplateDtoPage> {
    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    if (search) params = params.set('search', search);
    return this.http.get<TemplateDtoPage>(this.baseUrl, { params });
  }

  createTemplate(payload: CreateTemplatePayload): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, payload);
  }

  updateTemplate(id: string, payload: UpdateTemplatePayload): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}`, payload);
  }

  deleteTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  renderTemplate(id: string, ticketId: string): Observable<{ rendered: string }> {
    return this.http.post<{ rendered: string }>(`${this.baseUrl}/${id}/render`, { ticketId });
  }
}
