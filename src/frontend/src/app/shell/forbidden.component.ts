import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '../shared/pipes/translate.pipe';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule, TranslatePipe],
  template: `
    <div class="error-page">
      <mat-icon class="error-icon">block</mat-icon>
      <h1 class="error-code">403</h1>
      <p class="error-title">{{ 'error.forbidden' | translate }}</p>
      <p class="error-desc">{{ 'error.forbiddenDesc' | translate }}</p>
      <a mat-raised-button color="warn" routerLink="/app">{{ 'error.goToDashboard' | translate }}</a>
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
    .error-icon { font-size: 6rem; width: 6rem; height: 6rem; color: #fca5a5; }
    .error-code { font-size: 4rem; font-weight: 700; color: #fca5a5; margin: 0; }
    .error-title { font-size: 1.25rem; color: #6b7280; margin: 0; }
    .error-desc { color: #9ca3af; margin: 0; }
  `],
})
export class ForbiddenComponent {}
