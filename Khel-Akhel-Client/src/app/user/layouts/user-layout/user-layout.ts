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
import { catchError, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../common/services/auth.service';
import { UserProductService } from '../../services/user-product.service';
import { CartState } from '../../services/cart-state';
import { PageVisitTracker } from '../../services/page-visit-tracker';

function slugify(value: string): string {
  return (value || '')
    .toLowerCase()
    .replace(/&/g, ' ')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

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
  @ViewChild('accountWrap') private accountWrap?: ElementRef<HTMLElement>;

  readonly currentYear = new Date().getFullYear();
  public authService = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private renderer = inject(Renderer2);
  private productService = inject(UserProductService);
  private cart = inject(CartState);
  private pageVisits = inject(PageVisitTracker);

  /** Items in the bag (updated by CartState whenever something is added) */
  readonly cartCount = computed(() => this.cart.totalItems());

  /** Shop categories from the API, in the admin's display order */
  readonly shopCategories = signal<{ name: string; slug: string }[]>([]);

  /** Footer shop links: live categories, or the original list until they load */
  readonly footerCategories = computed(() => {
    const live = this.shopCategories();
    if (live.length) return live.slice(0, 4);
    return [
      { name: 'Art prints', slug: 'art-prints' },
      { name: 'Lamps', slug: 'lamps' },
      { name: 'Clothing', slug: 'clothing' },
      { name: 'Gifts', slug: 'gifts' },
    ];
  });

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
   */
  private readonly baseMenus: NavMenu[] = [
    {
      id: 'shop',
      label: 'Shop',
      link: '/products',
      intro: 'Art, objects and wearables, made by hand in the studio.',
      viewAll: 'Shop all',
      columns: [
        {
          title: 'Art for walls',
          links: [
            { label: 'Art prints', link: '/products', queryParams: { category: 'art-prints' } },
            { label: 'Photographs', link: '/products', queryParams: { category: 'photographs' } },
            { label: 'Murals', link: '/products', queryParams: { category: 'murals' } },
          ],
        },
        {
          title: 'Objects & forms',
          links: [
            { label: 'Lamps', link: '/products', queryParams: { category: 'lamps' } },
            { label: 'Sculptures', link: '/products', queryParams: { category: 'sculptures' } },
            { label: 'Furniture', link: '/products', queryParams: { category: 'furniture' } },
          ],
        },
        {
          title: 'Wear & gift',
          links: [
            { label: 'Clothing', link: '/products', queryParams: { category: 'clothing' } },
            { label: 'Bags', link: '/products', queryParams: { category: 'bags' } },
            { label: 'Gifts', link: '/products', queryParams: { category: 'gifts' } },
          ],
        },
      ],
    },
    {
      id: 'projects',
      label: 'Projects',
      link: '/projects',
      intro: 'Festivals, installations, images, sound and gatherings.',
      viewAll: 'All projects',
      columns: [
        {
          title: 'Spaces & play',
          links: [
            {
              label: 'Festivals & sets',
              link: '/projects',
              queryParams: { type: 'festivals-and-sets' },
            },
            {
              label: 'Interactive art',
              link: '/projects',
              queryParams: { type: 'interactive-art' },
            },
            {
              label: 'Objects & forms',
              link: '/projects',
              queryParams: { type: 'objects-and-forms' },
            },
            { label: 'Wearable art', link: '/projects', queryParams: { type: 'wearable-art' } },
            { label: 'Architecture', link: '/projects', queryParams: { type: 'architecture' } },
          ],
        },
        {
          title: 'Image & sound',
          links: [
            {
              label: 'Visual art works',
              link: '/projects',
              queryParams: { type: 'visual-art-works' },
            },
            {
              label: 'Photo art works',
              link: '/projects',
              queryParams: { type: 'photo-art-works' },
            },
            { label: 'Film art works', link: '/projects', queryParams: { type: 'film-art-works' } },
            { label: 'Soundscapes', link: '/projects', queryParams: { type: 'soundscapes' } },
          ],
        },
        {
          title: 'Circles & learning',
          links: [
            {
              label: 'Circle: Sawal-e-Safar',
              link: '/projects',
              queryParams: { type: 'circle-sawal-e-safar' },
            },
            {
              label: 'E7: LTA workshop',
              link: '/projects',
              queryParams: { type: 'e7-lta-workshop' },
            },
            { label: 'Shilp Khel', link: '/projects', queryParams: { type: 'shilp-khel' } },
            { label: 'Exhibitions', link: '/projects', queryParams: { type: 'exhibitions' } },
            { label: 'Publications', link: '/projects', queryParams: { type: 'publications' } },
          ],
        },
      ],
    },
    { id: 'about', label: 'About', link: '/about' },
    { id: 'contact', label: 'Contact', link: '/contact' },
  ];

  /**
   * Menus as shown. Once the category API answers, the Shop panel lists the
   * real categories (so links always match the shop's ?category= filter);
   * until then, or if it fails, the original columns stay.
   */
  readonly menus = computed<NavMenu[]>(() => {
    const cats = this.shopCategories();
    if (!cats.length) return this.baseMenus;
    const perCol = Math.max(3, Math.ceil(cats.length / 3));
    const columns: NavColumn[] = [];
    for (let i = 0; i < cats.length && columns.length < 3; i += perCol) {
      columns.push({
        title: i === 0 ? 'Shop by form' : '',
        links: cats.slice(i, i + perCol).map((c) => ({
          label: c.name,
          link: '/products',
          queryParams: { category: c.slug },
        })),
      });
    }
    return this.baseMenus.map((m) => (m.id === 'shop' ? { ...m, columns } : m));
  });

  /* ---------- State ---------- */
  readonly isMobileMenuOpen = signal<boolean>(false);
  readonly isHomePage = signal<boolean>(this.router.url.split(/[?#]/)[0] === '/');
  readonly isHeaderScrolled = signal<boolean>(false);
  readonly openMegaMenu = signal<string | null>(null);
  readonly openDrawerSection = signal<string | null>(null);
  readonly isAccountMenuOpen = signal<boolean>(false);

  /** Header is transparent only on the home page, at the top, with no menu open */
  readonly isHeaderSolid = computed(
    () => !this.isHomePage() || this.isHeaderScrolled() || !!this.openMegaMenu(),
  );

  private megaCloseTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.pageVisits.start();

    this.productService
      .getCategories()
      .pipe(
        catchError(() => of([])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((cats) =>
        this.shopCategories.set(
          cats
            .filter((c) => c.isActive !== false && !!c.categoryName)
            .map((c) => ({ name: c.categoryName.trim(), slug: slugify(c.categoryName) })),
        ),
      );

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.closeMobileMenu();
        this.closeMegaMenu();
        this.closeAccountMenu();
        this.isHomePage.set(this.router.url.split(/[?#]/)[0] === '/');

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
    this.closeAccountMenu();
    if (this.isMobileMenuOpen()) this.closeMobileMenu();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const header = this.storefrontHeader?.nativeElement;
    if (header && !header.contains(event.target as Node)) {
      this.closeMegaMenu();
    }
    const account = this.accountWrap?.nativeElement;
    if (account && !account.contains(event.target as Node)) {
      this.closeAccountMenu();
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
    const current = this.menus().find((m) => m.columns && path.startsWith(m.link));
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

  /* ---------- Account menu ---------- */
  toggleAccountMenu(): void {
    this.closeMegaMenu();
    this.isAccountMenuOpen.update((open) => !open);
  }

  closeAccountMenu(): void {
    this.isAccountMenuOpen.set(false);
  }

  /** 2 -> "02" for the drawer's numbering */
  pad(n: number): string {
    return n.toString().padStart(2, '0');
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
    this.closeAccountMenu();
    this.authService.logout().subscribe();
  }
}
