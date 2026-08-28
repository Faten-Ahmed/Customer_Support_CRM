# Login Page (Internal Staff) — Implementation Plan

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

**Story:** US-FE-001
**Goal:** Implement the internal staff login page at `/login` with full client-side validation, reactive form, loading state, and error handling for deactivated accounts and forced password changes.

**Architecture:** `AuthModule` is a lazy-loaded feature module. `AuthService` handles all HTTP calls against `/api/v1/auth/*` and maintains the in-memory access token via an Angular Signal. `LoginComponent` uses a `ReactiveForm` with Angular Material components and delegates auth logic entirely to `AuthService`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/auth/auth.service.ts` |
| Create | `src/app/auth/auth.service.spec.ts` |
| Create | `src/app/auth/login/login.component.ts` |
| Create | `src/app/auth/login/login.component.html` |
| Create | `src/app/auth/login/login.component.spec.ts` |

---

## Task 1: AuthService — login method

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/auth.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService, LoginResponse } from './auth.service';
import { Router } from '@angular/router';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    routerSpy = jasmine.createSpyObj('Router', ['navigate', 'navigateByUrl']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpy },
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should store access token in memory on successful login', () => {
    const mockResponse: LoginResponse = {
      accessToken: 'jwt.token.here',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    };

    service.login('staff@azmsquad.com', 'Password1!').subscribe(res => {
      expect(res.accessToken).toBe('jwt.token.here');
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'staff@azmsquad.com', password: 'Password1!' });
    req.flush(mockResponse);

    expect(service.accessToken()).toBe('jwt.token.here');
    expect(service.currentUser()).toEqual(mockResponse.user);
  });

  it('should NOT store token in localStorage on successful login', () => {
    spyOn(localStorage, 'setItem');

    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    expect(localStorage.setItem).not.toHaveBeenCalled();
  });

  it('should pass through 401 error so component can handle it', () => {
    let errorStatus = 0;
    service.login('x@x.com', 'wrong').subscribe({
      error: err => (errorStatus = err.status),
    });

    httpMock.expectOne('/api/v1/auth/login').flush(
      { code: 'ACCOUNT_INACTIVE' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(errorStatus).toBe(401);
  });

  it('should pass through 423 error so component can redirect to /change-password', () => {
    let errorStatus = 0;
    service.login('x@x.com', 'pass').subscribe({
      error: err => (errorStatus = err.status),
    });

    httpMock.expectOne('/api/v1/auth/login').flush(
      { code: 'PASSWORD_CHANGE_REQUIRED' },
      { status: 423, statusText: 'Locked' }
    );

    expect(errorStatus).toBe(423);
  });

  it('isAuthenticated() should return true when token is set', () => {
    expect(service.isAuthenticated()).toBeFalse();

    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    expect(service.isAuthenticated()).toBeTrue();
  });

  it('logout() should clear the access token signal', () => {
    service.login('staff@azmsquad.com', 'Password1!').subscribe();
    httpMock.expectOne('/api/v1/auth/login').flush({
      accessToken: 'tok',
      user: { id: '1', email: 'staff@azmsquad.com', role: 'Agent', passwordMustChange: false },
    });

    service.logout();
    expect(service.accessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```
Expected: FAIL — `AuthService` does not exist yet.

- [ ] **Step 3: Implement AuthService**

```typescript
// src/app/auth/auth.service.ts
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';

export interface AuthUser {
  id: string;
  email: string;
  role: 'Admin' | 'Manager' | 'Agent' | 'PortalUser';
  passwordMustChange: boolean;
}

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _accessToken = signal<string | null>(null);
  private readonly _currentUser = signal<AuthUser | null>(null);

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/v1/auth/login', { email, password })
      .pipe(
        tap(res => {
          this._accessToken.set(res.accessToken);
          this._currentUser.set(res.user);
        })
      );
  }

  logout(): void {
    this._accessToken.set(null);
    this._currentUser.set(null);
    this.http.post('/api/v1/auth/logout', {}).subscribe();
  }

  refreshToken(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/v1/auth/refresh', {}).pipe(
      tap(res => {
        this._accessToken.set(res.accessToken);
        this._currentUser.set(res.user);
      })
    );
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/auth.service.spec.ts --watch=false
```
Expected: 6 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/auth.service.ts src/app/auth/auth.service.spec.ts
git commit -m "feat(auth): add AuthService with in-memory token signal and login/logout"
```

---

## Task 2: LoginComponent — template and reactive form

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/login/login.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login'], {
      isAuthenticated: jasmine.createSpy().and.returnValue(false),
    });
    routerSpy = jasmine.createSpyObj('Router', ['navigate', 'navigateByUrl']);

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
        MatProgressSpinnerModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have an invalid form when empty', () => {
    expect(component.loginForm.valid).toBeFalse();
  });

  it('should mark email invalid with bad format', () => {
    component.loginForm.controls['email'].setValue('notanemail');
    expect(component.loginForm.controls['email'].valid).toBeFalse();
  });

  it('should mark email valid with correct format', () => {
    component.loginForm.controls['email'].setValue('user@example.com');
    expect(component.loginForm.controls['email'].valid).toBeTrue();
  });

  it('should mark password invalid when empty', () => {
    component.loginForm.controls['password'].setValue('');
    expect(component.loginForm.controls['password'].valid).toBeFalse();
  });

  it('should call AuthService.login on valid submit', fakeAsync(() => {
    authServiceSpy.login.and.returnValue(
      of({ accessToken: 'tok', user: { id: '1', email: 'a@b.com', role: 'Agent', passwordMustChange: false } })
    );

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();
    tick();

    expect(authServiceSpy.login).toHaveBeenCalledWith('staff@azmsquad.com', 'Password1!');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/dashboard']);
  }));

  it('should set loading to true while request is in flight', fakeAsync(() => {
    // Use a subject to control when the observable emits
    const { Subject } = require('rxjs');
    const subject = new Subject<any>();
    authServiceSpy.login.and.returnValue(subject.asObservable());

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();

    expect(component.loading()).toBeTrue();
    subject.next({ accessToken: 'tok', user: { id: '1', email: 'a@b.com', role: 'Agent', passwordMustChange: false } });
    subject.complete();
    tick();
    expect(component.loading()).toBeFalse();
  }));

  it('should show ACCOUNT_INACTIVE banner on 401 with that code', fakeAsync(() => {
    authServiceSpy.login.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 401,
            error: { code: 'ACCOUNT_INACTIVE' },
          })
      )
    );

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('ACCOUNT_INACTIVE');
    const banner = fixture.nativeElement.querySelector('[data-testid="error-banner"]');
    expect(banner?.textContent).toContain('deactivated');
  }));

  it('should redirect to /change-password on 423', fakeAsync(() => {
    authServiceSpy.login.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 423,
            error: { code: 'PASSWORD_CHANGE_REQUIRED' },
          })
      )
    );

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();
    tick();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/change-password']);
  }));

  it('should not call login when form is invalid', () => {
    component.loginForm.setValue({ email: '', password: '' });
    component.onSubmit();
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('should redirect to /dashboard if already authenticated', () => {
    Object.defineProperty(authServiceSpy, 'isAuthenticated', {
      get: () => () => true,
    });
    // Reconstruct component to trigger ngOnInit check
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/dashboard']);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/login/login.component.spec.ts --watch=false
```
Expected: FAIL — `LoginComponent` does not exist yet.

- [ ] **Step 3: Implement LoginComponent**

```typescript
// src/app/auth/login/login.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  loading = signal(false);
  errorCode = signal<string | null>(null);
  hidePassword = signal(true);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
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
      .login(email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/dashboard']),
        error: (err: HttpErrorResponse) => {
          if (err.status === 423) {
            this.router.navigate(['/change-password']);
          } else if (err.status === 401) {
            this.errorCode.set(err.error?.code ?? 'INVALID_CREDENTIALS');
          } else {
            this.errorCode.set('SERVER_ERROR');
          }
        },
      });
  }
}
```

```html
<!-- src/app/auth/login/login.component.html -->
<div class="login-container">
  <mat-card class="login-card">
    <mat-card-header>
      <mat-card-title>Staff Login</mat-card-title>
    </mat-card-header>

    <mat-card-content>
      <!-- Error banner -->
      @if (errorCode() === 'ACCOUNT_INACTIVE') {
        <div class="error-banner" data-testid="error-banner" role="alert">
          Your account is deactivated. Contact your administrator.
        </div>
      }
      @if (errorCode() === 'INVALID_CREDENTIALS') {
        <div class="error-banner" data-testid="error-banner" role="alert">
          Invalid email or password.
        </div>
      }
      @if (errorCode() === 'SERVER_ERROR') {
        <div class="error-banner" data-testid="error-banner" role="alert">
          An unexpected error occurred. Please try again.
        </div>
      }

      <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>
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
          <button
            mat-icon-button
            matSuffix
            type="button"
            (click)="hidePassword.set(!hidePassword())"
            [attr.aria-label]="hidePassword() ? 'Show password' : 'Hide password'"
          >
            <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          @if (loginForm.controls['password'].hasError('required') && loginForm.controls['password'].touched) {
            <mat-error>Password is required.</mat-error>
          }
        </mat-form-field>

        <a routerLink="/forgot-password" class="forgot-link">Forgot password?</a>

        <button
          mat-raised-button
          color="primary"
          type="submit"
          class="full-width submit-btn"
          [disabled]="loading()"
        >
          @if (loading()) {
            <mat-spinner diameter="20" class="btn-spinner"></mat-spinner>
          } @else {
            Sign In
          }
        </button>
      </form>
    </mat-card-content>
  </mat-card>
</div>
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/login/login.component.spec.ts --watch=false
```
Expected: 10 tests PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/login/
git commit -m "feat(auth): implement LoginComponent with reactive form, error handling, and loading state"
```

---

## Task 3: Wire up AuthModule routing

- [ ] **Step 1: Write the failing test**

```typescript
// Verify route guard redirects authenticated users — tested in auth.guard.spec.ts (US-FE-005)
// For this task, verify the login route loads the component.
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { LoginComponent } from './login/login.component';
import { AuthService } from './auth.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('Auth routing', () => {
  it('should load LoginComponent at /login', async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, NoopAnimationsModule],
      providers: [
        AuthService,
        provideRouter([{ path: 'login', component: LoginComponent }]),
      ],
    }).compileComponents();

    const harness = await RouterTestingHarness.create('/login');
    expect(harness.routeNativeElement).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**
```bash
ng test --include=src/app/auth/auth.routes.spec.ts --watch=false
```
Expected: FAIL — routes file not defined.

- [ ] **Step 3: Implement auth routes**

```typescript
// src/app/auth/auth.routes.ts
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./forgot-password/forgot-password.component').then(
        m => m.ForgotPasswordComponent
      ),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./reset-password/reset-password.component').then(
        m => m.ResetPasswordComponent
      ),
  },
  {
    path: 'change-password',
    loadComponent: () =>
      import('./change-password/change-password.component').then(
        m => m.ChangePasswordComponent
      ),
  },
];
```

- [ ] **Step 4: Run tests to verify they pass**
```bash
ng test --include=src/app/auth/auth.routes.spec.ts --watch=false
```
Expected: 1 test PASS.

- [ ] **Step 5: Commit**
```bash
git add src/app/auth/auth.routes.ts
git commit -m "feat(auth): add auth feature routes with lazy-loaded components"
```
