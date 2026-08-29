import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Branch {
  id: string;
  name: string;
  nameAr?: string;
  isActive: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class BranchService {
  private readonly http = inject(HttpClient);

  list(): Observable<{ data: Branch[] }> {
    return this.http.get<{ data: Branch[] }>('/api/v1/admin/branches');
  }

  create(payload: { name: string; nameAr?: string }): Observable<any> {
    return this.http.post('/api/v1/admin/branches', payload);
  }

  update(id: string, payload: { name?: string; nameAr?: string }): Observable<any> {
    return this.http.put(`/api/v1/admin/branches/${id}`, payload);
  }

  deactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/branches/${id}/deactivate`, null);
  }

  reactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/branches/${id}/reactivate`, null);
  }
}
