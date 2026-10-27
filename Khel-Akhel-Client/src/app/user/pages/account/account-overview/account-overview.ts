import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../common/services/auth.service';

@Component({
  selector: 'app-account-overview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './account-overview.html',
  styleUrl: './account-overview.scss',
})
export class AccountOverview {
  public authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
  }
}
