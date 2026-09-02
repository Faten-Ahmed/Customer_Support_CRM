import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PortalKbCategory {
  id: string;
  name: string;
}

export interface PortalKbArticleSummary {
  id: string;
  title: string;
  titleAr?: string;
  categoryId: string;
  categoryName?: string;
  visibility: string;
  publishedAt?: string;
  createdAt: string;
}

export interface PortalKbSearchResult {
  id: string;
  title: string;
  titleAr?: string;
  categoryId: string;
  visibility: string;
  publishedAt?: string;
  excerpt: string;
}

export interface PortalKbArticle {
  id: string;
  title: string;
  titleAr?: string;
  content?: string;
  contentAr?: string;
  categoryId: string;
  categoryName?: string;
  visibility: string;
  status: string;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class PortalKbService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/portal/kb';

  getCategories(): Observable<PortalKbCategory[]> {
    return this.http.get<PortalKbCategory[]>(`${this.base}/categories`);
  }

  list(options?: { categoryId?: string; page?: number; pageSize?: number }):
      Observable<{ items: PortalKbArticleSummary[]; totalCount: number }> {
    let params = new HttpParams()
      .set('page', String(options?.page ?? 1))
      .set('pageSize', String(options?.pageSize ?? 20));
    if (options?.categoryId) params = params.set('categoryId', options.categoryId);
    return this.http.get<{ items: PortalKbArticleSummary[]; totalCount: number }>(
      `${this.base}/articles`, { params });
  }

  search(q: string): Observable<PortalKbSearchResult[]> {
    const params = new HttpParams().set('q', q);
    return this.http.get<PortalKbSearchResult[]>(`${this.base}/search`, { params });
  }

  getById(id: string): Observable<PortalKbArticle> {
    return this.http.get<PortalKbArticle>(`${this.base}/articles/${id}`);
  }
}
