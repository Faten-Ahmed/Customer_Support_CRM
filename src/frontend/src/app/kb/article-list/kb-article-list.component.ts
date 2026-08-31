import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { KbArticle, KbService, KbStatus } from '../services/kb.service';

const STATUS_CLASSES: Record<KbStatus, string> = {
  Draft: 'status-badge status-draft',
  PendingReview: 'status-badge status-pending',
  Published: 'status-badge status-published',
  Archived: 'status-badge status-archived',
};

@Component({
  selector: 'app-kb-article-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './kb-article-list.component.html',
})
export class KbArticleListComponent implements OnInit, OnDestroy {
  private readonly kbService = inject(KbService);
  private readonly destroy$ = new Subject<void>();

  readonly articles = signal<KbArticle[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);

  readonly searchControl = new FormControl('');
  readonly statusFilter = new FormControl<KbStatus | ''>('');

  readonly displayedColumns = ['title', 'category', 'status', 'visibility', 'author', 'publishedAt'];

  ngOnInit(): void {
    this.loadArticles();
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.loadArticles());
    this.statusFilter.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadArticles());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadArticles(): void {
    this.loading.set(true);
    this.kbService.list({
      page: 1,
      pageSize: 50,
      status: (this.statusFilter.value as KbStatus) || undefined,
      search: this.searchControl.value || undefined,
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: res => {
        this.articles.set(res.data);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  badgeClass(status: KbStatus): string {
    return STATUS_CLASSES[status] ?? 'status-badge status-draft';
  }
}
