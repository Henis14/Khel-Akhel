import { Injectable, inject } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PageVisitTracker {
  private readonly router = inject(Router);
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly endpoint = `${environment.apiBaseUrl}/website-statistics/track`;
  private readonly storageKey = 'ka.visited';

  private started = false;
  private lastUrlPath: string | null = null;

  private static readonly PAGE_KEYS: Record<string, string> = {
    '': 'home',
    products: 'shop',
    projects: 'projects',
    commissions: 'commissions',
    about: 'about',
    contact: 'contact',
    faq: 'faq',
    wishlist: 'wishlist',
    account: 'account',
    login: 'login',
    register: 'register',
    'track-order': 'track-order',
    'terms-conditions': 'terms',
    'privacy-policy': 'privacy',
  };

  start(): void {
    if (this.started || typeof window === 'undefined') return;
    this.started = true;

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => this.onNavigation(e.urlAfterRedirects || e.url));
  }

  trackProductView(): void {
    this.send('product');
  }

  private onNavigation(url: string): void {
    const path = (url || '/').split(/[?#]/)[0];
    if (path === this.lastUrlPath) return;
    this.lastUrlPath = path;

    const firstSegment = path.replace(/^\/+/, '').split('/')[0].toLowerCase();
    const pageKey = PageVisitTracker.PAGE_KEYS[firstSegment];
    if (pageKey) this.send(pageKey);
  }

  private send(pageKey: string): void {
    const isUnique = this.markVisited(pageKey);
    this.http
      .post(this.endpoint, { pageKey, isUnique }, { withCredentials: false })
      .subscribe({ error: () => undefined });
  }

  private markVisited(pageKey: string): boolean {
    const today = new Date();
    const stamp = `${today.getFullYear()}-${today.getMonth() + 1}-${today.getDate()}|${pageKey}`;
    try {
      const seen: string[] = JSON.parse(sessionStorage.getItem(this.storageKey) || '[]');
      const todays = Array.isArray(seen) ? seen.filter((s) => s.startsWith(stamp.split('|')[0] + '|')) : [];
      if (todays.includes(stamp)) return false;
      todays.push(stamp);
      sessionStorage.setItem(this.storageKey, JSON.stringify(todays));
      return true;
    } catch {
      return false;
    }
  }
}
