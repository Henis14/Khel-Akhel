import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  OnInit,
  Output,
  Renderer2,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { ProductResponse } from '../../../admin/models/product.model';
import { UserProductService } from '../../services/user-product.service';
import { WishlistState } from '../../services/wishlist-state';
import { CartState } from '../../services/cart-state';
import { PageVisitTracker } from '../../services/page-visit-tracker';
import { AuthService } from '../../../common/services/auth.service';

type StockState = 'in' | 'low' | 'out';
type CartStatus = 'idle' | 'adding' | 'added' | 'error';

/** Stock is never shown as a number; under this many pieces counts as low */
const LOW_STOCK_BELOW = 3;
const MAX_QTY = 10;

@Component({
  selector: 'app-product-quickview-modal',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './product-quickview-modal.html',
  styleUrl: './product-quickview-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductQuickviewModal implements OnInit, AfterViewInit, OnDestroy {
  private productService = inject(UserProductService);
  private wishlist = inject(WishlistState);
  private cart = inject(CartState);
  private auth = inject(AuthService);
  private router = inject(Router);
  private renderer = inject(Renderer2);
  private document = inject(DOCUMENT);
  private destroyRef = inject(DestroyRef);
  private pageVisits = inject(PageVisitTracker);

  /** Product as shown on the card; full details are refreshed on open */
  @Input({ required: true }) set product(val: ProductResponse) {
    this._product.set(val);
  }
  get product(): ProductResponse {
    return this._product();
  }
  private readonly _product = signal<ProductResponse>({} as ProductResponse);
  @Output() readonly closed = new EventEmitter<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');
  private readonly closeBtn = viewChild.required<ElementRef<HTMLButtonElement>>('closeBtn');

  readonly detail = signal<ProductResponse | null>(null);
  readonly activeIndex = signal(0);
  readonly qty = signal(1);
  readonly cartStatus = signal<CartStatus>('idle');
  readonly cartMessage = signal('');
  readonly wishBusy = signal(false);
  readonly closing = signal(false);
  readonly openSection = signal<'description' | 'details' | 'care' | null>('description');

  readonly p = computed(() => this.detail() ?? this._product());
  readonly images = computed(() =>
    (this.p().imagePaths ?? []).map((path) => this.productService.imageUrl(path)),
  );
  readonly stock = computed<StockState>(() => {
    const q = this.p().availableQty ?? 0;
    if (q <= 0) return 'out';
    return q < LOW_STOCK_BELOW ? 'low' : 'in';
  });
  readonly maxQty = computed(() => Math.max(1, Math.min(this.p().availableQty ?? 1, MAX_QTY)));
  readonly saved = computed(() => this.wishlist.has(this.p().productCode));
  readonly discount = computed(() => {
    const { mrp, sellingPrice } = this.p();
    return mrp > sellingPrice && mrp > 0 ? Math.round(((mrp - sellingPrice) / mrp) * 100) : 0;
  });
  readonly tags = computed(() => {
    const p = this.p();
    const t: string[] = [];
    if (p.isNewArrival) t.push('New arrival');
    if (p.isBestSeller) t.push('Bestseller');
    if (p.isTrending) t.push('Trending');
    if (p.isCustomerFavourite) t.push('Customer favourite');
    return t;
  });

  private readonly priceFormat = new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  });

  private previouslyFocused: HTMLElement | null = null;
  private touchStartX = 0;

  ngOnInit(): void {
    this.previouslyFocused = this.document.activeElement as HTMLElement | null;
    this.renderer.setStyle(this.document.body, 'overflow', 'hidden');
    this.pageVisits.trackProductView();

    this.productService
      .getProductById(this.product.encryptedProductId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (res?.success && res.data) this.detail.set(res.data);
        },
        error: () => {
          /* keep showing the card data */
        },
      });
  }

  ngAfterViewInit(): void {
    this.closeBtn().nativeElement.focus({ preventScroll: true });
  }

  ngOnDestroy(): void {
    this.renderer.removeStyle(this.document.body, 'overflow');
    this.previouslyFocused?.focus?.({ preventScroll: true });
  }

  /* ---------- formatting ---------- */
  price(value: number): string {
    return this.priceFormat.format(value ?? 0);
  }

  /* ---------- gallery ---------- */
  show(index: number): void {
    const count = this.images().length;
    if (!count) return;
    this.activeIndex.set((index + count) % count);
  }

  next(): void {
    this.show(this.activeIndex() + 1);
  }

  prev(): void {
    this.show(this.activeIndex() - 1);
  }

  onTouchStart(e: TouchEvent): void {
    this.touchStartX = e.changedTouches[0]?.clientX ?? 0;
  }

  onTouchEnd(e: TouchEvent): void {
    const dx = (e.changedTouches[0]?.clientX ?? 0) - this.touchStartX;
    if (Math.abs(dx) > 40) dx < 0 ? this.next() : this.prev();
  }

  /* ---------- quantity ---------- */
  changeQty(delta: number): void {
    const next = Math.min(this.maxQty(), Math.max(1, this.qty() + delta));
    this.qty.set(next);
  }

  /* ---------- actions ---------- */
  addToCart(): void {
    if (this.stock() === 'out' || this.cartStatus() === 'adding') return;

    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    this.cartStatus.set('adding');
    this.cartMessage.set('');
    this.cart.add(this.p().encryptedProductId, this.qty()).subscribe({
      next: () => {
        this.cartStatus.set('added');
        this.cartMessage.set('Added to your bag');
      },
      error: (err: unknown) => {
        this.cartStatus.set('error');
        this.cartMessage.set(this.cartError(err));
      },
    });
  }

  private cartError(err: unknown): string {
    const code =
      err instanceof HttpErrorResponse ? (err.error?.message as string | undefined) : undefined;
    switch (code) {
      case 'INSUFFICIENT_STOCK':
        return 'Only a few are left. Please try a smaller quantity.';
      case 'PRODUCT_INACTIVE':
      case 'PRODUCT_NOT_FOUND':
        return 'Sorry, this piece is no longer available.';
      case 'UNAUTHORIZED':
        return 'Please sign in to add pieces to your bag.';
      default:
        return 'Something went wrong. Please try again.';
    }
  }

  toggleWishlist(): void {
    if (this.wishBusy()) return;
    this.wishBusy.set(true);
    this.wishlist.toggle(this.p().encryptedProductId, this.p().productCode).subscribe({
      next: () => this.wishBusy.set(false),
      error: () => this.wishBusy.set(false),
    });
  }

  toggleSection(id: 'description' | 'details' | 'care'): void {
    this.openSection.update((cur) => (cur === id ? null : id));
  }

  /* ---------- closing ---------- */
  close(): void {
    if (this.closing()) return;
    this.closing.set(true);
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    setTimeout(() => this.closed.emit(), reduce ? 0 : 220);
  }

  onBackdrop(e: MouseEvent): void {
    if (e.target === e.currentTarget) this.close();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
      e.preventDefault();
      this.close();
    } else if (e.key === 'ArrowRight' && !this.isTyping(e)) {
      this.next();
    } else if (e.key === 'ArrowLeft' && !this.isTyping(e)) {
      this.prev();
    } else if (e.key === 'Tab') {
      this.trapFocus(e);
    }
  }

  private isTyping(e: KeyboardEvent): boolean {
    const t = e.target as HTMLElement | null;
    return !!t && /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName);
  }

  private trapFocus(e: KeyboardEvent): void {
    const focusables = Array.from(
      this.dialog().nativeElement.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ),
    ).filter((el) => el.offsetParent !== null);
    if (!focusables.length) return;
    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    const active = this.document.activeElement;
    if (e.shiftKey && active === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && active === last) {
      e.preventDefault();
      first.focus();
    }
  }
}
