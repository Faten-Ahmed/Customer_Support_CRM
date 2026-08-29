import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Department {
  id: string;
  name: string;
  nameAr?: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly http = inject(HttpClient);

  list(): Observable<{ data: Department[] }> {
    return this.http.get<{ data: Department[] }>('/api/v1/admin/departments');
  }

  create(payload: { name: string; nameAr?: string; description?: string }): Observable<any> {
    return this.http.post('/api/v1/admin/departments', payload);
  }

  update(
    id: string,
    payload: { name?: string; nameAr?: string; description?: string }
  ): Observable<any> {
    return this.http.put(`/api/v1/admin/departments/${id}`, payload);
  }

  deactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/departments/${id}/deactivate`, null);
  }

  reactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/departments/${id}/reactivate`, null);
  }
}
