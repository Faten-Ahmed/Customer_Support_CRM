import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { PortalKbService, PortalKbArticle } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

@Component({
  selector: 'app-portal-kb-article',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule],
  templateUrl: './portal-kb-article.component.html',
  styleUrl: './portal-kb-article.component.scss',
})
export class PortalKbArticleComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);

  readonly article = signal<PortalKbArticle | null>(null);
  readonly feedbackGiven = signal<'up' | 'down' | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.kbService.getById(id).subscribe(a => this.article.set(a));
  }

  articleTitle(a: PortalKbArticle): string {
    return this.i18n.lang() === 'ar' && a.titleAr ? a.titleAr : a.title;
  }

  articleContent(a: PortalKbArticle): string {
    return (this.i18n.lang() === 'ar' && a.contentAr ? a.contentAr : a.content) ?? '';
  }

  submitFeedback(helpful: boolean): void {
    this.feedbackGiven.set(helpful ? 'up' : 'down');
  }
}
