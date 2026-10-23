import {
  Component,
  ElementRef,
  HostListener,
  ViewChild,
  computed,
  inject,
  signal,
  DestroyRef,
  Renderer2,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IsActiveMatchOptions,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../common/services/auth.service';

/* ---------- Navigation data shapes ---------- */
export interface NavLinkItem {
  label: string;
  link: string;
  queryParams?: Record<string, string>;
  badge?: string;
}
export interface NavColumn {
  title: string;
  links: NavLinkItem[];
}
export interface NavMenu {
  id: string;
  label: string;
  link: string;
  /** Short line shown in the first column of the mega panel */
  intro?: string;
  /** "View all" link label for the mega panel */
  viewAll?: string;
  columns?: NavColumn[];
}

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.scss',
})
export class UserLayoutComponent {
  @ViewChild('mobileMenuToggle') private mobileMenuToggle?: ElementRef<HTMLButtonElement>;
  @ViewChild('mobileNavDrawer') private mobileNavDrawer?: ElementRef<HTMLElement>;
  @ViewChild('storefrontHeader') private storefrontHeader?: ElementRef<HTMLElement>;

  readonly currentYear = new Date().getFullYear();
  public authService = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private renderer = inject(Renderer2);

  /** Sub-links (with query params) are highlighted only on an exact match */
  readonly exactMatch: IsActiveMatchOptions = {
    paths: 'exact',
    queryParams: 'exact',
    matrixParams: 'ignored',
    fragment: 'ignored',
  };

  /**
   * Primary navigation. Menus with `columns` open a mega panel on desktop and an
   * accordion in the mobile drawer.
   * TODO: bind the Shop columns to product categories from the API.
   */
  readonly menus: NavMenu[] = [
    {
      id: 'shop',
      label: 'Shop',
      link: '/products',
      intro: 'Objects, art and wearables, made by hand in the studio.',
      viewAll: 'Shop all',
      columns: [
        {
          title: 'Objects & forms',
          links: [
            {
              label: 'Lamps & lighting',
              link: '/products',
              queryParams: { category: 'lamps' },
              badge: 'New',
            },
            { label: 'Pottery', link: '/products', queryParams: { category: 'pottery' } },
            { label: 'Furniture', link: '/products', queryParams: { category: 'furniture' } },
            {
              label: 'Functional art',
              link: '/products',
              queryParams: { category: 'functional-art' },
            },
          ],
        },
        {
          title: 'Wearable art',
          links: [
            { label: 'Clothing', link: '/products', queryParams: { category: 'clothing' } },
            { label: 'Bags', link: '/products', queryParams: { category: 'bags' } },
            {
              label: 'One of a kind',
              link: '/products',
              queryParams: { category: 'one-of-a-kind' },
            },
          ],
        },
        {
          title: 'On paper',
          links: [
            {
              label: 'Prints & photographs',
              link: '/products',
              queryParams: { category: 'prints' },
            },
            { label: 'Calligraphy', link: '/products', queryParams: { category: 'calligraphy' } },
          ],
        },
      ],
    },
    {
      id: 'projects',
      label: 'Projects',
      link: '/projects',
      intro: 'Festivals, installations, images and gatherings.',
      viewAll: 'All projects',
      columns: [
        {
          title: 'Spaces',
          links: [
            { label: 'Festivals & sets', link: '/projects', queryParams: { type: 'festivals' } },
            { label: 'Interactive art', link: '/projects', queryParams: { type: 'interactive' } },
            { label: 'Installations', link: '/projects', queryParams: { type: 'installations' } },
          ],
        },
        {
          title: 'Image & story',
          links: [
            { label: 'Visual art', link: '/projects', queryParams: { type: 'visual-art' } },
            { label: 'Photography', link: '/projects', queryParams: { type: 'photography' } },
            { label: 'Film', link: '/projects', queryParams: { type: 'film' } },
          ],
        },
        {
          title: 'Learn & play',
          links: [
            { label: 'Shilp Khel', link: '/projects', queryParams: { type: 'workshops' } },
            { label: 'Sawal-e-Safar circles', link: '/projects', queryParams: { type: 'circles' } },
          ],
        },
      ],
    },
    { id: 'about', label: 'About', link: '/about' },
    { id: 'contact', label: 'Contact', link: '/contact' },
  ];

  /* ---------- State ---------- */
  readonly isMobileMenuOpen = signal<boolean>(false);
  readonly isHomePage = signal<boolean>(this.router.url.split(/[?#]/)[0] === '/');
  readonly isHeaderScrolled = signal<boolean>(false);
  readonly openMegaMenu = signal<string | null>(null);
  readonly openDrawerSection = signal<string | null>(null);

  /** Header is transparent only on the home page, at the top, with no menu open */
  readonly isHeaderSolid = computed(
    () => !this.isHomePage() || this.isHeaderScrolled() || !!this.openMegaMenu(),
  );

  private megaCloseTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.closeMobileMenu();
        this.closeMegaMenu();
        this.isHomePage.set(this.router.url.split(/[?#]/)[0] === '/');

        // Every page opens at the top (jump, not glide). In-page #anchors keep smooth scrolling.
        if (!this.router.parseUrl(this.router.url).fragment) {
          window.scrollTo({ top: 0, left: 0, behavior: 'instant' as ScrollBehavior });
        }
        this.isHeaderScrolled.set(false);
      });

    this.destroyRef.onDestroy(() => {
      this.renderer.removeStyle(document.body, 'overflow');
    });
  }

  /** The header always stays in view; it only turns solid white once you scroll */
  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.isHeaderScrolled.set(window.scrollY > 40);
  }

  @HostListener('window:keydown.escape')
  onEscape(): void {
    this.closeMegaMenu();
    if (this.isMobileMenuOpen()) this.closeMobileMenu();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const header = this.storefrontHeader?.nativeElement;
    if (header && !header.contains(event.target as Node)) {
      this.closeMegaMenu();
    }
  }

  @HostListener('window:keydown', ['$event'])
  onDrawerTab(event: KeyboardEvent): void {
    if (!this.isMobileMenuOpen() || event.key !== 'Tab') return;
    const drawer = this.mobileNavDrawer?.nativeElement;
    const focusable = drawer?.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])',
    );
    if (!focusable?.length) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  /* ---------- Mega menu ---------- */
  openMega(id: string): void {
    clearTimeout(this.megaCloseTimer);
    this.openMegaMenu.set(id);
  }

  scheduleMegaClose(): void {
    clearTimeout(this.megaCloseTimer);
    this.megaCloseTimer = setTimeout(() => this.openMegaMenu.set(null), 180);
  }

  keepMegaOpen(): void {
    clearTimeout(this.megaCloseTimer);
  }

  /**
   * Click on a menu with a mega panel. On touch screens (no hover) the first tap
   * opens the panel and a second tap follows the link; with a mouse it navigates.
   */
  onMegaTriggerClick(id: string, event: MouseEvent): void {
    const canHover = window.matchMedia?.('(hover: hover)').matches ?? true;
    if (!canHover && this.openMegaMenu() !== id) {
      event.preventDefault();
      this.openMega(id);
    }
  }

  toggleMega(id: string, event: Event): void {
    event.preventDefault();
    this.openMegaMenu.update((cur) => (cur === id ? null : id));
  }

  closeMegaMenu(): void {
    clearTimeout(this.megaCloseTimer);
    this.openMegaMenu.set(null);
  }

  /* ---------- Mobile drawer ---------- */
  toggleMobileMenu(): void {
    if (this.isMobileMenuOpen()) {
      this.closeMobileMenu();
    } else {
      this.openMobileMenu();
    }
  }

  openMobileMenu(): void {
    // Open the accordion of the section the visitor is currently in
    const path = this.router.url.split(/[?#]/)[0];
    const current = this.menus.find((m) => m.columns && path.startsWith(m.link));
    this.openDrawerSection.set(current?.id ?? null);

    this.isMobileMenuOpen.set(true);
    this.renderer.setStyle(document.body, 'overflow', 'hidden');
    queueMicrotask(() =>
      this.mobileNavDrawer?.nativeElement.querySelector<HTMLElement>('.drawer-close-btn')?.focus(),
    );
  }

  closeMobileMenu(): void {
    const wasOpen = this.isMobileMenuOpen();
    this.isMobileMenuOpen.set(false);
    this.renderer.removeStyle(document.body, 'overflow');
    if (wasOpen) queueMicrotask(() => this.mobileMenuToggle?.nativeElement.focus());
  }

  toggleDrawerSection(id: string): void {
    this.openDrawerSection.update((cur) => (cur === id ? null : id));
  }

  /* ---------- Scrolling helpers ---------- */
  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  skipToMain(event: Event): void {
    event.preventDefault();
    const main = document.getElementById('main-content');
    main?.focus({ preventScroll: true });
    main?.scrollIntoView({ behavior: 'smooth' });
  }

  logout(): void {
    this.closeMobileMenu();
    this.authService.logout().subscribe();
  }
}
