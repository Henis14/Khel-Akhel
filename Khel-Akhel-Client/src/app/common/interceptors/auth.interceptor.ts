import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  let authReq = req.clone({
    withCredentials: true
  });

  const token = authService.token;
  if (token && !req.headers.has('Authorization')) {
    authReq = authReq.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // Do not attempt refresh on auth endpoints to avoid infinite loops
        if (req.url.includes('/api/auth/login') || req.url.includes('/api/auth/refresh')) {
          authService.clearSession();
          return throwError(() => error);
        }

        if (!isRefreshing) {
          isRefreshing = true;
          refreshTokenSubject.next(null);

          return authService.refreshToken().pipe(
            switchMap((res) => {
              isRefreshing = false;
              if (res.success && res.data?.token) {
                refreshTokenSubject.next(res.data.token);
                return next(
                  req.clone({
                    withCredentials: true,
                    setHeaders: {
                      Authorization: `Bearer ${res.data.token}`
                    }
                  })
                );
              }
              authService.clearSessionAndRedirect();
              return throwError(() => error);
            }),
            catchError((refreshErr) => {
              isRefreshing = false;
              authService.clearSessionAndRedirect();
              return throwError(() => refreshErr);
            })
          );
        } else {
          return refreshTokenSubject.pipe(
            filter((t) => t !== null),
            take(1),
            switchMap((newToken) => {
              return next(
                req.clone({
                  withCredentials: true,
                  setHeaders: {
                    Authorization: `Bearer ${newToken}`
                  }
                })
              );
            })
          );
        }
      }

      if (error.status === 403) {
        if (req.url.includes('/admin/')) {
          router.navigate(['/unauthorized']);
        }
      }

      return throwError(() => error);
    })
  );
};
