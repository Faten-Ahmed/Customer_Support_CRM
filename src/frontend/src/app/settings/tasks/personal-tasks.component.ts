import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe } from '@angular/common';
import { AgentTaskService, AgentTaskDto } from '../agent-task.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-personal-tasks',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSlideToggleModule,
    MatChipsModule,
    MatDividerModule,
    MatListModule,
    MatToolbarModule,
    MatTooltipModule,
    DatePipe,
    TranslatePipe,
  ],
  template: `
    <div class="tasks-page">
      <mat-toolbar color="primary">
        <span>{{ 'nav.tasks' | translate }}</span>
      </mat-toolbar>

      <div class="content-area">

        <!-- Add Task Form -->
        <mat-card class="form-card">
          <mat-card-header>
            <mat-card-title>{{ 'tasks.addTask' | translate }}</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="form-row">
              <mat-form-field appearance="outline" class="title-field">
                <mat-label>{{ 'tasks.taskTitle' | translate }}</mat-label>
                <input matInput [(ngModel)]="newTitle" name="newTitle" required
                       placeholder="Enter task title…" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="desc-field">
                <mat-label>{{ 'common.description' | translate }}</mat-label>
                <input matInput [(ngModel)]="newDescription" name="newDescription" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="date-field">
                <mat-label>{{ 'tasks.dueDate' | translate }}</mat-label>
                <input matInput [matDatepicker]="picker" [(ngModel)]="newDueDate" name="newDueDate" />
                <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
                <mat-datepicker #picker></mat-datepicker>
              </mat-form-field>

              <button mat-raised-button color="primary"
                      [disabled]="!newTitle.trim() || adding()"
                      (click)="addTask()">
                @if (adding()) {
                  <mat-spinner diameter="18" style="display:inline-block"></mat-spinner>
                } @else {
                  <ng-container>
                    <mat-icon>add</mat-icon> {{ 'common.add' | translate }}
                  </ng-container>
                }
              </button>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Toggle: Show Completed -->
        <div class="toggle-row">
          <mat-slide-toggle [(ngModel)]="showCompleted" (change)="onToggleCompleted()">
            {{ 'tasks.showCompleted' | translate }}
          </mat-slide-toggle>
        </div>

        @if (loading()) {
          <div class="loading-container">
            <mat-spinner diameter="40"></mat-spinner>
          </div>
        } @else {

          <!-- Active Tasks -->
          <mat-card class="section-card">
            <mat-card-header>
              <mat-card-title>{{ 'tasks.activeTasks' | translate }} ({{ activeTasks().length }})</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (activeTasks().length === 0) {
                <p class="empty-message">{{ 'tasks.noActive' | translate }}</p>
              } @else {
                <mat-list>
                  @for (task of activeTasks(); track task.id) {
                    <mat-list-item class="task-item">
                      <div class="task-content">
                        <div class="task-main">
                          <span class="task-title">{{ task.title }}</span>
                          @if (task.description) {
                            <span class="task-desc">{{ task.description }}</span>
                          }
                          @if (task.dueAt) {
                            <span class="task-due" [class.overdue]="task.isOverdue">
                              <mat-icon style="font-size: 14px; height: 14px; width: 14px; vertical-align: middle;">schedule</mat-icon>
                              {{ task.dueAt | date:'mediumDate' }}
                            </span>
                          }
                        </div>
                        <div class="task-actions">
                          <button mat-icon-button color="primary"
                                  [matTooltip]="'tasks.complete' | translate"
                                  (click)="completeTask(task)">
                            <mat-icon>check_circle</mat-icon>
                          </button>
                          <button mat-icon-button color="warn"
                                  [matTooltip]="'common.delete' | translate"
                                  (click)="deleteTask(task)">
                            <mat-icon>delete</mat-icon>
                          </button>
                        </div>
                      </div>
                    </mat-list-item>
                    <mat-divider></mat-divider>
                  }
                </mat-list>
              }
            </mat-card-content>
          </mat-card>

          <!-- Completed Tasks (shown if toggle on) -->
          @if (showCompleted) {
            <mat-card class="section-card">
              <mat-card-header>
                <mat-card-title>{{ 'tasks.completedTasks' | translate }} ({{ completedTasks().length }})</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                @if (completedTasks().length === 0) {
                  <p class="empty-message">{{ 'tasks.noCompleted' | translate }}</p>
                } @else {
                  <mat-list>
                    @for (task of completedTasks(); track task.id) {
                      <mat-list-item class="task-item">
                        <div class="task-content">
                          <div class="task-main">
                            <span class="task-title completed-title">{{ task.title }}</span>
                            @if (task.completedAt) {
                              <span class="task-due">
                                Completed: {{ task.completedAt | date:'mediumDate' }}
                              </span>
                            }
                          </div>
                          <div class="task-actions">
                            <button mat-icon-button color="warn"
                                    [matTooltip]="'common.delete' | translate"
                                    (click)="deleteTask(task)">
                              <mat-icon>delete</mat-icon>
                            </button>
                          </div>
                        </div>
                      </mat-list-item>
                      <mat-divider></mat-divider>
                    }
                  </mat-list>
                }
              </mat-card-content>
            </mat-card>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .tasks-page {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .content-area {
      padding: 16px;
      flex: 1;
      overflow: auto;
    }

    .form-card {
      margin-bottom: 16px;
    }

    .form-row {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      flex-wrap: wrap;
    }

    .title-field {
      flex: 2;
      min-width: 200px;
    }

    .desc-field {
      flex: 2;
      min-width: 200px;
    }

    .date-field {
      flex: 1;
      min-width: 160px;
    }

    .toggle-row {
      margin-bottom: 16px;
      padding: 0 4px;
    }

    .section-card {
      margin-bottom: 16px;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 32px;
    }

    .empty-message {
      text-align: center;
      padding: 16px;
      color: rgba(0,0,0,0.5);
    }

    .task-item {
      height: auto !important;
      padding: 8px 0;
    }

    .task-content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
      gap: 8px;
    }

    .task-main {
      display: flex;
      flex-direction: column;
      gap: 2px;
      flex: 1;
    }

    .task-title {
      font-size: 1rem;
      font-weight: 500;
    }

    .completed-title {
      text-decoration: line-through;
      color: rgba(0,0,0,0.5);
    }

    .task-desc {
      font-size: 0.85rem;
      color: rgba(0,0,0,0.6);
    }

    .task-due {
      font-size: 0.8rem;
      color: rgba(0,0,0,0.5);
    }

    .task-due.overdue {
      color: #f44336;
      font-weight: 500;
    }

    .task-actions {
      display: flex;
      gap: 4px;
      flex-shrink: 0;
    }
  `],
})
export class PersonalTasksComponent implements OnInit {
  private readonly taskService = inject(AgentTaskService);
  private readonly snackBar = inject(MatSnackBar);

  readonly allTasks = signal<AgentTaskDto[]>([]);
  readonly loading = signal(false);
  readonly adding = signal(false);

  showCompleted = false;

  newTitle = '';
  newDescription = '';
  newDueDate: Date | null = null;

  readonly activeTasks = computed(() => this.allTasks().filter(t => t.status !== 'Completed'));
  readonly completedTasks = computed(() => this.allTasks().filter(t => t.status === 'Completed'));

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.taskService.listTasks(this.showCompleted, 1, 200).subscribe({
      next: page => {
        this.allTasks.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onToggleCompleted(): void {
    this.loadTasks();
  }

  addTask(): void {
    if (!this.newTitle.trim()) return;
    this.adding.set(true);

    const payload = {
      title: this.newTitle.trim(),
      description: this.newDescription.trim() || undefined,
      dueAt: this.newDueDate ? this.newDueDate.toISOString() : undefined,
    };

    this.taskService.createTask(payload).subscribe({
      next: resp => {
        this.adding.set(false);
        const newTask: AgentTaskDto = {
          id: resp.id,
          title: payload.title,
          description: payload.description,
          priority: 'Medium',
          status: 'Pending',
          dueAt: payload.dueAt,
          isOverdue: false,
          createdAt: new Date().toISOString(),
        };
        this.allTasks.update(list => [newTask, ...list]);
        this.newTitle = '';
        this.newDescription = '';
        this.newDueDate = null;
      },
      error: (err) => {
        this.adding.set(false);
        if (err?.status === 422) {
          const code = err?.error?.errors?.[0]?.code;
          if (code === 'MAX_TASKS_REACHED') {
            this.snackBar.open('Maximum 200 active tasks reached. Complete or delete some tasks first.', 'OK', { duration: 6000 });
            return;
          }
        }
        this.snackBar.open('Failed to create task. Please try again.', 'OK', { duration: 4000 });
      },
    });
  }

  completeTask(task: AgentTaskDto): void {
    this.taskService.completeTask(task.id).subscribe({
      next: updated => {
        this.allTasks.update(list =>
          list.map(t => t.id === task.id ? updated : t)
        );
      },
      error: () => {
        this.snackBar.open('Failed to complete task.', 'OK', { duration: 4000 });
      },
    });
  }

  deleteTask(task: AgentTaskDto): void {
    this.taskService.deleteTask(task.id).subscribe({
      next: () => {
        this.allTasks.update(list => list.filter(t => t.id !== task.id));
      },
      error: () => {
        this.snackBar.open('Failed to delete task.', 'OK', { duration: 4000 });
      },
    });
  }

}
