import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PortalKbService, PortalKbSearchResult } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

@Component({
  selector: 'app-portal-kb-search',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './portal-kb-search.component.html',
  styleUrl: './portal-kb-search.component.scss',
})
export class PortalKbSearchComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);

  readonly results = signal<PortalKbSearchResult[]>([]);
  readonly loading = signal(false);
  readonly errorMsg = signal<string | null>(null);
  query = '';

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q) {
        this.query = q;
        this.runSearch(q);
      }
    });
  }

  runSearch(q: string): void {
    this.loading.set(true);
    this.errorMsg.set(null);
    this.kbService.search(q).subscribe({
      next: res => {
        this.results.set(res);
        this.loading.set(false);
      },
      error: (err) => {
        const msg = err?.error?.error ?? err?.error?.message ?? 'Search failed. Please try again.';
        this.errorMsg.set(msg);
        this.results.set([]);
        this.loading.set(false);
      },
    });
  }

  articleTitle(article: PortalKbSearchResult): string {
    return this.i18n.lang() === 'ar' && article.titleAr ? article.titleAr : article.title;
  }
}
