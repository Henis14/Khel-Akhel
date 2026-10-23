import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { ApiService } from '../../common/services/api.service';

interface CartResponse {
  totalItems: number;
  distinctItemsCount: number;
}

/** Cart shared across the storefront (the API requires a signed-in customer) */
@Injectable({
  providedIn: 'root',
})
export class CartState {
  private api = inject(ApiService);

  readonly totalItems = signal(0);

  add(encryptedProductId: string, quantity = 1): Observable<CartResponse> {
    return this.api.post<CartResponse>('cart/items', { encryptedProductId, quantity }).pipe(
      map((res) => {
        if (!res?.success) throw new Error(res?.message || 'CART_FAILED');
        return res.data;
      }),
      tap((cart) => this.totalItems.set(cart?.totalItems ?? 0)),
    );
  }
}
