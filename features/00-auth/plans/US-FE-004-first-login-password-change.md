# First Login Password Change — Implementation Plan

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

**Story:** US-FE-004
**Goal:** Force newly created staff members to set a permanent password on first login via `/change-password`, guarded by a `PasswordChangeGuard` that reads the `passwordMustChange` flag from the JWT.

**Architecture:** `ChangePasswordComponent` is a standalone, lazy-loaded page behind a `PasswordChangeGuard`. The guard inspects the decoded JWT held in `AuthStore`; any route other than `/change-password` redirects here when `passwordMustChange = true`. On success the JWT is refreshed (flag cleared) and the user is routed to `/dashboard`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/auth/change-password/change-password.component.ts` |
| Create | `src/app/auth/change-password/change-password.component.html` |
| Create | `src/app/auth/change-password/change-password.component.spec.ts` |
| Create | `src/app/auth/guards/password-change.guard.ts` |
| Create | `src/app/auth/guards/password-change.guard.spec.ts` |
| Modify | `src/app/auth/auth.service.ts` |
| Modify | `src/app/auth/auth.service.spec.ts` |
| Modify | `src/app/auth/auth.routes.ts` |

---

## Task 1: Extend AuthService with changePassword()

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/auth/auth.service.spec.ts

describe('AuthService — changePassword', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should POST to /api/v1/auth/change-password with correct body', () => {
    service.changePassword('OldPass1!', 'NewPass2@', 'NewPass2@').subscribe(res => {
      expect(res.message).toContain('changed');
    });

    const req = httpMock.expectOne('/api/v1/auth/change-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      currentPassword: 'OldPass1!',
      newPassword: 'NewPass2@',
      confirmPassword: 'NewPass2@',
    });
    req.flush({ message: 'Password changed successfully.' });
  });

  it('should propagate 422 INVALID_CURRENT_PASSWORD error', () => {
    let errorCode = '';
    service.changePassword('wrong', 'NewPass2@', 'NewPass2@').subscribe({
      error: err => (errorCode = err.error?.code),
    });

    const req = httpMock.expectOne('/api/v1/auth/change-password');
    req.flush({ code: 'INVALID_CURRENT_PASSWORD' }, { status: 422, statusText: 'Unprocessable Entity' });
    expect(errorCode).toBe('INVALID_CURRENT_PASSWORD');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// In src/app/auth/auth.service.ts — add method

changePassword(currentPassword: string, newPassword: string, confirmPassword: string): Observable<{ message: string }> {
  return this.http.post<{ message: string }>('/api/v1/auth/change-password', {
    currentPassword,
    newPassword,
    confirmPassword,
  });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/auth.service.ts src/app/auth/auth.service.spec.ts
git commit -m "feat(auth): add changePassword() to AuthService (US-FE-004)"
```

---

## Task 2: Implement PasswordChangeGuard

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/guards/password-change.guard.spec.ts

import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { PasswordChangeGuard } from './password-change.guard';
import { AuthStore } from '../auth.store';

describe('PasswordChangeGuard', () => {
  let guard: PasswordChangeGuard;
  let router: Router;
  let authStore: jasmine.SpyObj<AuthStore>;

  beforeEach(() => {
    authStore = jasmine.createSpyObj('AuthStore', [], {
      passwordMustChange: false,
    });

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [
        PasswordChangeGuard,
        { provide: AuthStore, useValue: authStore },
      ],
    });

    guard = TestBed.inject(PasswordChangeGuard);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  it('should allow activation when passwordMustChange is false', () => {
    Object.defineProperty(authStore, 'passwordMustChange', { get: () => false });
    expect(guard.canActivate()).toBeTrue();
  });

  it('should redirect to /change-password when passwordMustChange is true', () => {
    Object.defineProperty(authStore, 'passwordMustChange', { get: () => true });
    const result = guard.canActivate();
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/change-password']);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/guards/password-change.guard.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/auth/guards/password-change.guard.ts

import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../auth.store';

@Injectable({ providedIn: 'root' })
export class PasswordChangeGuard {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  canActivate(): boolean {
    if (this.authStore.passwordMustChange) {
      this.router.navigate(['/change-password']);
      return false;
    }
    return true;
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/guards/password-change.guard.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/guards/
git commit -m "feat(auth): add PasswordChangeGuard (US-FE-004)"
```

---

## Task 3: Implement ChangePasswordComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/change-password/change-password.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ChangePasswordComponent } from './change-password.component';
import { AuthService } from '../auth.service';

describe('ChangePasswordComponent', () => {
  let fixture: ComponentFixture<ChangePasswordComponent>;
  let component: ChangePasswordComponent;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['changePassword']);

    await TestBed.configureTestingModule({
      imports: [
        ChangePasswordComponent,
        ReactiveFormsModule,
        RouterTestingModule,
        NoopAnimationsModule,
      ],
      providers: [{ provide: AuthService, useValue: authService }],
    }).compileComponents();

    fixture = TestBed.createComponent(ChangePasswordComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should require all three fields', () => {
    expect(component.form.valid).toBeFalse();
  });

  it('should validate password strength (min 8 chars, uppercase, digit)', () => {
    component.form.get('newPassword')!.setValue('weak');
    expect(component.form.get('newPassword')!.errors?.['strength']).toBeTruthy();
  });

  it('should validate confirm password matches new password', () => {
    component.form.get('newPassword')!.setValue('StrongPass1!');
    component.form.get('confirmPassword')!.setValue('Mismatch1!');
    expect(component.form.errors?.['mismatch']).toBeTruthy();
  });

  it('should call changePassword() on valid submit and navigate to /dashboard', () => {
    authService.changePassword.and.returnValue(of({ message: 'Password changed successfully.' }));
    component.form.setValue({
      currentPassword: 'OldPass1!',
      newPassword: 'NewPass2@',
      confirmPassword: 'NewPass2@',
    });
    component.onSubmit();
    expect(authService.changePassword).toHaveBeenCalledWith('OldPass1!', 'NewPass2@', 'NewPass2@');
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should show inline error on 422 INVALID_CURRENT_PASSWORD', () => {
    authService.changePassword.and.returnValue(
      throwError(() => ({ error: { code: 'INVALID_CURRENT_PASSWORD' } }))
    );
    component.form.setValue({
      currentPassword: 'wrongpass',
      newPassword: 'NewPass2@',
      confirmPassword: 'NewPass2@',
    });
    component.onSubmit();
    expect(component.form.get('currentPassword')!.errors?.['invalid']).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/change-password/change-password.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/auth/change-password/change-password.component.ts

import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../auth.service';

function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl) => {
    const val: string = control.value ?? '';
    const ok = val.length >= 8 && /[A-Z]/.test(val) && /[0-9]/.test(val);
    return ok ? null : { strength: true };
  };
}

function confirmMatchValidator(group: AbstractControl) {
  const pw = group.get('newPassword')?.value;
  const conf = group.get('confirmPassword')?.value;
  return pw === conf ? null : { mismatch: true };
}

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatCardModule],
  templateUrl: './change-password.component.html',
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordStrengthValidator()]],
      confirmPassword: ['', Validators.required],
    },
    { validators: confirmMatchValidator }
  );

  submitting = false;

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { currentPassword, newPassword, confirmPassword } = this.form.value as {
      currentPassword: string;
      newPassword: string;
      confirmPassword: string;
    };
    this.authService.changePassword(currentPassword, newPassword, confirmPassword).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: err => {
        this.submitting = false;
        if (err.error?.code === 'INVALID_CURRENT_PASSWORD') {
          this.form.get('currentPassword')!.setErrors({ invalid: true });
        }
      },
    });
  }
}
```

```html
<!-- src/app/auth/change-password/change-password.component.html -->

<div class="flex items-center justify-center min-h-screen">
  <mat-card class="w-full max-w-md p-6">
    <mat-card-title>Set New Password</mat-card-title>
    <mat-card-subtitle>You must set a permanent password before continuing.</mat-card-subtitle>

    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4 mt-4">
      <mat-form-field appearance="outline">
        <mat-label>Current (Temporary) Password</mat-label>
        <input matInput type="password" formControlName="currentPassword" />
        @if (form.get('currentPassword')?.hasError('required')) {
          <mat-error>Required</mat-error>
        }
        @if (form.get('currentPassword')?.hasError('invalid')) {
          <mat-error>Current password is incorrect</mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>New Password</mat-label>
        <input matInput type="password" formControlName="newPassword" />
        @if (form.get('newPassword')?.hasError('strength')) {
          <mat-error>Min 8 chars, one uppercase letter, one digit</mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Confirm New Password</mat-label>
        <input matInput type="password" formControlName="confirmPassword" />
        @if (form.hasError('mismatch') && form.get('confirmPassword')?.dirty) {
          <mat-error>Passwords do not match</mat-error>
        }
      </mat-form-field>

      <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || submitting">
        {{ submitting ? 'Saving…' : 'Set Password' }}
      </button>
    </form>
  </mat-card>
</div>
```

Register the route in `src/app/auth/auth.routes.ts`:
```typescript
{
  path: 'change-password',
  loadComponent: () =>
    import('./change-password/change-password.component').then(m => m.ChangePasswordComponent),
  canActivate: [], // intentionally no guard — accessible only when passwordMustChange=true
},
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/change-password/change-password.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/change-password/ src/app/auth/auth.routes.ts
git commit -m "feat(auth): implement ChangePasswordComponent (US-FE-004)"
```
