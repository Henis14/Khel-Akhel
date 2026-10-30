import { Component, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
})
export class NotFoundComponent {
  private location = inject(Location);
  private router = inject(Router);

  /** The admin dashboard keeps its own 404; the storefront gets the studio version */
  readonly isAdmin = this.router.url.startsWith('/admin');

  /**
   * Navigate back to the previously visited page in browser history.
   */
  goBack(): void {
    this.location.back();
  }
}

// Backward-compatibility alias
export { NotFoundComponent as NotFound };
