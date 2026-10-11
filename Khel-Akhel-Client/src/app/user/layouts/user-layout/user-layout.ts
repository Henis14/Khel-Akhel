import {
  Component,
  HostListener,
  OnInit,
  inject,
  signal,
  DestroyRef,
  Renderer2,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../common/services/auth.service';

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.scss',
})
export class UserLayoutComponent implements OnInit {
  public authService = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private renderer = inject(Renderer2);

  /**
   * Reactive signal controlling the mobile navigation drawer state.
   */
  readonly isMobileMenuOpen = signal<boolean>(false);

  constructor() {
    // Automatically close the mobile navigation drawer whenever router navigation succeeds.
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.closeMobileMenu();
      });

    // Cleanup scroll lock if component is destroyed while drawer is open
    this.destroyRef.onDestroy(() => {
      this.renderer.removeStyle(document.body, 'overflow');
    });
  }

  ngOnInit(): void {
    // Layout initialisation hook
  }

  /**
   * Listen for keyboard Escape key to close the mobile menu drawer.
   */
  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.isMobileMenuOpen()) {
      this.closeMobileMenu();
    }
  }

  /**
   * Toggle mobile navigation open/close state.
   */
  toggleMobileMenu(): void {
    if (this.isMobileMenuOpen()) {
      this.closeMobileMenu();
    } else {
      this.openMobileMenu();
    }
  }

  /**
   * Open the mobile navigation drawer and lock body scroll.
   */
  openMobileMenu(): void {
    this.isMobileMenuOpen.set(true);
    this.renderer.setStyle(document.body, 'overflow', 'hidden');
  }

  /**
   * Close the mobile navigation drawer and restore body scroll.
   */
  closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
    this.renderer.removeStyle(document.body, 'overflow');
  }

  /**
   * Log out the current customer using the existing AuthService session management.
   */
  logout(): void {
    this.closeMobileMenu();
    this.authService.logout().subscribe();
  }
}
