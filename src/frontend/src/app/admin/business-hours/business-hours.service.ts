import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Holiday {
  id: string;
  date: string;   // YYYY-MM-DD
  name: string;
}

export interface BusinessHoursCard {
  id: string;
  departmentId: string | null;
  workDays: string[];   // e.g. ['Sunday', 'Monday', 'Thursday']
  startTime: string;    // HH:mm
  endTime: string;      // HH:mm
  timeZone: string;
  holidays: Holiday[];
}

export interface UpdateBusinessHoursPayload {
  workDays: string[];
  startTime: string;
  endTime: string;
  timeZone: string;
}

@Injectable({ providedIn: 'root' })
export class BusinessHoursService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/admin/business-hours';

  list(): Observable<BusinessHoursCard[]> {
    return this.http.get<BusinessHoursCard[]>(this.base);
  }

  update(id: string, payload: UpdateBusinessHoursPayload): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, payload);
  }

  addHoliday(id: string, date: string, name: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/${id}/holidays`, { date, name });
  }

  deleteHoliday(id: string, holidayId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/holidays/${holidayId}`);
  }
}
