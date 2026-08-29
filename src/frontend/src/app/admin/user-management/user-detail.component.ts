import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService, UserDetail } from './user.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './user-detail.component.html',
})
export class UserDetailComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly route = inject(ActivatedRoute);

  readonly user = signal<UserDetail | null>(null);
  readonly loading = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loading.set(true);
      this.userService.getById(id).subscribe({
        next: res => {
          this.user.set(res.data);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    }
  }

  deactivate(): void {
    const id = this.user()?.id;
    if (!id) return;
    this.userService.deactivate(id).subscribe(res => {
      this.user.update(u => (u ? { ...u, isActive: false } : u));
    });
  }

  reactivate(): void {
    const id = this.user()?.id;
    if (!id) return;
    this.userService.reactivate(id).subscribe(res => {
      this.user.update(u => (u ? { ...u, isActive: true } : u));
    });
  }
}
