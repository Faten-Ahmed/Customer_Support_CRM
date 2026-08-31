import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { PortalKbService, KbCategory, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

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
  ],
  templateUrl: './portal-kb-home.component.html',
})
export class PortalKbHomeComponent implements OnInit {
  private readonly kbService = inject(PortalKbService);
  readonly i18n = inject(I18nService);
  private readonly router = inject(Router);

  readonly categories = signal<KbCategory[]>([]);
  readonly featured = signal<KbArticleSummary[]>([]);
  readonly searchControl = new FormControl('');

  ngOnInit(): void {
    this.kbService.getCategories().subscribe(c => this.categories.set(c));
    this.kbService.list({ featured: true }).subscribe(a => this.featured.set(a));
  }

  search(): void {
    const q = this.searchControl.value?.trim();
    if (q) this.router.navigate(['/portal/kb/search'], { queryParams: { q } });
  }

  label(obj: { en: string; ar: string }): string {
    return this.i18n.lang() === 'ar' ? obj.ar : obj.en;
  }
}
