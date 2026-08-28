# Auth Guards & HTTP Interceptor — Implementation Plan

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

**Story:** US-FE-005
**Goal:** Protect all internal and portal routes with `AuthGuard`, `RoleGuard`, and `PasswordChangeGuard`, and attach Bearer tokens via an HTTP interceptor that handles silent refresh on 401 and redirects on `423 PASSWORD_CHANGE_REQUIRED`.

**Architecture:** Angular Signal-based `AuthStore` holds the decoded JWT payload. Guards read directly from the store. The `AuthInterceptor` clones every outgoing request to add the Authorization header; on 401 it attempts a silent refresh via `AuthService.refresh()` and retries the original request once, logging out on failure. On 423 it stores the attempted URL and navigates to `/change-password`.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/auth/auth.store.ts` |
| Create | `src/app/auth/auth.store.spec.ts` |
| Create | `src/app/auth/guards/auth.guard.ts` |
| Create | `src/app/auth/guards/auth.guard.spec.ts` |
| Create | `src/app/auth/guards/role.guard.ts` |
| Create | `src/app/auth/guards/role.guard.spec.ts` |
| Create | `src/app/auth/interceptors/auth.interceptor.ts` |
| Create | `src/app/auth/interceptors/auth.interceptor.spec.ts` |
| Modify | `src/app/app.config.ts` |

---

## Task 1: AuthStore (Signal-based JWT state)

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/auth.store.spec.ts

import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  let store: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [AuthStore] });
    store = TestBed.inject(AuthStore);
    localStorage.clear();
  });

  it('should be unauthenticated initially when localStorage is empty', () => {
    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
  });

  it('setToken() should decode JWT and expose user claims', () => {
    // Minimal JWT with payload { sub:"1", role:"Agent", passwordMustChange:false }
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', role: 'Agent', passwordMustChange: false }));
    const fakeJwt = `${header}.${payload}.sig`;

    store.setToken(fakeJwt);
    expect(store.isAuthenticated()).toBeTrue();
    expect(store.user()?.role).toBe('Agent');
    expect(store.passwordMustChange()).toBeFalse();
  });

  it('clearToken() should reset state', () => {
    store.setToken('a.eyJzdWIiOiIxIn0.s');
    store.clearToken();
    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/auth.store.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/auth/auth.store.ts

import { Injectable, signal, computed } from '@angular/core';

export interface JwtUser {
  sub: string;
  role: 'Admin' | 'Manager' | 'Agent' | 'Customer';
  passwordMustChange: boolean;
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _token = signal<string | null>(localStorage.getItem('access_token'));
  private readonly _user = signal<JwtUser | null>(this._decodeToken(localStorage.getItem('access_token')));

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly passwordMustChange = computed(() => this._user()?.passwordMustChange ?? false);

  setToken(token: string): void {
    localStorage.setItem('access_token', token);
    this._token.set(token);
    this._user.set(this._decodeToken(token));
  }

  clearToken(): void {
    localStorage.removeItem('access_token');
    this._token.set(null);
    this._user.set(null);
  }

  getToken(): string | null {
    return this._token();
  }

  private _decodeToken(token: string | null): JwtUser | null {
    if (!token) return null;
    try {
      const payload = token.split('.')[1];
      return JSON.parse(atob(payload)) as JwtUser;
    } catch {
      return null;
    }
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/auth.store.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/auth.store.ts src/app/auth/auth.store.spec.ts
git commit -m "feat(auth): add AuthStore with Signal-based JWT state (US-FE-005)"
```

---

## Task 2: AuthGuard

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/guards/auth.guard.spec.ts

import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthGuard } from './auth.guard';
import { AuthStore } from '../auth.store';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let router: Router;
  let authStore: { isAuthenticated: jasmine.Spy; user: jasmine.Spy };

  beforeEach(() => {
    authStore = {
      isAuthenticated: jasmine.createSpy('isAuthenticated').and.returnValue(false),
      user: jasmine.createSpy('user').and.returnValue(null),
    };

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [
        AuthGuard,
        { provide: AuthStore, useValue: authStore },
      ],
    });

    guard = TestBed.inject(AuthGuard);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  it('should block unauthenticated users and redirect to /login', () => {
    authStore.isAuthenticated.and.returnValue(false);
    const result = guard.canActivate();
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should allow authenticated users', () => {
    authStore.isAuthenticated.and.returnValue(true);
    authStore.user.and.returnValue({ role: 'Agent', passwordMustChange: false } as any);
    expect(guard.canActivate()).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/guards/auth.guard.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/auth/guards/auth.guard.ts

import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../auth.store';

@Injectable({ providedIn: 'root' })
export class AuthGuard {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  canActivate(): boolean {
    if (!this.authStore.isAuthenticated()) {
      this.router.navigate(['/login']);
      return false;
    }
    return true;
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/guards/auth.guard.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/guards/auth.guard.ts src/app/auth/guards/auth.guard.spec.ts
git commit -m "feat(auth): add AuthGuard (US-FE-005)"
```

---

## Task 3: RoleGuard

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/guards/role.guard.spec.ts

import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { RoleGuard } from './role.guard';
import { AuthStore } from '../auth.store';

describe('RoleGuard', () => {
  let guard: RoleGuard;
  let router: Router;
  let authStore: { user: jasmine.Spy };

  const makeRoute = (roles: string[]) => {
    const snap = new ActivatedRouteSnapshot();
    (snap as any).data = { roles };
    return snap;
  };

  beforeEach(() => {
    authStore = { user: jasmine.createSpy('user') };

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [RoleGuard, { provide: AuthStore, useValue: authStore }],
    });

    guard = TestBed.inject(RoleGuard);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  it('should allow when user role is in the route roles list', () => {
    authStore.user.and.returnValue({ role: 'Admin' });
    expect(guard.canActivate(makeRoute(['Admin', 'Manager']))).toBeTrue();
  });

  it('should block and navigate to /403 when role not permitted', () => {
    authStore.user.and.returnValue({ role: 'Agent' });
    const result = guard.canActivate(makeRoute(['Admin']));
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/403']);
  });

  it('should block when no user', () => {
    authStore.user.and.returnValue(null);
    expect(guard.canActivate(makeRoute(['Admin']))).toBeFalse();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/guards/role.guard.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/auth/guards/role.guard.ts

import { Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthStore } from '../auth.store';

@Injectable({ providedIn: 'root' })
export class RoleGuard {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const user = this.authStore.user();
    const allowedRoles: string[] = route.data['roles'] ?? [];
    if (!user || !allowedRoles.includes(user.role)) {
      this.router.navigate(['/403']);
      return false;
    }
    return true;
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/guards/role.guard.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/guards/role.guard.ts src/app/auth/guards/role.guard.spec.ts
git commit -m "feat(auth): add RoleGuard (US-FE-005)"
```

---

## Task 4: AuthInterceptor

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/auth/interceptors/auth.interceptor.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClient, HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { AuthInterceptor } from './auth.interceptor';
import { AuthStore } from '../auth.store';
import { AuthService } from '../auth.service';

describe('AuthInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authStore: jasmine.SpyObj<AuthStore>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    authStore = jasmine.createSpyObj('AuthStore', ['getToken', 'clearToken', 'setToken']);
    authService = jasmine.createSpyObj('AuthService', ['refresh']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: AuthStore, useValue: authStore },
        { provide: AuthService, useValue: authService },
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  afterEach(() => httpMock.verify());

  it('should attach Authorization header when token exists', () => {
    authStore.getToken.and.returnValue('fake-jwt');

    http.get('/api/v1/tickets').subscribe();

    const req = httpMock.expectOne('/api/v1/tickets');
    expect(req.request.headers.get('Authorization')).toBe('Bearer fake-jwt');
    req.flush([]);
  });

  it('should not attach Authorization header when no token', () => {
    authStore.getToken.and.returnValue(null);

    http.get('/api/v1/tickets').subscribe();

    const req = httpMock.expectOne('/api/v1/tickets');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('should attempt refresh on 401 and retry', () => {
    authStore.getToken.and.returnValue('expired-jwt');
    authService.refresh.and.returnValue(of({ accessToken: 'new-jwt' }));

    http.get('/api/v1/tickets').subscribe();

    const firstReq = httpMock.expectOne('/api/v1/tickets');
    firstReq.flush({}, { status: 401, statusText: 'Unauthorized' });

    const retryReq = httpMock.expectOne('/api/v1/tickets');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-jwt');
    retryReq.flush([]);
  });

  it('should navigate to /login on 423 PASSWORD_CHANGE_REQUIRED', () => {
    authStore.getToken.and.returnValue('fake-jwt');

    http.get('/api/v1/tickets').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/v1/tickets');
    req.flush({ code: 'PASSWORD_CHANGE_REQUIRED' }, { status: 423, statusText: 'Locked' });

    expect(router.navigate).toHaveBeenCalledWith(['/change-password']);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/auth/interceptors/auth.interceptor.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/auth/interceptors/auth.interceptor.ts

import { Injectable, inject } from '@angular/core';
import {
  HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthStore } from '../auth.store';
import { AuthService } from '../auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly authStore = inject(AuthStore);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authStore.getToken();
    const authed = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

    return next.handle(authed).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 423) {
          this.router.navigate(['/change-password']);
          return throwError(() => err);
        }
        if (err.status === 401) {
          return this.authService.refresh().pipe(
            switchMap(res => {
              this.authStore.setToken(res.accessToken);
              const retried = req.clone({ setHeaders: { Authorization: `Bearer ${res.accessToken}` } });
              return next.handle(retried);
            }),
            catchError(refreshErr => {
              this.authStore.clearToken();
              this.router.navigate(['/login']);
              return throwError(() => refreshErr);
            })
          );
        }
        return throwError(() => err);
      })
    );
  }
}
```

Register in `src/app/app.config.ts`:
```typescript
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './auth/interceptors/auth.interceptor';

// In providers array:
{ provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/auth/interceptors/auth.interceptor.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/auth/interceptors/ src/app/app.config.ts
git commit -m "feat(auth): add AuthInterceptor with silent refresh and 423 handling (US-FE-005)"
```
