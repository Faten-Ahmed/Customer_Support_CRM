import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { KbArticle, KbService } from '../services/kb.service';
import { AuthStore } from '../../auth/auth.store';
import { RejectDialogComponent } from './reject-dialog.component';

@Component({
  selector: 'app-kb-article-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatDialogModule,
    MatSnackBarModule,
    MatIconModule,
    MatChipsModule,
  ],
  templateUrl: './kb-article-detail.component.html',
})
export class KbArticleDetailComponent implements OnInit {
  private readonly kbService = inject(KbService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly authStore = inject(AuthStore);

  readonly article = signal<KbArticle | null>(null);

  get canReview(): boolean {
    const role = this.authStore.user()?.role;
    return (role === 'Manager' || role === 'Admin') && this.article()?.status === 'PendingReview';
  }

  get canArchive(): boolean {
    const role = this.authStore.user()?.role;
    return (role === 'Manager' || role === 'Admin') && this.article()?.status !== 'Archived';
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.kbService.getById(params['id']).subscribe(art => this.article.set(art));
    });
  }

  approve(): void {
    const id = this.article()?.id;
    if (!id) return;
    this.kbService.approve(id).subscribe(() => {
      this.snackBar.open('Article published', 'OK', { duration: 3000 });
      this.router.navigate(['/app/kb']);
    });
  }

  openRejectDialog(): void {
    const id = this.article()?.id;
    if (!id) return;
    const ref = this.dialog.open(RejectDialogComponent, {
      width: '480px',
      data: { articleId: id },
    });
    ref.afterClosed().subscribe(rejected => {
      if (rejected) this.router.navigate(['/app/kb']);
    });
  }

  archive(): void {
    const id = this.article()?.id;
    if (!id) return;
    this.kbService.archive(id).subscribe(() => {
      this.snackBar.open('Article archived', 'OK', { duration: 3000 });
      this.router.navigate(['/app/kb']);
    });
  }
}
