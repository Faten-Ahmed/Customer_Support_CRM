import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Category {
  id: string;
  name: string;
  nameAr?: string;
  parentCategoryId?: string;
  sortOrder: number;
  isActive: boolean;
  children?: Category[];
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);

  list(): Observable<{ data: Category[] }> {
    return this.http.get<{ data: Category[] }>('/api/v1/admin/categories');
  }

  create(payload: {
    name: string;
    nameAr?: string;
    parentId?: string;
    sortOrder: number;
  }): Observable<any> {
    return this.http.post('/api/v1/admin/categories', payload);
  }

  update(
    id: string,
    payload: { name?: string; nameAr?: string; sortOrder?: number }
  ): Observable<any> {
    return this.http.put(`/api/v1/admin/categories/${id}`, payload);
  }

  deactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/categories/${id}/deactivate`, null);
  }

  reactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/categories/${id}/reactivate`, null);
  }
}
