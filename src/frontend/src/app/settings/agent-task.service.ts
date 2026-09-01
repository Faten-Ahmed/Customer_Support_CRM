import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AgentTaskDto {
  id: string;
  agentUserId: string;
  title: string;
  description?: string;
  dueDate?: string;
  isCompleted: boolean;
  completedAt?: string;
  createdAt: string;
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
  dueDate?: string;
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

  completeTask(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/complete`, {});
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
