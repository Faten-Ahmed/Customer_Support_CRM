import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PortalProfile {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  companyName?: string;
  country?: string;
  city?: string;
}

@Injectable({ providedIn: 'root' })
export class PortalProfileService {
  private readonly http = inject(HttpClient);

  get(): Observable<{ data: PortalProfile }> {
    return this.http.get<{ data: PortalProfile }>('/api/v1/portal/profile');
  }

  update(payload: Partial<Pick<PortalProfile, 'fullName' | 'phone' | 'city'>>): Observable<{ data: PortalProfile }> {
    return this.http.patch<{ data: PortalProfile }>('/api/v1/portal/profile', payload);
  }
}
