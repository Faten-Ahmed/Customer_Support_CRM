import { Component, OnInit, inject } from '@angular/core';
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
import { KbService } from '../services/kb.service';

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
  ],
  templateUrl: './kb-article-editor.component.html',
})
export class KbArticleEditorComponent implements OnInit {
  private readonly kbService = inject(KbService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  articleId: string | null = null;
  isEditMode = false;
  saving = false;

  readonly form = this.fb.group({
    title: ['', Validators.required],
    titleAr: [''],
    content: ['', Validators.required],
    contentAr: [''],
    categoryId: [''],
    visibility: ['Public', Validators.required],
  });

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.articleId = params['id'];
        this.isEditMode = true;
        this.kbService.getById(this.articleId!).subscribe(art => this.form.patchValue(art));
      }
    });
  }

  saveDraft(): void {
    const val = this.form.value as any;
    if (this.articleId) {
      this.kbService.update(this.articleId, val).subscribe(() => {
        this.snackBar.open('Draft saved', 'OK', { duration: 2000 });
      });
    } else {
      this.kbService.create(val).subscribe(art => {
        this.articleId = art.id;
        this.isEditMode = true;
        this.snackBar.open('Draft saved', 'OK', { duration: 2000 });
        this.router.navigate(['/app/kb/articles', art.id, 'edit']);
      });
    }
  }

  submitForReview(): void {
    const val = this.form.value as any;
    const save$ = this.articleId
      ? this.kbService.update(this.articleId, val)
      : this.kbService.create(val);

    save$.subscribe(art => {
      this.articleId = art.id;
      this.kbService.submitForReview(this.articleId!).subscribe(() => {
        this.snackBar.open('Submitted for review', 'OK', { duration: 3000 });
        this.router.navigate(['/app/kb']);
      });
    });
  }
}
