import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../common/services/auth.service';

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.scss'
})
export class UserLayoutComponent {
  public authService = inject(AuthService);

  logout(): void {
    this.authService.logout().subscribe();
  }
}
