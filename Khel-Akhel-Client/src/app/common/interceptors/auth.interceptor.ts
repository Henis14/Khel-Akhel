import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, Subject, take, timer } from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
// Emits either { token: string } on success or { error: any } on refresh failure
const refreshResult$ = new Subject<{ token?: string; error?: any }>();

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  let authReq = req.clone({
    withCredentials: true,
  });

  const token = authService.token;
  if (token && !req.headers.has('Authorization')) {
    authReq = authReq.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // 409 Conflict from refresh endpoint: concurrent refresh race condition
      if (error.status === 409 && req.url.includes('/api/auth/refresh') && !req.headers.has('X-Refresh-Retry')) {
        return timer(300).pipe(
          switchMap(() => {
            const retryReq = req.clone({
              setHeaders: { 'X-Refresh-Retry': '1' },
            });
            return next(retryReq);
          }),
        );
      }

      if (error.status === 401) {
        // Do not attempt refresh on auth endpoints to avoid infinite loops
        if (req.url.includes('/api/auth/login') || req.url.includes('/api/auth/refresh')) {
          authService.clearSession();
          return throwError(() => error);
        }

        if (!isRefreshing) {
          isRefreshing = true;

          return authService.refreshToken().pipe(
            switchMap((res) => {
              isRefreshing = false;
              if (res.success && res.data?.token) {
                const newToken = res.data.token;
                refreshResult$.next({ token: newToken });
                return next(
                  req.clone({
                    withCredentials: true,
                    setHeaders: {
                      Authorization: `Bearer ${newToken}`,
                    },
                  }),
                );
              }
              const failError = new Error('Refresh token invalid or empty');
              refreshResult$.next({ error: failError });
              authService.clearSessionAndRedirect();
              return throwError(() => error);
            }),
            catchError((refreshErr) => {
              isRefreshing = false;
              refreshResult$.next({ error: refreshErr });
              authService.clearSessionAndRedirect();
              return throwError(() => refreshErr);
            }),
          );
        } else {
          // Waiting requests listen for the refresh result (either token or error)
          return refreshResult$.pipe(
            take(1),
            switchMap((result) => {
              if (result.token) {
                return next(
                  req.clone({
                    withCredentials: true,
                    setHeaders: {
                      Authorization: `Bearer ${result.token}`,
                    },
                  }),
                );
              }
              return throwError(() => result.error ?? error);
            }),
          );
        }
      }

      if (error.status === 403) {
        if (req.url.includes('/admin/')) {
          router.navigate(['/unauthorized']);
        }
      }

      return throwError(() => error);
    }),
  );
};
