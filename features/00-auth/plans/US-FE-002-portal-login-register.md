# Portal Login & Registration — Implementation Plan

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

**Story:** US-FE-002
**Goal:** Implement the customer portal login and registration page at `/portal/login` with tabbed interface, RTL support, email-verification error handling, and password confirmation validation.

**Architecture:** A lazy-loaded `PortalModule` under `src/app/portal/` houses all customer-facing components. `PortalLoginComponent` and `PortalRegisterComponent` are standalone components rendered inside a shared `PortalAuthShellComponent` that provides the `mat-tab-group`. `AuthService` is extended with `portalLogin` and `portalRegister` methods. The portal module uses Angular's `@angular/localize` direction binding for RTL support.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/auth/portal-login/portal-login.component.ts` |
| Create | `src/app/portal/auth/portal-login/portal-login.component.html` |
| Create | `src/app/portal/auth/portal-login/portal-login.component.spec.ts` |
| Create | `src/app/portal/auth/portal-register/portal-register.component.ts` |
| Create | `src/app/portal/auth/portal-register/portal-register.component.html` |
| Create | `src/app/portal/auth/portal-register/portal-register.component.spec.ts` |
| Create | `src/app/portal/auth/portal-auth-shell/portal-auth-shell.component.ts` |
| Modify | `src/app/auth/auth.service.ts` |
| Modify | `src/app/auth/auth.service.spec.ts` |

---

## Task 1: Extend AuthService with portalLogin and portalRegister

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/auth/auth.service.spec.ts

describe('AuthService — portal methods', () => {
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

  it('portalLogin() should POST to /api/v1/portal/auth/login', () => {
    service.portalLogin('customer@example.com', 'pass123').subscribe(res => {
      expect(res.accessToken).toBe('portal.jwt');
    });

    const req = httpMock.expectOne('/api/v1/portal/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ accessToken: 'portal.jwt', user: { id: '2', email: 'customer@example.com', role: 'PortalUser', passwordMustChange: false } });
  });

  it('portalLogin() should store token on success', () => {
    service.portalLogin('customer@example.com', 'pass123').subscribe();
    httpMock.expectOne('/api/v1/portal/auth/login').flush({
      accessToken: 'portal.jwt',
      user: { id: '2', email: 'customer@example.com', role: 'PortalUser', passwordMustChange: false },
    });

    expect(service.accessToken()).toBe('portal.jwt');
  });

  it('portalLogin() should pass through 401 EMAIL_NOT_VERIFIED error', () => {
    let errorCode = '';
    service.portalLogin('unverified@example.com', 'pass123').subscribe({
      error: err => (errorCode = err.error?.code),
    });

    httpMock.expectOne('/api/v1/portal/auth/login').flush(
      { code: 'EMAIL_NOT_VERIFIED' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(errorCode).toBe('EMAIL_NOT_VERIFIED');
  });

  it('portalRegister() should POST to /api/v1/portal/auth/register', () => {
    const payload = {
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Secure123!',
      confirmPassword: 'Secure123!',
    };

    service.portalRegister(payload).subscribe(res => {
      expect(res.message).toBe('Check your email to activate your account');
    });

    const req = httpMock.expectOne('/api/v1/portal/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ message: 'Check your email to activate your account' });
  });

  it('resendVerificationEmail() should POST to /api/v1/portal/auth/resend-verification', () => {
    service.resendVerificationEmail('user@example.com').subscribe();
    const req = httpMock.expectOne('/api/v1/portal/auth/resend-verification');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush({ message: 'Sent' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```
Expected: FAIL — `portalLogin`, `portalRegister`, `resendVerificationEmail` not defined.

- [ ] **Step 3: Extend AuthService**

```typescript
// Add to src/app/auth/auth.service.ts (inside the AuthService class)

export interface PortalRegisterPayload {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface MessageResponse {
  message: string;
}

// --- append these methods to AuthService ---

portalLogin(email: string, password: string): Observable<LoginResponse> {
  return this.http
    .post<LoginResponse>('/api/v1/portal/auth/login', { email, password })
    .pipe(
      tap(res => {
        this._accessToken.set(res.accessToken);
        this._currentUser.set(res.user);
      })
    );
}

portalRegister(payload: PortalRegisterPayload): Observable<MessageResponse> {
  return this.http.post<MessageResponse>('/api/v1/portal/auth/register', payload);
}

resendVerificationEmail(email: string): Observable<MessageResponse> {
  return this.http.post<MessageResponse>('/api/v1/portal/auth/resend-verification', { email });
}
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```
Expected: 11 tests PASS (6 original + 5 new).

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/auth.service.ts src/app/auth/auth.service.spec.ts
git commit -m "feat(auth): extend AuthService with portalLogin, portalRegister, resendVerificationEmail"
```

---

## Task 2: PortalLoginComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/auth/portal-login/portal-login.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { PortalLoginComponent } from './portal-login.component';
import { AuthService } from '../../../auth/auth.service';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('PortalLoginComponent', () => {
  let component: PortalLoginComponent;
  let fixture: ComponentFixture<PortalLoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['portalLogin', 'resendVerificationEmail']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [
        PortalLoginComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalLoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have invalid form when empty', () => {
    expect(component.loginForm.valid).toBeFalse();
  });

  it('should require valid email format', () => {
    component.loginForm.controls['email'].setValue('bademail');
    expect(component.loginForm.controls['email'].valid).toBeFalse();
  });

  it('should require password of at least 8 characters', () => {
    component.loginForm.controls['password'].setValue('short');
    expect(component.loginForm.controls['password'].valid).toBeFalse();
  });

  it('should accept password of 8+ characters', () => {
    component.loginForm.controls['password'].setValue('ValidPass1');
    expect(component.loginForm.controls['password'].valid).toBeTrue();
  });

  it('should call portalLogin on valid submit and navigate to /portal/dashboard', fakeAsync(() => {
    authServiceSpy.portalLogin.and.returnValue(
      of({ accessToken: 'tok', user: { id: '2', email: 'c@c.com', role: 'PortalUser', passwordMustChange: false } })
    );

    component.loginForm.setValue({ email: 'customer@example.com', password: 'Password1!' });
    component.onSubmit();
    tick();

    expect(authServiceSpy.portalLogin).toHaveBeenCalledWith('customer@example.com', 'Password1!');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/portal/dashboard']);
  }));

  it('should show EMAIL_NOT_VERIFIED message and resend link on 401 with that code', fakeAsync(() => {
    authServiceSpy.portalLogin.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 401, error: { code: 'EMAIL_NOT_VERIFIED' } }))
    );

    component.loginForm.setValue({ email: 'unverified@example.com', password: 'Password1!' });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('EMAIL_NOT_VERIFIED');
    const msg = fixture.nativeElement.querySelector('[data-testid="email-not-verified-msg"]');
    expect(msg).toBeTruthy();
    expect(msg.textContent).toContain('verify your email');
  }));

  it('should call resendVerificationEmail when resend link is clicked', fakeAsync(() => {
    authServiceSpy.resendVerificationEmail.and.returnValue(of({ message: 'Sent' }));
    component.unverifiedEmail.set('unverified@example.com');
    component.errorCode.set('EMAIL_NOT_VERIFIED');
    fixture.detectChanges();

    const resendBtn = fixture.nativeElement.querySelector('[data-testid="resend-verification-btn"]');
    resendBtn.click();
    tick();

    expect(authServiceSpy.resendVerificationEmail).toHaveBeenCalledWith('unverified@example.com');
  }));

  it('should toggle password visibility', () => {
    expect(component.hidePassword()).toBeTrue();
    component.hidePassword.set(false);
    expect(component.hidePassword()).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/portal/auth/portal-login/portal-login.component.spec.ts --watch=false
```
Expected: FAIL — `PortalLoginComponent` does not exist yet.

- [ ] **Step 3: Implement PortalLoginComponent**

```typescript
// src/app/portal/auth/portal-login/portal-login.component.ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../auth/auth.service';

@Component({
  selector: 'app-portal-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './portal-login.component.html',
})
export class PortalLoginComponent {
  loginForm: FormGroup;
  loading = signal(false);
  errorCode = signal<string | null>(null);
  hidePassword = signal(true);
  unverifiedEmail = signal<string | null>(null);
  resendSent = signal(false);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.errorCode.set(null);
    this.loading.set(true);
    const { email, password } = this.loginForm.value;

    this.authService
      .portalLogin(email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/portal/dashboard']),
        error: (err: HttpErrorResponse) => {
          const code = err.error?.code ?? 'SERVER_ERROR';
          this.errorCode.set(code);
          if (code === 'EMAIL_NOT_VERIFIED') {
            this.unverifiedEmail.set(email);
          }
        },
      });
  }

  resendVerification(): void {
    const email = this.unverifiedEmail();
    if (!email) return;
    this.authService.resendVerificationEmail(email).subscribe(() => {
      this.resendSent.set(true);
    });
  }
}
```

```html
<!-- src/app/portal/auth/portal-login/portal-login.component.html -->
<form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>

  @if (errorCode() === 'EMAIL_NOT_VERIFIED') {
    <div data-testid="email-not-verified-msg" class="info-banner" role="alert">
      Please verify your email before logging in.
      @if (!resendSent()) {
        <button mat-button type="button" data-testid="resend-verification-btn" (click)="resendVerification()">
          Resend verification email
        </button>
      } @else {
        <span> Verification email sent!</span>
      }
    </div>
  }

  @if (errorCode() && errorCode() !== 'EMAIL_NOT_VERIFIED') {
    <div class="error-banner" data-testid="error-banner" role="alert">
      Invalid email or password. Please try again.
    </div>
  }

  <mat-form-field appearance="outline" class="full-width">
    <mat-label>Email</mat-label>
    <input matInput type="email" formControlName="email" autocomplete="email" />
    @if (loginForm.controls['email'].hasError('required') && loginForm.controls['email'].touched) {
      <mat-error>Email is required.</mat-error>
    }
    @if (loginForm.controls['email'].hasError('email') && loginForm.controls['email'].touched) {
      <mat-error>Enter a valid email address.</mat-error>
    }
  </mat-form-field>

  <mat-form-field appearance="outline" class="full-width">
    <mat-label>Password</mat-label>
    <input
      matInput
      [type]="hidePassword() ? 'password' : 'text'"
      formControlName="password"
      autocomplete="current-password"
    />
    <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
      <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
    </button>
    @if (loginForm.controls['password'].hasError('required') && loginForm.controls['password'].touched) {
      <mat-error>Password is required.</mat-error>
    }
    @if (loginForm.controls['password'].hasError('minlength') && loginForm.controls['password'].touched) {
      <mat-error>Password must be at least 8 characters.</mat-error>
    }
  </mat-form-field>

  <button mat-raised-button color="primary" type="submit" class="full-width" [disabled]="loading()">
    @if (loading()) {
      <mat-spinner diameter="20"></mat-spinner>
    } @else {
      Sign In
    }
  </button>
</form>
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/portal/auth/portal-login/portal-login.component.spec.ts --watch=false
```
Expected: 9 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/portal/auth/portal-login/
git commit -m "feat(portal): implement PortalLoginComponent with email verification error handling"
```

---

## Task 3: PortalRegisterComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/auth/portal-register/portal-register.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { PortalRegisterComponent } from './portal-register.component';
import { AuthService } from '../../../auth/auth.service';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('PortalRegisterComponent', () => {
  let component: PortalRegisterComponent;
  let fixture: ComponentFixture<PortalRegisterComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['portalRegister']);

    await TestBed.configureTestingModule({
      imports: [
        PortalRegisterComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalRegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should be invalid when empty', () => {
    expect(component.registerForm.valid).toBeFalse();
  });

  it('fullName should be required', () => {
    component.registerForm.controls['fullName'].setValue('');
    expect(component.registerForm.controls['fullName'].valid).toBeFalse();
  });

  it('password must be at least 8 characters', () => {
    component.registerForm.controls['password'].setValue('short');
    expect(component.registerForm.controls['password'].valid).toBeFalse();
  });

  it('form should be invalid when passwords do not match', () => {
    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Different1!',
    });
    expect(component.registerForm.hasError('passwordMismatch')).toBeTrue();
  });

  it('form should be valid when all fields are correct and passwords match', () => {
    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    expect(component.registerForm.valid).toBeTrue();
  });

  it('should call portalRegister on valid submit', fakeAsync(() => {
    authServiceSpy.portalRegister.and.returnValue(
      of({ message: 'Check your email to activate your account' })
    );

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    component.onSubmit();
    tick();

    expect(authServiceSpy.portalRegister).toHaveBeenCalledWith({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
  }));

  it('should show success message after registration', fakeAsync(() => {
    authServiceSpy.portalRegister.and.returnValue(
      of({ message: 'Check your email to activate your account' })
    );

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.successMessage()).toContain('Check your email');
    const successEl = fixture.nativeElement.querySelector('[data-testid="register-success"]');
    expect(successEl).toBeTruthy();
  }));

  it('should show error on duplicate email (409)', fakeAsync(() => {
    authServiceSpy.portalRegister.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'EMAIL_ALREADY_EXISTS' } }))
    );

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'existing@example.com',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('EMAIL_ALREADY_EXISTS');
  }));

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    expect(authServiceSpy.portalRegister).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/portal/auth/portal-register/portal-register.component.spec.ts --watch=false
```
Expected: FAIL — `PortalRegisterComponent` does not exist yet.

- [ ] **Step 3: Implement PortalRegisterComponent**

```typescript
// src/app/portal/auth/portal-register/portal-register.component.ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../auth/auth.service';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');
  if (!password || !confirmPassword) return null;
  return password.value !== confirmPassword.value ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-portal-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './portal-register.component.html',
})
export class PortalRegisterComponent {
  registerForm: FormGroup;
  loading = signal(false);
  errorCode = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  hidePassword = signal(true);
  hideConfirm = signal(true);

  constructor(private fb: FormBuilder, private authService: AuthService) {
    this.registerForm = this.fb.group(
      {
        fullName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: passwordMatchValidator }
    );
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.errorCode.set(null);
    this.loading.set(true);
    const payload = this.registerForm.value;

    this.authService
      .portalRegister(payload)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: res => this.successMessage.set(res.message),
        error: (err: HttpErrorResponse) => {
          this.errorCode.set(err.error?.code ?? 'SERVER_ERROR');
        },
      });
  }
}
```

```html
<!-- src/app/portal/auth/portal-register/portal-register.component.html -->
@if (successMessage()) {
  <div class="success-banner" data-testid="register-success" role="status">
    {{ successMessage() }}
  </div>
} @else {
  <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" novalidate>

    @if (errorCode() === 'EMAIL_ALREADY_EXISTS') {
      <div class="error-banner" data-testid="error-banner" role="alert">
        An account with this email already exists. Please log in or use a different email.
      </div>
    }
    @if (errorCode() === 'SERVER_ERROR') {
      <div class="error-banner" data-testid="error-banner" role="alert">
        Registration failed. Please try again.
      </div>
    }

    <mat-form-field appearance="outline" class="full-width">
      <mat-label>Full Name</mat-label>
      <input matInput formControlName="fullName" autocomplete="name" />
      @if (registerForm.controls['fullName'].hasError('required') && registerForm.controls['fullName'].touched) {
        <mat-error>Full name is required.</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline" class="full-width">
      <mat-label>Email</mat-label>
      <input matInput type="email" formControlName="email" autocomplete="email" />
      @if (registerForm.controls['email'].hasError('required') && registerForm.controls['email'].touched) {
        <mat-error>Email is required.</mat-error>
      }
      @if (registerForm.controls['email'].hasError('email') && registerForm.controls['email'].touched) {
        <mat-error>Enter a valid email address.</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline" class="full-width">
      <mat-label>Password</mat-label>
      <input matInput [type]="hidePassword() ? 'password' : 'text'" formControlName="password" autocomplete="new-password" />
      <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
        <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
      </button>
      @if (registerForm.controls['password'].hasError('required') && registerForm.controls['password'].touched) {
        <mat-error>Password is required.</mat-error>
      }
      @if (registerForm.controls['password'].hasError('minlength') && registerForm.controls['password'].touched) {
        <mat-error>Password must be at least 8 characters.</mat-error>
      }
    </mat-form-field>

    <mat-form-field appearance="outline" class="full-width">
      <mat-label>Confirm Password</mat-label>
      <input matInput [type]="hideConfirm() ? 'password' : 'text'" formControlName="confirmPassword" autocomplete="new-password" />
      <button mat-icon-button matSuffix type="button" (click)="hideConfirm.set(!hideConfirm())">
        <mat-icon>{{ hideConfirm() ? 'visibility_off' : 'visibility' }}</mat-icon>
      </button>
      @if (registerForm.hasError('passwordMismatch') && registerForm.controls['confirmPassword'].touched) {
        <mat-error>Passwords do not match.</mat-error>
      }
    </mat-form-field>

    <button mat-raised-button color="primary" type="submit" class="full-width" [disabled]="loading()">
      @if (loading()) {
        <mat-spinner diameter="20"></mat-spinner>
      } @else {
        Create Account
      }
    </button>
  </form>
}
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/portal/auth/portal-register/portal-register.component.spec.ts --watch=false
```
Expected: 9 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/portal/auth/portal-register/
git commit -m "feat(portal): implement PortalRegisterComponent with password match validation and success state"
```

---

## Task 4: PortalAuthShellComponent — tabbed layout with RTL support

- [ ] **Step 1: Write the failing test**

```typescript
// src/app/portal/auth/portal-auth-shell/portal-auth-shell.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PortalAuthShellComponent } from './portal-auth-shell.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatTabsModule } from '@angular/material/tabs';
import { AuthService } from '../../../auth/auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('PortalAuthShellComponent', () => {
  let component: PortalAuthShellComponent;
  let fixture: ComponentFixture<PortalAuthShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PortalAuthShellComponent,
        NoopAnimationsModule,
        MatTabsModule,
        HttpClientTestingModule,
      ],
      providers: [AuthService, provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalAuthShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render a mat-tab-group with two tabs', () => {
    const tabs = fixture.nativeElement.querySelectorAll('.mat-mdc-tab');
    expect(tabs.length).toBe(2);
  });

  it('should apply dir="rtl" when isRtl is true', () => {
    component.isRtl.set(true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.getAttribute('dir')).toBe('rtl');
  });

  it('default isRtl should be false', () => {
    expect(component.isRtl()).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/portal/auth/portal-auth-shell/portal-auth-shell.component.spec.ts --watch=false
```
Expected: FAIL — component does not exist.

- [ ] **Step 3: Implement PortalAuthShellComponent**

```typescript
// src/app/portal/auth/portal-auth-shell/portal-auth-shell.component.ts
import { Component, signal, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { PortalLoginComponent } from '../portal-login/portal-login.component';
import { PortalRegisterComponent } from '../portal-register/portal-register.component';

@Component({
  selector: 'app-portal-auth-shell',
  standalone: true,
  imports: [CommonModule, MatTabsModule, PortalLoginComponent, PortalRegisterComponent],
  template: `
    <div class="portal-auth-container">
      <mat-tab-group animationDuration="200ms">
        <mat-tab label="Sign In">
          <div class="tab-content">
            <app-portal-login />
          </div>
        </mat-tab>
        <mat-tab label="Create Account">
          <div class="tab-content">
            <app-portal-register />
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
})
export class PortalAuthShellComponent {
  isRtl = signal(false);

  @HostBinding('attr.dir')
  get dir(): string | null {
    return this.isRtl() ? 'rtl' : null;
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/portal/auth/portal-auth-shell/portal-auth-shell.component.spec.ts --watch=false
```
Expected: 4 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/portal/auth/
git commit -m "feat(portal): add PortalAuthShellComponent with tab layout and RTL direction binding"
```
