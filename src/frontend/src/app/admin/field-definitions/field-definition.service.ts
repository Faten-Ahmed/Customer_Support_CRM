import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type FieldType = 'Text' | 'Number' | 'Date' | 'Dropdown' | 'Checkbox';

export interface FieldDefinition {
  id: string;
  departmentId: string;
  categoryId?: string;
  fieldName: string;
  fieldNameAr?: string;
  fieldType: FieldType;
  options?: string[];
  isRequired: boolean;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateFieldDefinitionPayload {
  departmentId: string;
  categoryId?: string;
  fieldName: string;
  fieldNameAr?: string;
  fieldType: FieldType;
  options?: string[];
  isRequired: boolean;
  sortOrder: number;
}

@Injectable({ providedIn: 'root' })
export class FieldDefinitionService {
  private readonly http = inject(HttpClient);

  list(departmentId?: string, categoryId?: string): Observable<{ data: FieldDefinition[] }> {
    const params: Record<string, string> = {};
    if (departmentId) params['departmentId'] = departmentId;
    if (categoryId) params['categoryId'] = categoryId;
    return this.http.get<{ data: FieldDefinition[] }>('/api/v1/admin/field-definitions', { params });
  }

  create(payload: CreateFieldDefinitionPayload): Observable<any> {
    return this.http.post('/api/v1/admin/field-definitions', payload);
  }

  update(id: string, payload: Partial<CreateFieldDefinitionPayload>): Observable<any> {
    return this.http.put(`/api/v1/admin/field-definitions/${id}`, payload);
  }

  deactivate(id: string): Observable<any> {
    return this.http.delete(`/api/v1/admin/field-definitions/${id}`);
  }
}
