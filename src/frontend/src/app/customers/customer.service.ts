// src/app/customers/customer.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface CreateCustomerDto {
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
}

export interface UpdateCustomerDto {
  fullName?: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
}

export interface Customer {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
  createdAt?: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/customers';

  create(dto: CreateCustomerDto): Observable<Customer> {
    return this.http.post<Customer>(this.baseUrl, dto).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 409 && err.error?.code === 'EMAIL_ALREADY_EXISTS') {
          return throwError(() => ({ code: 'EMAIL_ALREADY_EXISTS', message: err.error.message }));
        }
        return throwError(() => err);
      })
    );
  }

  update(id: string, dto: UpdateCustomerDto): Observable<Customer> {
    return this.http.patch<Customer>(`${this.baseUrl}/${id}`, dto);
  }
}
