import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Template {
  id: string;
  title: string;
  titleAr?: string;
  content: string;
  contentAr?: string;
  category?: string;
  scope: string;
  isActive: boolean;
}

export interface TemplatePage {
  data: Template[];
  meta: { totalCount: number };
}

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private readonly http = inject(HttpClient);

  list(): Observable<TemplatePage> {
    return this.http.get<TemplatePage>('/api/v1/templates');
  }

  render(templateId: string, ticketId: string): Observable<{ content: string }> {
    return this.http.post<{ content: string }>(`/api/v1/admin/templates/${templateId}/render`, { ticketId });
  }
}
