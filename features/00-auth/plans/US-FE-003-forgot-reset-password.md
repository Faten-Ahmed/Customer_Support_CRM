# Forgot / Reset Password Flow — Implementation Plan

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

**Story:** US-FE-003
**Goal:** Implement the forgot-password request page and the token-based reset-password page, including a live password strength indicator and confirm-password match validation.

**Architecture:** Both components are standalone, lazy-loaded via `AUTH_ROUTES`. `ForgotPasswordComponent` always shows a success message after submit (security through obscurity — never reveals whether email exists). `ResetPasswordComponent` reads the `?token=` query param, calls `AuthService.resetPassword()`, and handles expired/invalid token errors. Password strength is computed locally as a pure function inside the component.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/auth/forgot-password/forgot-password.component.ts` |
| Create | `src/app/auth/forgot-password/forgot-password.component.html` |
| Create | `src/app/auth/forgot-password/forgot-password.component.spec.ts` |
| Create | `src/app/auth/reset-password/reset-password.component.ts` |
| Create | `src/app/auth/reset-password/reset-password.component.html` |
| Create | `src/app/auth/reset-password/reset-password.component.spec.ts` |
| Modify | `src/app/auth/auth.service.ts` |
| Modify | `src/app/auth/auth.service.spec.ts` |

---

## Task 1: Extend AuthService with forgotPassword and resetPassword

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/auth/auth.service.spec.ts

describe('AuthService — password reset methods', () => {
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

  it('forgotPassword() should POST to /api/v1/auth/forgot-password', () => {
    service.forgotPassword('user@example.com').subscribe(res => {
      expect(res.message).toBeTruthy();
    });

    const req = httpMock.expectOne('/api/v1/auth/forgot-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush({ message: 'If that address is registered, an email has been sent.' });
  });

  it('resetPassword() should POST to /api/v1/auth/reset-password with token and new password', () => {
    service.resetPassword('reset-token-abc', 'NewPassword1!', 'NewPassword1!').subscribe(res => {
      expect(res.message).toContain('reset');
    });

    const req = httpMock.expectOne('/api/v1/auth/reset-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      token: 'reset-token-abc',
      password: 'NewPassword1!',
      confirmPassword: 'NewPassword1!',
    });
    req.flush({ message: 'Password reset successfully.' });
  });

  it('resetPassword() should pass through 400 INVALID_TOKEN error', () => {
    let errorCode = '';
    service.resetPassword('bad-token', 'Pass1!pass', 'Pass1!pass').subscribe({
      error: err => (errorCode = err.error?.code),
    });

    httpMock.expectOne('/api/v1/auth/reset-password').flush(
      { code: 'INVALID_TOKEN' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(errorCode).toBe('INVALID_TOKEN');
  });

  it('resetPassword() should pass through 400 TOKEN_EXPIRED error', () => {
    let errorCode = '';
    service.resetPassword('expired-token', 'Pass1!pass', 'Pass1!pass').subscribe({
      error: err => (errorCode = err.error?.code),
    });

    httpMock.expectOne('/api/v1/auth/reset-password').flush(
      { code: 'TOKEN_EXPIRED' },
      { status: 400, statusText: 'Bad Request' }
    );

    expect(errorCode).toBe('TOKEN_EXPIRED');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```
Expected: FAIL — `forgotPassword`, `resetPassword` not defined.

- [ ] **Step 3: Add methods to AuthService**

```typescript
// Add to src/app/auth/auth.service.ts (inside the AuthService class)

forgotPassword(email: string): Observable<MessageResponse> {
  return this.http.post<MessageResponse>('/api/v1/auth/forgot-password', { email });
}

resetPassword(token: string, password: string, confirmPassword: string): Observable<MessageResponse> {
  return this.http.post<MessageResponse>('/api/v1/auth/reset-password', {
    token,
    password,
    confirmPassword,
  });
}
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```
Expected: All auth service tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/auth.service.ts src/app/auth/auth.service.spec.ts
git commit -m "feat(auth): add forgotPassword and resetPassword methods to AuthService"
```

---

## Task 2: ForgotPasswordComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/forgot-password/forgot-password.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../auth.service';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['forgotPassword']);

    await TestBed.configureTestingModule({
      imports: [
        ForgotPasswordComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should be invalid when email is empty', () => {
    expect(component.forgotForm.valid).toBeFalse();
  });

  it('should be invalid with malformed email', () => {
    component.forgotForm.controls['email'].setValue('notanemail');
    expect(component.forgotForm.controls['email'].valid).toBeFalse();
  });

  it('should be valid with correct email', () => {
    component.forgotForm.controls['email'].setValue('user@example.com');
    expect(component.forgotForm.valid).toBeTrue();
  });

  it('should call forgotPassword on valid submit', fakeAsync(() => {
    authServiceSpy.forgotPassword.and.returnValue(
      of({ message: 'If that address is registered, an email has been sent.' })
    );

    component.forgotForm.controls['email'].setValue('user@example.com');
    component.onSubmit();
    tick();

    expect(authServiceSpy.forgotPassword).toHaveBeenCalledWith('user@example.com');
  }));

  it('should show success message regardless of whether email exists', fakeAsync(() => {
    authServiceSpy.forgotPassword.and.returnValue(
      of({ message: 'If that address is registered, an email has been sent.' })
    );

    component.forgotForm.controls['email'].setValue('anyone@example.com');
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.submitted()).toBeTrue();
    const successEl = fixture.nativeElement.querySelector('[data-testid="forgot-success"]');
    expect(successEl).toBeTruthy();
  }));

  it('should show success even when server returns 404', fakeAsync(() => {
    // Security: we should not reveal whether an email exists
    authServiceSpy.forgotPassword.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 404 }))
    );

    component.forgotForm.controls['email'].setValue('nobody@example.com');
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.submitted()).toBeTrue();
  }));

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    expect(authServiceSpy.forgotPassword).not.toHaveBeenCalled();
  });

  it('should show loading state while request is in flight', fakeAsync(() => {
    const { Subject } = require('rxjs');
    const subject = new Subject<any>();
    authServiceSpy.forgotPassword.and.returnValue(subject.asObservable());

    component.forgotForm.controls['email'].setValue('user@example.com');
    component.onSubmit();

    expect(component.loading()).toBeTrue();

    subject.next({ message: 'ok' });
    subject.complete();
    tick();

    expect(component.loading()).toBeFalse();
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/forgot-password/forgot-password.component.spec.ts --watch=false
```
Expected: FAIL — `ForgotPasswordComponent` does not exist.

- [ ] **Step 3: Implement ForgotPasswordComponent**

```typescript
// src/app/auth/forgot-password/forgot-password.component.ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
  forgotForm: FormGroup;
  loading = signal(false);
  submitted = signal(false);

  constructor(private fb: FormBuilder, private authService: AuthService) {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  onSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const { email } = this.forgotForm.value;

    this.authService
      .forgotPassword(email)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        // Always show success to prevent email enumeration
        next: () => this.submitted.set(true),
        error: () => this.submitted.set(true),
      });
  }
}
```

```html
<!-- src/app/auth/forgot-password/forgot-password.component.html -->
<div class="forgot-container">
  @if (submitted()) {
    <div class="success-card" data-testid="forgot-success" role="status">
      <h2>Check your email</h2>
      <p>If that email address is registered with us, you'll receive a password reset link shortly.</p>
      <a mat-button routerLink="/login">Back to Sign In</a>
    </div>
  } @else {
    <mat-card>
      <mat-card-header>
        <mat-card-title>Forgot Password</mat-card-title>
        <mat-card-subtitle>Enter your email and we'll send you a reset link.</mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        <form [formGroup]="forgotForm" (ngSubmit)="onSubmit()" novalidate>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            @if (forgotForm.controls['email'].hasError('required') && forgotForm.controls['email'].touched) {
              <mat-error>Email is required.</mat-error>
            }
            @if (forgotForm.controls['email'].hasError('email') && forgotForm.controls['email'].touched) {
              <mat-error>Enter a valid email address.</mat-error>
            }
          </mat-form-field>

          <button
            mat-raised-button
            color="primary"
            type="submit"
            class="full-width"
            [disabled]="loading()"
          >
            @if (loading()) {
              <mat-spinner diameter="20"></mat-spinner>
            } @else {
              Send Reset Link
            }
          </button>

          <div class="back-link">
            <a routerLink="/login">Back to Sign In</a>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/forgot-password/forgot-password.component.spec.ts --watch=false
```
Expected: 8 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/forgot-password/
git commit -m "feat(auth): implement ForgotPasswordComponent with security-safe success-always behavior"
```

---

## Task 3: ResetPasswordComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/reset-password/reset-password.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from '../auth.service';
import { Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  function createComponent(token: string = 'valid-token') {
    const activatedRouteStub = {
      snapshot: { queryParamMap: { get: (key: string) => (key === 'token' ? token : null) } },
    };

    TestBed.configureTestingModule({
      imports: [
        ResetPasswordComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ],
    });

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['resetPassword']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
  });

  it('should create', () => {
    createComponent();
    expect(component).toBeTruthy();
  });

  it('should read token from query params on init', () => {
    createComponent('my-reset-token');
    expect(component.token()).toBe('my-reset-token');
  });

  it('should be invalid when passwords are empty', () => {
    createComponent();
    expect(component.resetForm.valid).toBeFalse();
  });

  it('should enforce minimum 8 character password', () => {
    createComponent();
    component.resetForm.controls['password'].setValue('short');
    expect(component.resetForm.controls['password'].valid).toBeFalse();
  });

  it('should fail validation when passwords do not match', () => {
    createComponent();
    component.resetForm.setValue({ password: 'Password1!', confirmPassword: 'Different1!' });
    expect(component.resetForm.hasError('passwordMismatch')).toBeTrue();
  });

  it('should be valid when passwords match and meet requirements', () => {
    createComponent();
    component.resetForm.setValue({ password: 'Password1!', confirmPassword: 'Password1!' });
    expect(component.resetForm.valid).toBeTrue();
  });

  it('should call resetPassword with token and new password on valid submit', fakeAsync(() => {
    createComponent('valid-token');
    authServiceSpy.resetPassword.and.returnValue(of({ message: 'Password reset successfully.' }));

    component.resetForm.setValue({ password: 'NewPassword1!', confirmPassword: 'NewPassword1!' });
    component.onSubmit();
    tick();

    expect(authServiceSpy.resetPassword).toHaveBeenCalledWith('valid-token', 'NewPassword1!', 'NewPassword1!');
  }));

  it('should show success message after successful reset', fakeAsync(() => {
    createComponent('valid-token');
    authServiceSpy.resetPassword.and.returnValue(of({ message: 'Password reset successfully.' }));

    component.resetForm.setValue({ password: 'NewPassword1!', confirmPassword: 'NewPassword1!' });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.successMessage()).toContain('reset');
    const el = fixture.nativeElement.querySelector('[data-testid="reset-success"]');
    expect(el).toBeTruthy();
  }));

  it('should set errorCode to TOKEN_EXPIRED on 400 TOKEN_EXPIRED', fakeAsync(() => {
    createComponent('expired-token');
    authServiceSpy.resetPassword.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { code: 'TOKEN_EXPIRED' } }))
    );

    component.resetForm.setValue({ password: 'Password1!', confirmPassword: 'Password1!' });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('TOKEN_EXPIRED');
    const el = fixture.nativeElement.querySelector('[data-testid="token-error"]');
    expect(el).toBeTruthy();
  }));

  it('should compute password strength as weak for short passwords', () => {
    createComponent();
    expect(component.getPasswordStrength('abc')).toBe('weak');
  });

  it('should compute password strength as medium for 8+ chars no special', () => {
    createComponent();
    expect(component.getPasswordStrength('Password1')).toBe('medium');
  });

  it('should compute password strength as strong for complex passwords', () => {
    createComponent();
    expect(component.getPasswordStrength('P@ssword1!')).toBe('strong');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/reset-password/reset-password.component.spec.ts --watch=false
```
Expected: FAIL — `ResetPasswordComponent` does not exist.

- [ ] **Step 3: Implement ResetPasswordComponent**

```typescript
// src/app/auth/reset-password/reset-password.component.ts
import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth.service';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const pw = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return pw && confirm && pw !== confirm ? { passwordMismatch: true } : null;
}

export type PasswordStrength = 'weak' | 'medium' | 'strong';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './reset-password.component.html',
})
export class ResetPasswordComponent implements OnInit {
  resetForm: FormGroup;
  token = signal<string | null>(null);
  loading = signal(false);
  errorCode = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  hidePassword = signal(true);
  hideConfirm = signal(true);
  passwordStrength = signal<PasswordStrength>('weak');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.resetForm = this.fb.group(
      {
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: passwordMatchValidator }
    );

    this.resetForm.controls['password'].valueChanges.subscribe((val: string) => {
      this.passwordStrength.set(this.getPasswordStrength(val));
    });
  }

  ngOnInit(): void {
    const tok = this.route.snapshot.queryParamMap.get('token');
    this.token.set(tok);
  }

  getPasswordStrength(password: string): PasswordStrength {
    if (!password || password.length < 6) return 'weak';
    const hasUpper = /[A-Z]/.test(password);
    const hasDigit = /\d/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password);
    const longEnough = password.length >= 8;

    if (longEnough && hasUpper && hasDigit && hasSpecial) return 'strong';
    if (longEnough && (hasUpper || hasDigit)) return 'medium';
    return 'weak';
  }

  onSubmit(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    const tok = this.token();
    if (!tok) {
      this.errorCode.set('MISSING_TOKEN');
      return;
    }

    this.errorCode.set(null);
    this.loading.set(true);
    const { password, confirmPassword } = this.resetForm.value;

    this.authService
      .resetPassword(tok, password, confirmPassword)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: res => this.successMessage.set(res.message ?? 'Password reset successfully.'),
        error: (err: HttpErrorResponse) => {
          this.errorCode.set(err.error?.code ?? 'SERVER_ERROR');
        },
      });
  }
}
```

```html
<!-- src/app/auth/reset-password/reset-password.component.html -->
<div class="reset-container">
  @if (successMessage()) {
    <div class="success-card" data-testid="reset-success" role="status">
      <h2>Password Reset</h2>
      <p>{{ successMessage() }}</p>
      <a mat-raised-button color="primary" routerLink="/login">Log in now</a>
    </div>
  } @else {
    <mat-card>
      <mat-card-header>
        <mat-card-title>Set New Password</mat-card-title>
      </mat-card-header>

      <mat-card-content>
        @if (errorCode() === 'TOKEN_EXPIRED' || errorCode() === 'INVALID_TOKEN') {
          <div class="error-banner" data-testid="token-error" role="alert">
            This reset link has expired or is invalid.
            <a mat-button routerLink="/forgot-password">Request a new link</a>
          </div>
        }

        <form [formGroup]="resetForm" (ngSubmit)="onSubmit()" novalidate>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>New Password</mat-label>
            <input
              matInput
              [type]="hidePassword() ? 'password' : 'text'"
              formControlName="password"
              autocomplete="new-password"
            />
            <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
              <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            @if (resetForm.controls['password'].hasError('required') && resetForm.controls['password'].touched) {
              <mat-error>Password is required.</mat-error>
            }
            @if (resetForm.controls['password'].hasError('minlength') && resetForm.controls['password'].touched) {
              <mat-error>Password must be at least 8 characters.</mat-error>
            }
          </mat-form-field>

          <!-- Password strength indicator -->
          @if (resetForm.controls['password'].value) {
            <div class="strength-bar" data-testid="strength-indicator">
              <div class="strength-label">Strength: <strong>{{ passwordStrength() }}</strong></div>
              <div class="strength-track">
                <div
                  class="strength-fill"
                  [class.weak]="passwordStrength() === 'weak'"
                  [class.medium]="passwordStrength() === 'medium'"
                  [class.strong]="passwordStrength() === 'strong'"
                ></div>
              </div>
            </div>
          }

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Confirm New Password</mat-label>
            <input
              matInput
              [type]="hideConfirm() ? 'password' : 'text'"
              formControlName="confirmPassword"
              autocomplete="new-password"
            />
            <button mat-icon-button matSuffix type="button" (click)="hideConfirm.set(!hideConfirm())">
              <mat-icon>{{ hideConfirm() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            @if (resetForm.hasError('passwordMismatch') && resetForm.controls['confirmPassword'].touched) {
              <mat-error>Passwords do not match.</mat-error>
            }
          </mat-form-field>

          <button
            mat-raised-button
            color="primary"
            type="submit"
            class="full-width"
            [disabled]="loading() || !!(errorCode() === 'TOKEN_EXPIRED' || errorCode() === 'INVALID_TOKEN')"
          >
            @if (loading()) {
              <mat-spinner diameter="20"></mat-spinner>
            } @else {
              Reset Password
            }
          </button>
        </form>
      </mat-card-content>
    </mat-card>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/reset-password/reset-password.component.spec.ts --watch=false
```
Expected: 11 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/reset-password/
git commit -m "feat(auth): implement ResetPasswordComponent with password strength indicator and token error handling"
```
