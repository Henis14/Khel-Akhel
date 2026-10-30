import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, catchError, defer, forkJoin, of } from 'rxjs';
import { UserProductService } from '../../../services/user-product.service';
import { ProductResponse } from '../../../../admin/models/product.model';
import { ProductCategoryResponse } from '../../../../admin/models/product-category.model';
import { ProductQuickviewModal } from '../../../components/product-quickview-modal/product-quickview-modal';
import { RevealDirective } from '../../../components/reveal/reveal.directive';

/**
 * The homepage below the hero: the Khel-Akhel introduction and "From the
 * studio", which shows one piece from every product category.
 *
 * Data comes from the existing public endpoints (product-category/list and
 * product/list), so new categories and products from the admin dashboard
 * appear here automatically.
 */
@Component({
  selector: 'app-home-story',
  standalone: true,
  imports: [RouterLink, ProductQuickviewModal, RevealDirective],
  templateUrl: './home-story.html',
  styleUrl: './home-story.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeStory implements OnInit {
  private productService = inject(UserProductService);
  private destroyRef = inject(DestroyRef);

  readonly products = signal<ProductResponse[]>([]);
  readonly categories = signal<ProductCategoryResponse[]>([]);
  readonly selected = signal<ProductResponse | null>(null);

  /**
   * One representative piece per category, in the admin's category order.
   * Within a category: featured first, then bestseller, then in stock,
   * then the newest; pieces with a photograph are always preferred.
   */
  readonly featured = computed(() => {
    const byCategory = new Map<string, ProductResponse[]>();
    for (const p of this.products()) {
      if (!p || p.isActive === false) continue;
      const key = (p.categoryName || '').trim().toLowerCase();
      if (!key) continue;
      byCategory.set(key, [...(byCategory.get(key) || []), p]);
    }

    const score = (p: ProductResponse) =>
      (p.imagePaths?.length ? 0 : 1000) +
      (p.isFeatured ? 0 : 100) +
      (p.isBestSeller ? 0 : 10) +
      ((p.availableQty ?? 0) > 0 ? 0 : 1);
    const newest = (p: ProductResponse) => new Date(p.createdDate || 0).getTime();
    const pick = (list: ProductResponse[]) =>
      [...list].sort((a, b) => score(a) - score(b) || newest(b) - newest(a))[0];

    const ordered = this.categories()
      .filter((c) => !!c?.categoryName && c.isActive !== false)
      .map((c) => byCategory.get(c.categoryName.trim().toLowerCase()))
      .filter((list): list is ProductResponse[] => !!list?.length)
      .map(pick);

    // products whose category is missing from the category list still get a place
    const known = new Set(
      this.categories()
        .filter((c) => !!c?.categoryName)
        .map((c) => c.categoryName.trim().toLowerCase()),
    );
    for (const [key, list] of byCategory) {
      if (!known.has(key)) ordered.push(pick(list));
    }
    return ordered;
  });

  private readonly priceFormat = new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  });

  ngOnInit(): void {
    const safe = <T>(factory: () => Observable<T>, fallback: T) =>
      defer(factory).pipe(catchError(() => of(fallback)));

    forkJoin({
      products: safe(() => this.productService.getAllActiveProducts(), [] as ProductResponse[]),
      categories: safe(() => this.productService.getCategories(), [] as ProductCategoryResponse[]),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ products, categories }) => {
        this.products.set(Array.isArray(products) ? products : []);
        this.categories.set(Array.isArray(categories) ? categories : []);
      });
  }

  image(p: ProductResponse, i = 0): string {
    return this.productService.imageUrl(p.imagePaths?.[i]);
  }

  price(v: number): string {
    return this.priceFormat.format(v || 0);
  }

  hasDiscount(p: ProductResponse): boolean {
    return p.mrp > p.sellingPrice && p.sellingPrice > 0;
  }

  open(p: ProductResponse): void {
    this.selected.set(p);
  }

  close(): void {
    this.selected.set(null);
  }
}
