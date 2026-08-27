import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule],
  template: `
    <div class="error-page">
      <mat-icon class="error-icon">search_off</mat-icon>
      <h1 class="error-code">404</h1>
      <p class="error-title">Page not found</p>
      <p class="error-desc">The page you're looking for doesn't exist or has been moved.</p>
      <a mat-raised-button color="primary" routerLink="/app">Go to Dashboard</a>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .error-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      gap: 1rem;
      text-align: center;
      padding: 2rem;
    }
    .error-icon { font-size: 6rem; width: 6rem; height: 6rem; color: #d1d5db; }
    .error-code { font-size: 4rem; font-weight: 700; color: #d1d5db; margin: 0; }
    .error-title { font-size: 1.25rem; color: #6b7280; margin: 0; }
    .error-desc { color: #9ca3af; margin: 0; }
  `],
})
export class NotFoundComponent {}
