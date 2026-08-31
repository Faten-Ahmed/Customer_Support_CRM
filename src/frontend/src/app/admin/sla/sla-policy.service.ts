import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type SlaPriority = 'Critical' | 'High' | 'Medium' | 'Low';

export interface SlaPolicy {
  id: string;
  departmentId: string | null;
  priority: SlaPriority;
  firstResponseMinutes: number;
  resolutionMinutes: number;
  warningThresholdPercent: number;
  breachThresholdPercent: number;
  criticalBreachThresholdPercent: number;
}

export interface UpdateSlaPolicyPayload {
  firstResponseMinutes: number;
  resolutionMinutes: number;
  warningThresholdPercent: number;
  breachThresholdPercent: number;
  criticalBreachThresholdPercent: number;
}

@Injectable({ providedIn: 'root' })
export class SlaPolicyService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/admin/sla/policies';

  list(): Observable<SlaPolicy[]> {
    return this.http.get<SlaPolicy[]>(this.base);
  }

  update(id: string, payload: UpdateSlaPolicyPayload): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, payload);
  }
}
