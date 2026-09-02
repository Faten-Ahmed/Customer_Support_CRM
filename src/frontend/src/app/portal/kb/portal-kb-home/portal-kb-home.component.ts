import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PortalKbService, PortalKbCategory, PortalKbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-portal-kb-home',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './portal-kb-home.component.html',
  styleUrl: './portal-kb-home.component.scss',
})
export class PortalKbHomeComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly categories = signal<PortalKbCategory[]>([]);
  readonly articles = signal<PortalKbArticleSummary[]>([]);
  readonly loading = signal(false);
  readonly activeCategoryId = signal<string | null>(null);
  readonly activeCategoryName = signal<string | null>(null);
  readonly searchControl = new FormControl('');

  ngOnInit(): void {
    this.kbService.getCategories().subscribe(c => this.categories.set(c));

    this.route.queryParams.subscribe(params => {
      const categoryId = params['categoryId'] ?? null;
      this.activeCategoryId.set(categoryId);

      if (categoryId) {
        this.kbService.getCategories().subscribe(cats => {
          const cat = cats.find(c => c.id === categoryId);
          this.activeCategoryName.set(cat?.name ?? null);
        });
      } else {
        this.activeCategoryName.set(null);
      }

      this.loadArticles(categoryId);
    });
  }

  private loadArticles(categoryId: string | null): void {
    this.loading.set(true);
    const options = categoryId
      ? { categoryId, pageSize: 20 }
      : { pageSize: 6 };

    this.kbService.list(options).subscribe({
      next: r => {
        this.articles.set(r.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  search(): void {
    const q = this.searchControl.value?.trim();
    if (q) this.router.navigate(['/portal/kb/search'], { queryParams: { q } });
  }

  clearCategory(): void {
    this.router.navigate(['/portal/kb']);
  }

  articleTitle(article: PortalKbArticleSummary): string {
    return this.i18n.lang() === 'ar' && article.titleAr ? article.titleAr : article.title;
  }
}
