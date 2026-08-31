import { Routes } from '@angular/router';

export const KB_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./article-list/kb-article-list.component').then(m => m.KbArticleListComponent),
  },
  {
    path: 'articles/new',
    loadComponent: () =>
      import('./article-editor/kb-article-editor.component').then(m => m.KbArticleEditorComponent),
  },
  {
    path: 'articles/:id/edit',
    loadComponent: () =>
      import('./article-editor/kb-article-editor.component').then(m => m.KbArticleEditorComponent),
  },
  {
    path: 'articles/:id',
    loadComponent: () =>
      import('./article-detail/kb-article-detail.component').then(m => m.KbArticleDetailComponent),
  },
];
