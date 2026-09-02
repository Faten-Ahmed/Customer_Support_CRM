import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface SurveyDetail {
  id: string;
  ticketNumber: string;
  ticketSubject: string;
}

export interface SurveySubmitResponse {
  success: boolean;
}

@Injectable({ providedIn: 'root' })
export class PortalSurveyService {
  private readonly http = inject(HttpClient);

  get(id: string): Observable<SurveyDetail> {
    return this.http.get<{ data: SurveyDetail }>(`/api/v1/portal/surveys/${id}`)
      .pipe(map(r => r.data));
  }

  submit(id: string, rating: number, comment: string | null): Observable<SurveySubmitResponse> {
    return this.http.post<SurveySubmitResponse>(
      `/api/v1/portal/surveys/${id}/submit`,
      { rating, comment }
    );
  }
}
