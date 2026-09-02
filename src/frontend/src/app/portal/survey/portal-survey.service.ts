import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
    return this.http.get<SurveyDetail>(`/api/v1/portal/surveys/${id}`);
  }

  submit(id: string, rating: number, comment: string | null): Observable<SurveySubmitResponse> {
    return this.http.post<SurveySubmitResponse>(
      `/api/v1/portal/surveys/${id}/submit`,
      { rating, comment }
    );
  }
}
