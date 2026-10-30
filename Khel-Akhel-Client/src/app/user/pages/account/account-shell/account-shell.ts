import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../../common/services/auth.service';

interface AccountNavItem {
  label: string;
  link: string;
  /** exact match for the overview, so it is not "active" on every child page */
  exact: boolean;
}

/**
 * Frame shared by every /account page: page intro, section menu and sign out.
 * Child pages render inside the outlet and only draw their own content.
 */
@Component({
  selector: 'app-account-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './account-shell.html',
  styleUrl: './account-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountShell {
  private authService = inject(AuthService);

  readonly email = computed(() => this.authService.currentUser()?.email ?? '');

  /** "asha.kaur@gmail.com" → "Asha" — a friendly greeting without a profile lookup. */
  readonly firstName = computed(() => {
    const local = this.email().split('@')[0] || '';
    const word = local.split(/[._\-+0-9]+/).find((w) => w.length > 1) ?? '';
    return word ? word.charAt(0).toUpperCase() + word.slice(1).toLowerCase() : '';
  });

  readonly nav: AccountNavItem[] = [
    { label: 'Overview', link: '/account', exact: true },
    { label: 'Saved addresses', link: '/account/addresses', exact: false },
    { label: 'Wishlist', link: '/wishlist', exact: false },
    { label: 'Change password', link: '/reset-password', exact: false },
  ];

  signOut(): void {
    this.authService.logout().subscribe();
  }
}
