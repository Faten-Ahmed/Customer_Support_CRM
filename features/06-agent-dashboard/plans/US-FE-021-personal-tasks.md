# Personal Tasks Panel — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-021
**Goal:** Implement the personal tasks feature at `/tasks` — a list with inline add, checkbox-complete, past-due highlighting in red, immediate delete, and a counter badge showing incomplete count.

**Architecture:** `PersonalTasksComponent` is standalone, lazy-loaded. Tasks are loaded from `TaskService.list()` on init and sorted client-side (incomplete by due date, then completed). The inline add form uses a reactive form with optional due date. Complete/delete call their respective service methods optimistically updating the signal array.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tasks/task.service.ts` |
| Create | `src/app/tasks/task.service.spec.ts` |
| Create | `src/app/tasks/personal-tasks/personal-tasks.component.ts` |
| Create | `src/app/tasks/personal-tasks/personal-tasks.component.html` |
| Create | `src/app/tasks/personal-tasks/personal-tasks.component.spec.ts` |

---

## Task 1: TaskService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tasks/task.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TaskService],
    });
    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/tasks', () => {
    service.list().subscribe();
    const req = httpMock.expectOne('/api/v1/tasks');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('create() should POST /api/v1/tasks', () => {
    service.create({ title: 'Call customer', dueDate: '2025-12-01' }).subscribe();
    const req = httpMock.expectOne('/api/v1/tasks');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.title).toBe('Call customer');
    req.flush({ id: 'task-1', title: 'Call customer', completed: false });
  });

  it('complete() should PATCH /api/v1/tasks/{id}', () => {
    service.complete('task-1').subscribe();
    const req = httpMock.expectOne('/api/v1/tasks/task-1');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ completed: true });
    req.flush({ id: 'task-1', completed: true });
  });

  it('delete() should DELETE /api/v1/tasks/{id}', () => {
    service.delete('task-1').subscribe();
    const req = httpMock.expectOne('/api/v1/tasks/task-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tasks/task.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tasks/task.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Task {
  id: string;
  title: string;
  description?: string;
  dueDate?: string;
  completed: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  list(): Observable<Task[]> {
    return this.http.get<Task[]>('/api/v1/tasks');
  }

  create(payload: { title: string; description?: string; dueDate?: string }): Observable<Task> {
    return this.http.post<Task>('/api/v1/tasks', payload);
  }

  complete(id: string): Observable<Task> {
    return this.http.patch<Task>(`/api/v1/tasks/${id}`, { completed: true });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/tasks/${id}`);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tasks/task.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tasks/task.service.ts src/app/tasks/task.service.spec.ts
git commit -m "feat(tasks): add TaskService with list/create/complete/delete (US-FE-021)"
```

---

## Task 2: PersonalTasksComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tasks/personal-tasks/personal-tasks.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { PersonalTasksComponent } from './personal-tasks.component';
import { TaskService, Task } from '../task.service';

const today = new Date().toISOString().split('T')[0];
const yesterday = new Date(Date.now() - 86400000).toISOString().split('T')[0];

const mockTasks: Task[] = [
  { id: 't1', title: 'Call customer', completed: false, dueDate: today, createdAt: '2025-01-01' },
  { id: 't2', title: 'Send report', completed: false, dueDate: yesterday, createdAt: '2025-01-01' },
  { id: 't3', title: 'Done task', completed: true, dueDate: today, createdAt: '2025-01-01' },
];

describe('PersonalTasksComponent', () => {
  let fixture: ComponentFixture<PersonalTasksComponent>;
  let component: PersonalTasksComponent;
  let taskService: jasmine.SpyObj<TaskService>;

  beforeEach(async () => {
    taskService = jasmine.createSpyObj('TaskService', ['list', 'create', 'complete', 'delete']);
    taskService.list.and.returnValue(of(mockTasks));
    taskService.create.and.returnValue(of({ id: 't4', title: 'New task', completed: false, createdAt: new Date().toISOString() }));
    taskService.complete.and.returnValue(of({ ...mockTasks[0], completed: true }));
    taskService.delete.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [PersonalTasksComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: TaskService, useValue: taskService }],
    }).compileComponents();

    fixture = TestBed.createComponent(PersonalTasksComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load tasks', () => {
    expect(component).toBeTruthy();
    expect(component.tasks().length).toBe(3);
  });

  it('should show incomplete task count badge', () => {
    expect(component.incompleteCount()).toBe(2);
  });

  it('should mark task past-due when due date is before today', () => {
    const pastDue = component.isPastDue(mockTasks[1]);
    expect(pastDue).toBeTrue();
  });

  it('should mark task as complete and move to completed section', () => {
    component.completeTask(mockTasks[0]);
    expect(taskService.complete).toHaveBeenCalledWith('t1');
    expect(component.incompleteTasks().length).toBe(1);
  });

  it('should delete task immediately without confirmation', () => {
    component.deleteTask('t1');
    expect(taskService.delete).toHaveBeenCalledWith('t1');
    expect(component.tasks().length).toBe(2);
  });

  it('should add new task via inline form', () => {
    component.addForm.setValue({ title: 'New task', description: '', dueDate: '' });
    component.addTask();
    expect(taskService.create).toHaveBeenCalledWith({ title: 'New task', description: undefined, dueDate: undefined });
    expect(component.tasks().length).toBe(4);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tasks/personal-tasks/personal-tasks.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tasks/personal-tasks/personal-tasks.component.ts

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatBadgeModule } from '@angular/material/badge';
import { Task, TaskService } from '../task.service';

@Component({
  selector: 'app-personal-tasks',
  standalone: true,
  imports: [
    CommonModule, DatePipe, ReactiveFormsModule, MatCheckboxModule, MatIconModule,
    MatButtonModule, MatFormFieldModule, MatInputModule, MatDatepickerModule,
    MatNativeDateModule, MatBadgeModule,
  ],
  templateUrl: './personal-tasks.component.html',
})
export class PersonalTasksComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);

  readonly tasks = signal<Task[]>([]);

  readonly incompleteTasks = computed(() =>
    this.tasks()
      .filter(t => !t.completed)
      .sort((a, b) => {
        if (!a.dueDate) return 1;
        if (!b.dueDate) return -1;
        return a.dueDate.localeCompare(b.dueDate);
      })
  );

  readonly completedTasks = computed(() => this.tasks().filter(t => t.completed));
  readonly incompleteCount = computed(() => this.incompleteTasks().length);

  addForm = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    dueDate: [''],
  });

  ngOnInit(): void {
    this.taskService.list().subscribe(tasks => this.tasks.set(tasks));
  }

  isPastDue(task: Task): boolean {
    if (!task.dueDate || task.completed) return false;
    return task.dueDate < new Date().toISOString().split('T')[0];
  }

  addTask(): void {
    if (this.addForm.invalid) return;
    const { title, description, dueDate } = this.addForm.value;
    this.taskService.create({
      title: title!,
      description: description || undefined,
      dueDate: dueDate || undefined,
    }).subscribe(created => {
      this.tasks.update(list => [...list, created]);
      this.addForm.reset();
    });
  }

  completeTask(task: Task): void {
    this.taskService.complete(task.id).subscribe(updated => {
      this.tasks.update(list => list.map(t => t.id === updated.id ? updated : t));
    });
  }

  deleteTask(id: string): void {
    this.taskService.delete(id).subscribe(() => {
      this.tasks.update(list => list.filter(t => t.id !== id));
    });
  }
}
```

```html
<!-- src/app/tasks/personal-tasks/personal-tasks.component.html -->

<div class="p-6 max-w-2xl">
  <div class="flex items-center gap-3 mb-6">
    <h1 class="text-2xl font-semibold">My Tasks</h1>
    <span class="bg-blue-600 text-white rounded-full px-2 py-0.5 text-xs" *ngIf="incompleteCount() > 0">
      {{ incompleteCount() }}
    </span>
  </div>

  <!-- Add task form -->
  <form [formGroup]="addForm" (ngSubmit)="addTask()" class="flex gap-2 mb-6 items-end">
    <mat-form-field appearance="outline" class="flex-1">
      <mat-label>New task title</mat-label>
      <input matInput formControlName="title" />
    </mat-form-field>
    <mat-form-field appearance="outline" class="w-40">
      <mat-label>Due date</mat-label>
      <input matInput formControlName="dueDate" type="date" />
    </mat-form-field>
    <button mat-raised-button color="primary" type="submit" [disabled]="addForm.invalid" class="mb-5">
      Add
    </button>
  </form>

  <!-- Incomplete tasks -->
  <div class="flex flex-col gap-2 mb-6">
    @for (task of incompleteTasks(); track task.id) {
      <div class="flex items-center gap-3 p-3 border rounded-lg"
           [class.border-red-300]="isPastDue(task)" [class.bg-red-50]="isPastDue(task)">
        <mat-checkbox (change)="completeTask(task)"></mat-checkbox>
        <div class="flex-1">
          <p class="font-medium" [class.text-red-700]="isPastDue(task)">{{ task.title }}</p>
          @if (task.dueDate) {
            <p class="text-xs" [class.text-red-500]="isPastDue(task)" [class.text-gray-400]="!isPastDue(task)">
              Due: {{ task.dueDate | date:'mediumDate' }}
            </p>
          }
        </div>
        <button mat-icon-button color="warn" (click)="deleteTask(task.id)">
          <mat-icon>delete</mat-icon>
        </button>
      </div>
    }
  </div>

  <!-- Completed tasks -->
  @if (completedTasks().length > 0) {
    <h2 class="text-sm font-semibold text-gray-500 mb-2">Completed</h2>
    <div class="flex flex-col gap-1">
      @for (task of completedTasks(); track task.id) {
        <div class="flex items-center gap-3 p-2 opacity-50">
          <mat-icon class="text-green-500">check_circle</mat-icon>
          <p class="line-through text-sm">{{ task.title }}</p>
          <button mat-icon-button color="warn" (click)="deleteTask(task.id)" class="ml-auto">
            <mat-icon>delete</mat-icon>
          </button>
        </div>
      }
    </div>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tasks/personal-tasks/personal-tasks.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tasks/personal-tasks/
git commit -m "feat(tasks): implement PersonalTasksComponent with past-due highlighting (US-FE-021)"
```
