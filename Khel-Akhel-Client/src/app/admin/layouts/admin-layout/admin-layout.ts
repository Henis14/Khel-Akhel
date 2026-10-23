import { Component, DestroyRef, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from '../../../common/services/auth.service';

type MenuKey = 'customers' | 'products' | 'categories' | 'stock' | 'reports' | 'settings';

const MOBILE_BREAKPOINT = 768;

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
})
export class AdminLayoutComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  /** open = full sidebar on desktop / drawer visible on mobile */
  sidebarOpen = typeof window === 'undefined' || window.innerWidth > MOBILE_BREAKPOINT;
  currentUser = this.authService.currentUser;
  pendingResetCount = signal<number>(0);

  readonly userEmail = computed(() => this.currentUser()?.email || 'Administrator');
  readonly userInitial = computed(() => (this.currentUser()?.email || 'A').charAt(0).toUpperCase());

  // Submenu Toggle States
  customersMenuOpen = false;
  productsMenuOpen = false;
  categoriesMenuOpen = false;
  stockMenuOpen = false;
  reportsMenuOpen = false;
  settingsMenuOpen = false;

  ngOnInit(): void {
    this.openMenusForUrl(this.router.url);

    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((e) => {
        this.openMenusForUrl(e.urlAfterRedirects);
        // on phones the sidebar is a drawer: close it after choosing a page
        if (this.isMobile()) this.sidebarOpen = false;
      });

    this.loadPendingResetCount();
  }

  /** expands the group that contains the current page */
  private openMenusForUrl(url: string): void {
    if (url.includes('/admin/products')) this.productsMenuOpen = true;
    if (url.includes('/admin/product-categories')) this.categoriesMenuOpen = true;
    if (url.includes('/admin/product-stock')) this.stockMenuOpen = true;
    if (url.includes('/admin/customers') || url.includes('/admin/customer-addresses'))
      this.customersMenuOpen = true;
  }

  isGroupActive(prefix: string): boolean {
    return this.router.url.startsWith(prefix);
  }

  loadPendingResetCount(): void {
    this.authService.getPasswordResetRequests('PENDING', 1, 1).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.pendingResetCount.set(res.data.pendingCount || 0);
        }
      },
      error: () => {},
    });
  }

  isMobile(): boolean {
    return typeof window !== 'undefined' && window.innerWidth <= MOBILE_BREAKPOINT;
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  toggleMenu(menu: MenuKey | string): void {
    // in the icon-only rail, clicking a group opens the sidebar with that group expanded
    if (!this.sidebarOpen && !this.isMobile()) {
      this.sidebarOpen = true;
      this.setMenu(menu, true);
      return;
    }
    this.setMenu(menu, !this.isMenuOpen(menu));
  }

  private isMenuOpen(menu: string): boolean {
    switch (menu) {
      case 'customers': return this.customersMenuOpen;
      case 'products': return this.productsMenuOpen;
      case 'categories': return this.categoriesMenuOpen;
      case 'stock': return this.stockMenuOpen;
      case 'reports': return this.reportsMenuOpen;
      case 'settings': return this.settingsMenuOpen;
      default: return false;
    }
  }

  private setMenu(menu: string, open: boolean): void {
    if (menu === 'customers') this.customersMenuOpen = open;
    if (menu === 'products') this.productsMenuOpen = open;
    if (menu === 'categories') this.categoriesMenuOpen = open;
    if (menu === 'stock') this.stockMenuOpen = open;
    if (menu === 'reports') this.reportsMenuOpen = open;
    if (menu === 'settings') this.settingsMenuOpen = open;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isMobile() && this.sidebarOpen) this.sidebarOpen = false;
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/admin/login']);
      },
      error: () => {
        this.router.navigate(['/admin/login']);
      },
    });
  }
}
