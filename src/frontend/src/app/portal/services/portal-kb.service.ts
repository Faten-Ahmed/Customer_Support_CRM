import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface KbCategory {
  id: string;
  name: { en: string; ar: string };
  articleCount: number;
}

export interface KbArticleSummary {
  id: string;
  title: { en: string; ar: string };
  excerpt: { en: string; ar: string };
  categoryId: string;
  categoryName: { en: string; ar: string };
  featured: boolean;
  updatedAt: string;
}

export interface KbArticle extends KbArticleSummary {
  content: { en: string; ar: string };
}

@Injectable({ providedIn: 'root' })
export class PortalKbService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/portal/kb';

  list(options?: { categoryId?: string; featured?: boolean }): Observable<KbArticleSummary[]> {
    let params = new HttpParams();
    if (options?.categoryId) params = params.set('categoryId', options.categoryId);
    if (options?.featured !== undefined) params = params.set('featured', String(options.featured));
    return this.http.get<KbArticleSummary[]>(`${this.base}/articles`, { params });
  }

  search(q: string): Observable<KbArticleSummary[]> {
    const params = new HttpParams().set('q', q);
    return this.http.get<KbArticleSummary[]>(`${this.base}/search`, { params });
  }

  getById(id: string): Observable<KbArticle> {
    return this.http.get<KbArticle>(`${this.base}/articles/${id}`);
  }

  getCategories(): Observable<KbCategory[]> {
    return this.http.get<KbCategory[]>(`${this.base}/categories`);
  }
}
