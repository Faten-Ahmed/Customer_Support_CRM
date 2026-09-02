import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AgentTaskDto {
  id: string;
  title: string;
  description?: string;
  priority: string;
  status: string;
  dueAt?: string;
  isOverdue: boolean;
  ticketId?: string;
  customerId?: string;
  createdAt: string;
  completedAt?: string;
}

export interface AgentTaskPage {
  items: AgentTaskDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateTaskPayload {
  title: string;
  description?: string;
  dueAt?: string;
}

@Injectable({ providedIn: 'root' })
export class AgentTaskService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/agents/me/tasks';

  listTasks(includeCompleted = false, page = 1, pageSize = 50): Observable<AgentTaskPage> {
    const params = new HttpParams()
      .set('includeCompleted', String(includeCompleted))
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    return this.http.get<AgentTaskPage>(this.baseUrl, { params });
  }

  createTask(payload: CreateTaskPayload): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, payload);
  }

  completeTask(id: string): Observable<AgentTaskDto> {
    return this.http.put<AgentTaskDto>(`${this.baseUrl}/${id}`, { status: 'Completed' });
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
