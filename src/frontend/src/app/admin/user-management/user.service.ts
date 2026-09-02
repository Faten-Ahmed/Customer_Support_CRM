import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserSummary {
  id: string;
  firstName: string;
  lastName: string;
  firstNameAr?: string;
  lastNameAr?: string;
  email: string;
  role: string;
  isActive: boolean;
  availabilityStatus: string;
  createdAt: string;
  primaryDepartmentId?: string;
  primaryDepartmentName?: string;
}

export interface UserDetail extends UserSummary {
  jobTitle?: string;
  jobTitleAr?: string;
  passwordMustChange: boolean;
  departments: { departmentId: string; departmentName: string; isPrimary: boolean }[];
  skills: { categoryId: string; categoryName: string }[];
}

export interface CreateUserPayload {
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  tempPassword: string;
  primaryDepartmentId?: string;
  firstNameAr?: string;
  lastNameAr?: string;
  jobTitle?: string;
  jobTitleAr?: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  list(
    filters: {
      page?: number;
      pageSize?: number;
      role?: string;
      isActive?: boolean;
      search?: string;
    } = {}
  ): Observable<any> {
    let params = new HttpParams();
    if (filters.page) params = params.set('page', filters.page);
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize);
    if (filters.role) params = params.set('role', filters.role);
    if (filters.isActive !== undefined) params = params.set('isActive', filters.isActive);
    if (filters.search) params = params.set('search', filters.search);
    return this.http.get('/api/v1/admin/users', { params });
  }

  getById(id: string): Observable<{ data: UserDetail }> {
    return this.http.get<{ data: UserDetail }>(`/api/v1/admin/users/${id}`);
  }

  create(payload: CreateUserPayload): Observable<any> {
    return this.http.post('/api/v1/admin/users', {
      firstName: payload.firstName,
      lastName: payload.lastName,
      email: payload.email,
      role: payload.role,
      password: payload.tempPassword,
      primaryDepartmentId: payload.primaryDepartmentId,
      firstNameAr: payload.firstNameAr,
      lastNameAr: payload.lastNameAr,
      jobTitle: payload.jobTitle,
      jobTitleAr: payload.jobTitleAr,
    });
  }

  update(
    id: string,
    payload: Partial<{
      firstName: string;
      lastName: string;
      firstNameAr: string;
      lastNameAr: string;
      jobTitle: string;
      jobTitleAr: string;
    }>
  ): Observable<any> {
    return this.http.put(`/api/v1/admin/users/${id}`, payload);
  }

  deactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/users/${id}/deactivate`, null);
  }

  reactivate(id: string): Observable<any> {
    return this.http.post(`/api/v1/admin/users/${id}/reactivate`, null);
  }

  updateDepartments(
    id: string,
    departments: { departmentId: string; isPrimary: boolean }[]
  ): Observable<any> {
    return this.http.put(`/api/v1/admin/users/${id}/departments`, { departments });
  }

  updateSkills(id: string, categoryIds: string[]): Observable<any> {
    return this.http.put(`/api/v1/admin/users/${id}/skills`, { categoryIds });
  }
}
