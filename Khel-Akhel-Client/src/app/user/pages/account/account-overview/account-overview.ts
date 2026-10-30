import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { UserAddressService } from '../../../services/user-address.service';
import { WishlistState } from '../../../services/wishlist-state';

/** /account — a short summary of each account section, inside the account frame. */
@Component({
  selector: 'app-account-overview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './account-overview.html',
  styleUrl: './account-overview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountOverview implements OnInit {
  private addressService = inject(UserAddressService);
  private destroyRef = inject(DestroyRef);
  readonly wishlist = inject(WishlistState);

  /** null while loading or if the count could not be read */
  readonly addressCount = signal<number | null>(null);
  readonly defaultCity = signal<string>('');

  ngOnInit(): void {
    this.wishlist.load();

    this.addressService
      .getMyAddresses()
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        if (!res?.success || !Array.isArray(res.data)) return;
        this.addressCount.set(res.data.length);
        const preferred = res.data.find((a) => a.isDefault) ?? res.data[0];
        this.defaultCity.set(preferred?.city ?? '');
      });
  }
}
