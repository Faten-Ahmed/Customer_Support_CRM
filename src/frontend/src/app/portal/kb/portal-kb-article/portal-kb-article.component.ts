import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { PortalKbService, KbArticle } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

@Component({
  selector: 'app-portal-kb-article',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule],
  templateUrl: './portal-kb-article.component.html',
})
export class PortalKbArticleComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);
  private readonly sanitizer = inject(DomSanitizer);

  readonly article = signal<KbArticle | null>(null);
  readonly feedbackGiven = signal<'up' | 'down' | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.kbService.getById(id).subscribe(a => this.article.set(a));
  }

  label(obj: { en: string; ar: string }): string {
    return this.i18n.lang() === 'ar' ? obj.ar : obj.en;
  }

  safeContent(): SafeHtml {
    const a = this.article();
    if (!a) return '';
    const raw = this.i18n.lang() === 'ar' ? a.content.ar : a.content.en;
    return this.sanitizer.bypassSecurityTrustHtml(raw);
  }

  submitFeedback(helpful: boolean): void {
    this.feedbackGiven.set(helpful ? 'up' : 'down');
  }
}
