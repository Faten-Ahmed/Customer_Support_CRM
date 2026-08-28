import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface Contact {
  id: string;
  type: 'Phone' | 'Email' | 'WhatsApp';
  value: string;
  isPrimary: boolean;
}

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

export interface CustomerDetail extends Customer {
  contacts: Contact[];
}

export interface CustomerTicket {
  ticketNumber: string;
  subject: string;
  status: string;
  priority: string;
  createdAt: string;
  category?: string;
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

export interface AddContactDto {
  type: string;
  value: string;
  isPrimary: boolean;
}

export interface CustomerListQuery {
  page: number;
  pageSize: number;
  search?: string;
  isVip?: boolean;
  isActive?: boolean;
}

export interface PageMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CustomerPage {
  items: Customer[];
  meta: PageMeta;
}

export interface CustomerTicketPage {
  items: CustomerTicket[];
  meta: PageMeta;
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

  getById(id: string): Observable<CustomerDetail> {
    return this.http.get<CustomerDetail>(`${this.baseUrl}/${id}`);
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

  update(id: string, dto: UpdateCustomerDto): Observable<CustomerDetail> {
    return this.http.put<CustomerDetail>(`${this.baseUrl}/${id}`, dto);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  reactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/reactivate`, {});
  }

  setVip(id: string, isVip: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/vip`, { isVip });
  }

  addContact(id: string, dto: AddContactDto): Observable<Contact> {
    return this.http.post<Contact>(`${this.baseUrl}/${id}/contacts`, dto);
  }

  removeContact(id: string, contactId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/contacts/${contactId}`);
  }

  getTickets(id: string, page = 1, pageSize = 10, status?: string): Observable<CustomerTicketPage> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    if (status) params = params.set('status', status);
    return this.http.get<CustomerTicketPage>(`${this.baseUrl}/${id}/tickets`, { params });
  }
}
