import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type KbStatus = 'Draft' | 'PendingReview' | 'Published' | 'Archived';
export type KbVisibility = 'Public' | 'Internal' | 'Private';

export interface KbArticle {
  id: string;
  title: string;
  titleAr?: string;
  content: string;
  contentAr?: string;
  categoryId?: string;
  categoryName?: string;
  visibility: KbVisibility;
  status: KbStatus;
  authorName?: string;
  publishedAt?: string;
  createdAt: string;
}

export interface KbListQuery {
  page: number;
  pageSize: number;
  status?: KbStatus;
  categoryId?: string;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class KbService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/kb/articles';

  list(query: KbListQuery): Observable<{ data: KbArticle[]; total: number }> {
    let params = new HttpParams()
      .set('page', String(query.page))
      .set('pageSize', String(query.pageSize));
    if (query.status) params = params.set('status', query.status);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.search) params = params.set('search', query.search);
    return this.http.get<{ data: KbArticle[]; total: number }>(this.base, { params });
  }

  getById(id: string): Observable<KbArticle> {
    return this.http.get<KbArticle>(`${this.base}/${id}`);
  }

  create(payload: Partial<KbArticle>): Observable<KbArticle> {
    return this.http.post<KbArticle>(this.base, payload);
  }

  update(id: string, changes: Partial<KbArticle>): Observable<KbArticle> {
    return this.http.patch<KbArticle>(`${this.base}/${id}`, changes);
  }

  submitForReview(id: string): Observable<KbArticle> {
    return this.http.post<KbArticle>(`${this.base}/${id}/submit-review`, {});
  }

  approve(id: string): Observable<KbArticle> {
    return this.http.post<KbArticle>(`${this.base}/${id}/approve`, {});
  }

  reject(id: string, rejectionNote: string): Observable<KbArticle> {
    return this.http.post<KbArticle>(`${this.base}/${id}/reject`, { rejectionNote });
  }
}
