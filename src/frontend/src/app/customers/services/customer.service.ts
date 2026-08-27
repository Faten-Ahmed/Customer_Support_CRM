import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Customer {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  isVip: boolean;
  isActive: boolean;
  ticketCount: number;
  createdAt: string;
}

export interface CustomerListQuery {
  page: number;
  pageSize: number;
  search?: string;
  vipOnly?: boolean;
  activeOnly?: boolean;
}

export interface CustomerPage {
  data: Customer[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);

  list(query: CustomerListQuery): Observable<CustomerPage> {
    let params = new HttpParams()
      .set('page', String(query.page))
      .set('pageSize', String(query.pageSize));
    if (query.search) params = params.set('search', query.search);
    if (query.vipOnly !== undefined) params = params.set('vipOnly', String(query.vipOnly));
    if (query.activeOnly !== undefined) params = params.set('activeOnly', String(query.activeOnly));
    return this.http.get<CustomerPage>('/api/v1/customers', { params });
  }

  getById(id: string): Observable<Customer> {
    return this.http.get<Customer>(`/api/v1/customers/${id}`);
  }

  update(id: string, changes: Partial<Customer>): Observable<Customer> {
    return this.http.patch<Customer>(`/api/v1/customers/${id}`, changes);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/customers/${id}`);
  }
}
