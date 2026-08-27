import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterModule, MatButtonModule, MatIconModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-4 text-center">
      <mat-icon class="text-8xl text-red-300">block</mat-icon>
      <h1 class="text-6xl font-bold text-red-300">403</h1>
      <p class="text-xl text-gray-500">Access Denied</p>
      <p class="text-gray-400">You don't have permission to view this page.</p>
      <a mat-raised-button color="warn" routerLink="/app">Go to Dashboard</a>
    </div>
  `,
})
export class ForbiddenComponent {}
