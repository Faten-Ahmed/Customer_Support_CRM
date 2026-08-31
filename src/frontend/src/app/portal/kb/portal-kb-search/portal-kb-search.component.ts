import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PortalKbService, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

@Component({
  selector: 'app-portal-kb-search',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './portal-kb-search.component.html',
})
export class PortalKbSearchComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);

  readonly results = signal<KbArticleSummary[]>([]);
  readonly loading = signal(false);
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
    this.kbService.search(q).subscribe({
      next: res => {
        this.results.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  label(obj: { en: string; ar: string }): string {
    return this.i18n.lang() === 'ar' ? obj.ar : obj.en;
  }
}
