import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FieldDefinition {
  id: string;
  label: string;
  labelAr?: string;
  type: 'text' | 'number' | 'date' | 'select' | 'checkbox';
  required: boolean;
  options?: string[];
}

@Injectable({ providedIn: 'root' })
export class FieldDefinitionService {
  private readonly http = inject(HttpClient);

  list(departmentId: string): Observable<FieldDefinition[]> {
    const params = new HttpParams().set('departmentId', departmentId);
    return this.http.get<FieldDefinition[]>('/api/v1/admin/field-definitions', { params });
  }
}
