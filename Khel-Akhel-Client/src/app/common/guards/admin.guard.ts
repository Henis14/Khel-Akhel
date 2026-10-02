import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, catchError, of } from 'rxjs';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuth = authService.isAuthenticated();
  const isAdmin = authService.isAdmin();

  if (isAuth) {
    if (isAdmin) {
      return true;
    }
    return router.createUrlTree(['/unauthorized']);
  }

  return authService.refreshToken().pipe(
    map((res) => {
      const refreshedAuth = authService.isAuthenticated();
      const refreshedAdmin = authService.isAdmin();

      if (res.success && refreshedAuth && refreshedAdmin) {
        return true;
      }
      if (refreshedAuth && !refreshedAdmin) {
        return router.createUrlTree(['/unauthorized']);
      }
      return router.createUrlTree(['/admin/login'], { queryParams: { returnUrl: state.url } });
    }),
    catchError(() => {
      return of(router.createUrlTree(['/admin/login'], { queryParams: { returnUrl: state.url } }));
    })
  );
};
