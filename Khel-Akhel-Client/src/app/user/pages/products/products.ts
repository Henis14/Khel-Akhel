import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, forkJoin, from, mergeMap } from 'rxjs';
import { UserProductService } from '../../services/user-product.service';
import { WishlistState } from '../../services/wishlist-state';
import { ProductResponse } from '../../../admin/models/product.model';
import { ProductCategoryResponse } from '../../../admin/models/product-category.model';
import { ProductQuickviewModal } from '../../components/product-quickview-modal/product-quickview-modal';

export interface ShopCategoryRow {
  id: string;
  slug: string;
  name: string;
  description: string;
  products: ProductResponse[];
}

/** "Lamps & Lighting" -> "lamps-lighting" (used for ?category= links) */
export function slugify(value: string): string {
  return (value || '')
    .toLowerCase()
    .replace(/&/g, ' ')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

@Component({
  selector: 'app-user-products',
  standalone: true,
  imports: [RouterLink, ProductQuickviewModal],
  templateUrl: './products.html',
  styleUrl: './products.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserProductsComponent implements OnInit {
  private productService = inject(UserProductService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private host = inject(ElementRef<HTMLElement>);
  private destroyRef = inject(DestroyRef);
  readonly wishlist = inject(WishlistState);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly rows = signal<ShopCategoryRow[]>([]);
  readonly selected = signal<ProductResponse | null>(null);
  readonly activeSlug = signal<string | null>(null);

  readonly totalProducts = computed(() =>
    this.rows().reduce((sum, r) => sum + r.products.length, 0),
  );

  /** placeholder cards while loading */
  readonly skeletonRows = [0, 1];
  readonly skeletonCards = [0, 1, 2, 3];

  private readonly priceFormat = new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  });

  ngOnInit(): void {
    this.wishlist.load();
    this.load();

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((q) => {
      this.activeSlug.set(q.get('category'));
      if (!this.loading()) this.scrollToActiveCategory();
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      categories: this.productService.getCategories(),
      products: this.productService.getAllActiveProducts(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ categories, products }) => {
          this.rows.set(this.buildRows(categories, products));
          this.fillMissingImages();
          this.loading.set(false);
          this.scrollToActiveCategory();
        },
        error: () => {
          this.loading.set(false);
          this.error.set(true);
        },
      });
  }

  /**
   * One row per category in the admin's display order. Encrypted IDs differ
   * per response, so products are matched to categories by name.
   */
  private buildRows(
    categories: ProductCategoryResponse[],
    products: ProductResponse[],
  ): ShopCategoryRow[] {
    const byName = new Map<string, ProductResponse[]>();
    for (const p of products) {
      if (!p.isActive) continue;
      const key = (p.categoryName || '').trim().toLowerCase();
      if (!byName.has(key)) byName.set(key, []);
      byName.get(key)!.push(p);
    }

    return categories
      .map((c) => ({
        id: c.encryptedCategoryId,
        slug: slugify(c.categoryName),
        name: c.categoryName,
        description: c.description,
        products: byName.get(c.categoryName.trim().toLowerCase()) ?? [],
      }))
      .filter((r) => r.products.length > 0);
  }

  /**
   * Safety net: if the list API returns a product without images, read them
   * from the product detail API (a few at a time) and patch the card.
   */
  private fillMissingImages(): void {
    const missing = this.rows()
      .flatMap((r) => r.products)
      .filter((p) => !p.imagePaths?.length);
    if (!missing.length) return;

    from(missing)
      .pipe(
        mergeMap(
          (p) =>
            this.productService.getProductById(p.encryptedProductId).pipe(catchError(() => EMPTY)),
          4,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        const full = res?.success ? res.data : null;
        if (!full?.imagePaths?.length) return;
        this.rows.update((rows) =>
          rows.map((r) => ({
            ...r,
            products: r.products.map((p) =>
              p.productCode === full.productCode ? { ...p, imagePaths: full.imagePaths } : p,
            ),
          })),
        );
      });
  }

  /** "Art Prints" ~ "art-prints" ~ "artprint": ignores spacing, dashes and a plural s */
  private sameCategory(a: string, b: string): boolean {
    const norm = (v: string) => slugify(v).replace(/-/g, '').replace(/s$/, '');
    return norm(a) === norm(b);
  }

  private scrollToActiveCategory(): void {
    const wanted = this.activeSlug();
    if (!wanted) return;
    const row = this.rows().find((r) => this.sameCategory(r.slug, wanted));
    if (!row) return;
    const slug = row.slug;
    // wait for the rows to render
    setTimeout(() => {
      const el = this.host.nativeElement.querySelector(`#shop-${CSS.escape(slug)}`);
      if (!el) return;
      const offset = window.innerWidth <= 1100 ? 62 + 64 : 80 + 64; // header + chip bar
      const top = el.getBoundingClientRect().top + window.scrollY - offset;
      window.scrollTo({ top, behavior: 'smooth' });
    });
  }

  /* ---------- rail scrolling ---------- */
  scrollRail(rail: HTMLElement, direction: number): void {
    rail.scrollBy({ left: direction * rail.clientWidth, behavior: 'smooth' });
  }

  /* ---------- product helpers ---------- */
  image(p: ProductResponse, index = 0): string {
    return this.productService.imageUrl(p.imagePaths?.[index]);
  }

  price(value: number): string {
    return this.priceFormat.format(value ?? 0);
  }

  hasDiscount(p: ProductResponse): boolean {
    return p.mrp > p.sellingPrice && p.sellingPrice > 0;
  }

  badge(p: ProductResponse): string | null {
    if (p.availableQty <= 0) return 'Sold out';
    if (p.isNewArrival) return 'New';
    if (p.isBestSeller) return 'Bestseller';
    if (p.isTrending) return 'Trending';
    return null;
  }

  /* ---------- quick view ---------- */
  open(p: ProductResponse): void {
    this.selected.set(p);
  }

  close(): void {
    this.selected.set(null);
  }

  isInWishlist(productCode: string): boolean {
    return this.wishlist.has(productCode);
  }

  toggleWishlist(event: Event, p: ProductResponse): void {
    event.stopPropagation();
    this.wishlist.toggle(p.encryptedProductId, p.productCode).subscribe({ error: () => {} });
  }

  isActive(slug: string): boolean {
    const wanted = this.activeSlug();
    return !!wanted && this.sameCategory(slug, wanted);
  }

  goToCategory(slug: string): void {
    this.activeSlug.set(slug);
    this.scrollToActiveCategory();
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: slug },
      replaceUrl: true,
    });
  }
}
