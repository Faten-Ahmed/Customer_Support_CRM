import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface QuickReplyTemplate {
  id: string;
  title: string;
  titleAr: string;
  content: string;
  contentAr: string;
  category?: string;
  scope: 'Global' | 'Personal';
  createdByUserId: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTemplatePayload {
  title: string;
  titleAr: string;
  content: string;
  contentAr: string;
  category?: string;
}

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private readonly http = inject(HttpClient);

  list(): Observable<{ data: QuickReplyTemplate[] }> {
    return this.http.get<{ data: QuickReplyTemplate[] }>('/api/v1/admin/templates');
  }

  create(payload: CreateTemplatePayload): Observable<any> {
    return this.http.post('/api/v1/admin/templates', payload);
  }

  update(id: string, payload: Partial<CreateTemplatePayload>): Observable<any> {
    return this.http.put(`/api/v1/admin/templates/${id}`, payload);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`/api/v1/admin/templates/${id}`);
  }
}
