import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Template {
  id: string;
  title: string;
  content: string;
  category?: string;
  isGlobal: boolean;
}

export interface TemplatePage {
  items: Template[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private readonly http = inject(HttpClient);

  list(): Observable<TemplatePage> {
    return this.http.get<TemplatePage>('/api/v1/templates');
  }

  render(templateId: string, ticketId: string): Observable<{ content: string }> {
    return this.http.post<{ content: string }>(`/api/v1/templates/${templateId}/render`, { ticketId });
  }
}
