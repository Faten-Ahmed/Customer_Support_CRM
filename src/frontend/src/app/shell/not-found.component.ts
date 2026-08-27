import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-4 text-center">
      <mat-icon class="text-8xl text-gray-300">search_off</mat-icon>
      <h1 class="text-6xl font-bold text-gray-300">404</h1>
      <p class="text-xl text-gray-500">Page not found</p>
      <p class="text-gray-400">The page you're looking for doesn't exist or has been moved.</p>
      <a mat-raised-button color="primary" routerLink="/app">Go to Dashboard</a>
    </div>
  `,
})
export class NotFoundComponent {}
