import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, catchError, of } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Attempt background refresh if session can be restored via HttpOnly cookie
  return authService.refreshToken().pipe(
    map((res) => {
      if (res.success && authService.isAuthenticated()) {
        return true;
      }
      return router.createUrlTree(['/admin/login'], { queryParams: { returnUrl: state.url } });
    }),
    catchError(() => {
      return of(router.createUrlTree(['/admin/login'], { queryParams: { returnUrl: state.url } }));
    })
  );
};
