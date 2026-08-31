import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { KbService, KbCategory } from '../services/kb.service';

@Component({
  selector: 'app-kb-article-editor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatSnackBarModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './kb-article-editor.component.html',
  styleUrl: './kb-article-editor.component.scss',
})
export class KbArticleEditorComponent implements OnInit {
  private readonly kbService = inject(KbService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  articleId: string | null = null;
  isEditMode = false;
  readonly saving = signal(false);

  readonly categories = signal<KbCategory[]>([]);

  readonly form = this.fb.group({
    title: ['', Validators.required],
    titleAr: [''],
    categoryId: ['', Validators.required],
    visibility: ['Internal', Validators.required],
    content: [''],
    contentAr: [''],
  });

  ngOnInit(): void {
    this.kbService.listCategories().subscribe(cats => this.categories.set(cats));

    this.route.params.subscribe(params => {
      if (params['id']) {
        this.articleId = params['id'];
        this.isEditMode = true;
        this.kbService.getById(this.articleId!).subscribe(art => this.form.patchValue(art));
      }
    });
  }

  saveDraft(): void {
    if (this.form.invalid) return;
    const val = this.form.value as any;
    this.saving.set(true);
    if (this.articleId) {
      this.kbService.update(this.articleId, val).subscribe({
        next: () => {
          this.snackBar.open('Draft saved', 'OK', { duration: 2000 });
          this.saving.set(false);
        },
        error: () => this.saving.set(false),
      });
    } else {
      this.kbService.create(val).subscribe({
        next: art => {
          this.articleId = art.id;
          this.isEditMode = true;
          this.saving.set(false);
          this.snackBar.open('Draft saved', 'OK', { duration: 2000 });
          this.router.navigate(['/app/kb/articles', art.id, 'edit']);
        },
        error: () => this.saving.set(false),
      });
    }
  }

  submitForReview(): void {
    if (this.form.invalid) return;
    const val = this.form.value as any;
    this.saving.set(true);
    const save$ = this.articleId
      ? this.kbService.update(this.articleId, val)
      : this.kbService.create(val);

    save$.subscribe({
      next: art => {
        this.articleId = art.id;
        this.kbService.submitForReview(this.articleId!).subscribe({
          next: () => {
            this.snackBar.open('Submitted for review', 'OK', { duration: 3000 });
            this.router.navigate(['/app/kb']);
          },
          error: () => this.saving.set(false),
        });
      },
      error: () => this.saving.set(false),
    });
  }
}
