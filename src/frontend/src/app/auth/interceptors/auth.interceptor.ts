import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthStore } from '../auth.store';
import { AuthService } from '../auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authStore = inject(AuthStore);
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authStore.getToken();
  const authed = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authed).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 423) {
        router.navigate(['/change-password']);
        return throwError(() => err);
      }

      if (err.status === 401 && !req.url.includes('/auth/refresh') && !req.url.includes('/auth/login') && !req.url.includes('/auth/change-password-first-login')) {
        return authService.refresh().pipe(
          switchMap(res => {
            authStore.setToken(res.accessToken);
            const retried = req.clone({
              setHeaders: { Authorization: `Bearer ${res.accessToken}` },
            });
            return next(retried);
          }),
          catchError(refreshErr => {
            authStore.clearToken();
            router.navigate(['/login']);
            return throwError(() => refreshErr);
          })
        );
      }

      if (err.status === 401 && !req.url.includes('/auth/change-password-first-login')) {
        authStore.clearToken();
        router.navigate(['/login']);
      }

      return throwError(() => err);
    })
  );
};
