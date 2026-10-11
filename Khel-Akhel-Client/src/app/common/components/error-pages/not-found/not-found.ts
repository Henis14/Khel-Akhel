import { Component, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
})
export class NotFoundComponent {
  private location = inject(Location);

  /**
   * Navigate back to the previously visited page in browser history.
   */
  goBack(): void {
    this.location.back();
  }
}

// Backward-compatibility alias
export { NotFoundComponent as NotFound };
