import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type KbStatus = 'Draft' | 'PendingReview' | 'Published' | 'Archived';
export type KbVisibility = 'Internal' | 'Public' | 'Both';

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
  rejectionNote?: string;
  createdAt: string;
}

export interface KbSearchResult {
  id: string;
  title: string;
  titleAr?: string;
  categoryId: string;
  visibility: string;
  publishedAt?: string;
  excerpt: string;
}

export interface KbCategory {
  id: string;
  name: string;
}

export interface KbListQuery {
  page: number;
  pageSize: number;
  status?: KbStatus;
  categoryId?: string;
}

@Injectable({ providedIn: 'root' })
export class KbService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/kb/articles';

  listCategories(): Observable<KbCategory[]> {
    return this.http.get<KbCategory[]>('/api/v1/kb/categories');
  }

  list(query: KbListQuery): Observable<{ items: KbArticle[]; totalCount: number }> {
    let params = new HttpParams()
      .set('page', String(query.page))
      .set('pageSize', String(query.pageSize));
    if (query.status) params = params.set('status', query.status);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    return this.http.get<{ items: KbArticle[]; totalCount: number }>(this.base, { params });
  }

  search(q: string): Observable<KbSearchResult[]> {
    const params = new HttpParams().set('q', q);
    return this.http.get<KbSearchResult[]>('/api/v1/kb/search', { params });
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

  archive(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/archive`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
