import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { UserService, UserSummary } from './user.service';
import { UserFormDialogComponent } from './user-form-dialog.component';
import { UserEditDialogComponent } from './user-edit-dialog.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatDialogModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './user-list.component.html',
})
export class UserListComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);

  readonly users = signal<UserSummary[]>([]);
  readonly loading = signal(false);

  selectedRole = '';
  selectedActive = '';

  readonly displayedColumns = ['name', 'email', 'role', 'department', 'isActive', 'actions'];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    const filters: any = {};
    if (this.selectedRole) filters.role = this.selectedRole;
    if (this.selectedActive !== '') filters.isActive = this.selectedActive === 'true';
    this.userService.list(filters).subscribe({
      next: (res: any) => {
        this.users.set(res.items ?? res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNewUserDialog(): void {
    const ref = this.dialog.open(UserFormDialogComponent);
    ref.afterClosed().subscribe(result => {
      if (result) this.loadUsers();
    });
  }

  openEditDialog(user: UserSummary): void {
    this.dialog.open(UserEditDialogComponent, { data: user }).afterClosed()
      .subscribe(result => { if (result) this.loadUsers(); });
  }

  deactivate(user: UserSummary): void {
    this.userService.deactivate(user.id).subscribe(() => this.loadUsers());
  }

  reactivate(user: UserSummary): void {
    this.userService.reactivate(user.id).subscribe(() => this.loadUsers());
  }
}
