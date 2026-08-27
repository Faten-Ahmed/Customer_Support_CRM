import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface Customer {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  isVip: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCustomerDto {
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
}

export interface UpdateCustomerDto {
  fullName?: string;
  phone?: string;
  companyName?: string;
}

export interface CustomerListQuery {
  page: number;
  pageSize: number;
  search?: string;
  isVip?: boolean;
  isActive?: boolean;
}

export interface CustomerMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CustomerPage {
  items: Customer[];
  meta: CustomerMeta;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/customers';

  list(query: CustomerListQuery): Observable<CustomerPage> {
    let params = new HttpParams()
      .set('page', String(query.page))
      .set('pageSize', String(query.pageSize));
    if (query.search) params = params.set('search', query.search);
    if (query.isVip !== undefined) params = params.set('isVip', String(query.isVip));
    if (query.isActive !== undefined) params = params.set('isActive', String(query.isActive));
    return this.http.get<CustomerPage>(this.baseUrl, { params });
  }

  getById(id: string): Observable<Customer> {
    return this.http.get<Customer>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateCustomerDto): Observable<Customer> {
    return this.http.post<Customer>(this.baseUrl, dto).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 409) {
          return throwError(() => ({ code: 'EMAIL_ALREADY_EXISTS', message: err.error?.errors?.[0]?.message ?? 'Email already exists' }));
        }
        return throwError(() => err);
      })
    );
  }

  update(id: string, dto: UpdateCustomerDto): Observable<Customer> {
    return this.http.put<Customer>(`${this.baseUrl}/${id}`, dto);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
