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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { KbService, KbStatus } from '../services/kb.service';

export interface ArticleRow {
  id: string;
  title: string;
  categoryName?: string;
  status?: KbStatus;
  visibility: string;
  authorName?: string;
  publishedAt?: string;
}

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
    MatSnackBarModule,
  ],
  templateUrl: './kb-article-list.component.html',
})
export class KbArticleListComponent implements OnInit, OnDestroy {
  private readonly kbService = inject(KbService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly rows = signal<ArticleRow[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly isSearchMode = signal(false);

  readonly searchControl = new FormControl('');
  readonly statusFilter = new FormControl<KbStatus | ''>('');

  readonly displayedColumns = ['title', 'category', 'status', 'visibility', 'author', 'publishedAt', 'actions'];

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.load());
    this.statusFilter.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.load());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    const q = this.searchControl.value?.trim();
    if (q) {
      this.isSearchMode.set(true);
      this.loading.set(true);
      this.kbService.search(q).pipe(takeUntil(this.destroy$)).subscribe({
        next: results => {
          this.rows.set(results.map(r => ({
            id: r.id,
            title: r.title,
            visibility: r.visibility,
            publishedAt: r.publishedAt,
          })));
          this.total.set(results.length);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    } else {
      this.isSearchMode.set(false);
      this.loading.set(true);
      this.kbService.list({
        page: 1,
        pageSize: 50,
        status: (this.statusFilter.value as KbStatus) || undefined,
      }).pipe(takeUntil(this.destroy$)).subscribe({
        next: res => {
          this.rows.set(res.data.map(a => ({
            id: a.id,
            title: a.title,
            categoryName: a.categoryName,
            status: a.status,
            visibility: a.visibility,
            authorName: a.authorName,
            publishedAt: a.publishedAt,
          })));
          this.total.set(res.total);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    }
  }

  badgeClass(status: KbStatus | undefined): string {
    return status ? (STATUS_CLASSES[status] ?? 'status-badge status-draft') : '';
  }

  delete(row: ArticleRow, event: Event): void {
    event.stopPropagation();
    this.kbService.delete(row.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.snackBar.open('Article deleted', 'OK', { duration: 2000 });
        this.load();
      },
      error: (err) => {
        const msg = err.status === 403 ? 'You can only delete your own draft articles' : 'Delete failed';
        this.snackBar.open(msg, 'OK', { duration: 3000 });
      },
    });
  }
}
