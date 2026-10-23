import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { ApiService } from '../../common/services/api.service';

interface WishlistItem {
  encryptedProductId: string;
  productCode: string;
}

interface WishlistResponse {
  totalItems: number;
  items: WishlistItem[];
}

/**
 * Wishlist shared across the storefront. Works for guests too (the API keeps
 * a guest cookie). Encrypted IDs change on every response, so items are
 * matched by ProductCode.
 */
@Injectable({
  providedIn: 'root',
})
export class WishlistState {
  private api = inject(ApiService);

  private readonly codes = signal<ReadonlySet<string>>(new Set());
  private loaded = false;

  readonly count = computed(() => this.codes().size);

  has(productCode: string): boolean {
    return this.codes().has(productCode);
  }

  load(force = false): void {
    if (this.loaded && !force) return;
    this.loaded = true;
    this.api
      .get<WishlistResponse>('wishlist')
      .pipe(catchError(() => of(null)))
      .subscribe((res) => {
        if (res?.success && res.data) {
          this.codes.set(new Set(res.data.items.map((i) => i.productCode)));
        }
      });
  }

  /** Adds or removes the product. Emits true when it is now saved. */
  toggle(encryptedProductId: string, productCode: string): Observable<boolean> {
    const saved = this.has(productCode);
    const request$ = saved
      ? this.api.delete<unknown>(`wishlist/items/${encodeURIComponent(encryptedProductId)}`)
      : this.api.post<unknown>('wishlist/items', { encryptedProductId });

    return request$.pipe(
      map((res) => {
        if (!res?.success) throw new Error(res?.message || 'WISHLIST_FAILED');
        return !saved;
      }),
      tap((nowSaved) => {
        const next = new Set(this.codes());
        if (nowSaved) next.add(productCode);
        else next.delete(productCode);
        this.codes.set(next);
      }),
    );
  }
}
