import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal, computed } from '@angular/core';
import { of } from 'rxjs';
import { describe, beforeEach, it, expect, vi } from 'vitest';

import { routes } from './app.routes';
import { AuthService } from './common/services/auth.service';
import { NotFoundComponent } from './common/components/error-pages/not-found/not-found';
import { UnauthorizedComponent } from './common/components/unauthorized/unauthorized';

describe('App Routing & Wildcard Fallback', () => {
  let router: Router;
  const mockIsAuth = signal<boolean>(false);
  const mockIsAdmin = signal<boolean>(false);

  const mockAuthService = {
    isAuthenticated: mockIsAuth,
    isAdmin: mockIsAdmin,
    currentUser: signal(null),
    refreshToken: () => of({ success: false, data: null }),
    logout: () => of({ success: true, data: null }),
  };

  beforeEach(async () => {
    mockIsAuth.set(false);
    mockIsAdmin.set(false);

    await TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
  });

  it('should navigate to root / without throwing', async () => {
    const success = await router.navigateByUrl('/');
    expect(success).toBe(true);
    expect(router.url).toBe('/');
  });

  it('should navigate to /register successfully', async () => {
    const success = await router.navigateByUrl('/register');
    expect(success).toBe(true);
    expect(router.url).toBe('/register');
  });

  it('should navigate to /login successfully', async () => {
    const success = await router.navigateByUrl('/login');
    expect(success).toBe(true);
    expect(router.url).toBe('/login');
  });

  it('should navigate to /unauthorized successfully', async () => {
    const success = await router.navigateByUrl('/unauthorized');
    expect(success).toBe(true);
    expect(router.url).toBe('/unauthorized');
  });

  it('should display 404 NotFoundComponent for invalid route /regist', async () => {
    const success = await router.navigateByUrl('/regist');
    expect(success).toBe(true);
    expect(router.url).toBe('/regist');
  });

  it('should display 404 NotFoundComponent for invalid route /unknown-page', async () => {
    const success = await router.navigateByUrl('/unknown-page');
    expect(success).toBe(true);
    expect(router.url).toBe('/unknown-page');
  });

  it('should redirect unauthenticated users from /admin/unknown-page to /admin/login', async () => {
    mockIsAuth.set(false);
    mockIsAdmin.set(false);

    await router.navigateByUrl('/admin/unknown-page');
    expect(router.url).toContain('/admin/login');
  });

  it('should display 404 NotFoundComponent for /admin/unknown-page when authenticated as admin', async () => {
    mockIsAuth.set(true);
    mockIsAdmin.set(true);

    const success = await router.navigateByUrl('/admin/unknown-page');
    expect(success).toBe(true);
    expect(router.url).toBe('/admin/unknown-page');
  });

  it('should redirect non-admin authenticated users from /admin/dashboard to /unauthorized', async () => {
    mockIsAuth.set(true);
    mockIsAdmin.set(false);

    await router.navigateByUrl('/admin/dashboard');
    expect(router.url).toBe('/unauthorized');
  });

  it('should allow authenticated admin users to navigate to /admin/dashboard', async () => {
    mockIsAuth.set(true);
    mockIsAdmin.set(true);

    const success = await router.navigateByUrl('/admin/dashboard');
    expect(success).toBe(true);
    expect(router.url).toBe('/admin/dashboard');
  });

  it('should redirect unauthenticated users from /admin/commissioned-projects to /admin/login', async () => {
    mockIsAuth.set(false);
    mockIsAdmin.set(false);

    await router.navigateByUrl('/admin/commissioned-projects');
    expect(router.url).toContain('/admin/login');
  });

  it('should allow authenticated admin users to navigate to /admin/commissioned-projects', async () => {
    mockIsAuth.set(true);
    mockIsAdmin.set(true);

    const success = await router.navigateByUrl('/admin/commissioned-projects');
    expect(success).toBe(true);
    expect(router.url).toBe('/admin/commissioned-projects');
  });
});
