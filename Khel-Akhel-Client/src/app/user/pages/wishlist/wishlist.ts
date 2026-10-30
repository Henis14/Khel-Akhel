import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../common/services/api.service';
import { AuthService } from '../../../common/services/auth.service';
import { UserProductService } from '../../services/user-product.service';
import { WishlistState } from '../../services/wishlist-state';
import { CartState } from '../../services/cart-state';

/** One saved piece, as returned by GET /api/wishlist */
interface WishlistItemView {
  encryptedProductId: string;
  productCode: string;
  productName: string;
  categoryName: string;
  shortDescription?: string | null;
  mrp: number;
  sellingPrice: number;
  primaryImagePath?: string | null;
  isInStock: boolean;
  isActive: boolean;
}

interface WishlistPayload {
  totalItems: number;
  items: WishlistItemView[];
}

/**
 * /wishlist — pieces the visitor saved with the heart. Works for guests too
 * (the API keeps a guest cookie); adding to the bag needs a signed-in customer.
 */
@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './wishlist.html',
  styleUrl: './wishlist.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Wishlist implements OnInit {
  private api = inject(ApiService);
  private destroyRef = inject(DestroyRef);
  private products = inject(UserProductService);
  private wishlistState = inject(WishlistState);
  private cartState = inject(CartState);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal(false);
  readonly items = signal<WishlistItemView[]>([]);
  /** productCode of the row being changed, to disable its buttons */
  readonly busy = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly skeletons = [1, 2, 3];

  private readonly priceFormat = new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.api
      .get<WishlistPayload>('wishlist')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.set(res?.success && res.data ? res.data.items ?? [] : []);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  remove(item: WishlistItemView): void {
    if (this.busy()) return;
    this.busy.set(item.productCode);
    this.notice.set(null);
    this.wishlistState.toggle(item.encryptedProductId, item.productCode).subscribe({
      next: () => {
        this.items.update((list) => list.filter((i) => i.productCode !== item.productCode));
        this.busy.set(null);
      },
      error: () => {
        this.notice.set(`We couldn't remove ${item.productName}. Please try again.`);
        this.busy.set(null);
      },
    });
  }

  moveToBag(item: WishlistItemView): void {
    if (this.busy()) return;
    this.busy.set(item.productCode);
    this.notice.set(null);
    this.api
      .post<{ totalItems?: number }>(
        `wishlist/items/${encodeURIComponent(item.encryptedProductId)}/move-to-cart`,
        { quantity: 1 },
      )
      .subscribe({
        next: (res) => {
          this.busy.set(null);
          if (!res?.success) {
            this.notice.set(`We couldn't add ${item.productName} to your bag.`);
            return;
          }
          if (typeof res.data?.totalItems === 'number') {
            this.cartState.totalItems.set(res.data.totalItems);
          }
          this.items.update((list) => list.filter((i) => i.productCode !== item.productCode));
          this.wishlistState.load(true);
          this.notice.set(`${item.productName} is in your bag.`);
        },
        error: () => {
          this.busy.set(null);
          this.notice.set(`We couldn't add ${item.productName} to your bag. Please try again.`);
        },
      });
  }

  image(item: WishlistItemView): string {
    return this.products.imageUrl(item.primaryImagePath);
  }

  price(value: number): string {
    return this.priceFormat.format(value ?? 0);
  }

  hasDiscount(item: WishlistItemView): boolean {
    return item.mrp > item.sellingPrice && item.sellingPrice > 0;
  }

  canBuy(item: WishlistItemView): boolean {
    return item.isActive && item.isInStock;
  }
}
