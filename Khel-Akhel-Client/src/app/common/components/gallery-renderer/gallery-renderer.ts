import {
  Component,
  Input,
  Output,
  EventEmitter,
  HostListener,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface GalleryImageItem {
  imageUrl: string;
  originalFileName?: string;
  displayOrder?: number;
  caption?: string;
}

@Component({
  selector: 'app-gallery-renderer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gallery-renderer.html',
  styleUrl: './gallery-renderer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GalleryRendererComponent {
  @Input() images: GalleryImageItem[] = [];
  @Input() layoutType: string = 'grid-3';
  @Input() interactive: boolean = true;
  @Input() sectionTitle: string = '';

  // Lightbox state
  isLightboxOpen = false;
  activeLightboxIndex = 0;

  get resolvedLayout(): string {
    const valid = [
      'grid-3',
      'grid-2',
      'editorial',
      'featured',
      'full-width',
      'masonry',
      'asymmetric',
    ];
    return valid.includes(this.layoutType?.toLowerCase())
      ? this.layoutType.toLowerCase()
      : 'grid-3';
  }

  get activeImage(): GalleryImageItem | null {
    if (!this.images || this.images.length === 0) return null;
    return this.images[this.activeLightboxIndex] ?? null;
  }

  openLightbox(index: number): void {
    if (!this.interactive || !this.images || this.images.length === 0) return;
    this.activeLightboxIndex = Math.max(0, Math.min(index, this.images.length - 1));
    this.isLightboxOpen = true;
  }

  closeLightbox(): void {
    this.isLightboxOpen = false;
  }

  nextImage(): void {
    if (!this.images || this.images.length === 0) return;
    this.activeLightboxIndex = (this.activeLightboxIndex + 1) % this.images.length;
  }

  prevImage(): void {
    if (!this.images || this.images.length === 0) return;
    this.activeLightboxIndex =
      (this.activeLightboxIndex - 1 + this.images.length) % this.images.length;
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isLightboxOpen) return;
    if (event.key === 'Escape') {
      this.closeLightbox();
    } else if (event.key === 'ArrowRight') {
      this.nextImage();
    } else if (event.key === 'ArrowLeft') {
      this.prevImage();
    }
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement;
    if (target) {
      target.src =
        'data:image/svg+xml;charset=UTF-8,%3Csvg%20width%3D%22600%22%20height%3D%22400%22%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%20600%20400%22%20preserveAspectRatio%3D%22none%22%3E%3Cdefs%3E%3Cstyle%20type%3D%22text%2Fcss%22%3E%23holder_1%20text%20%7B%20fill%3A%238A8A82%3Bfont-weight%3A600%3Bfont-family%3Asans-serif%3Bfont-size%3A18pt%20%7D%3C%2Fstyle%3E%3C%2Fdefs%3E%3Cg%20id%3D%22holder_1%22%3E%3Crect%20width%3D%22600%22%20height%3D%22400%22%20fill%3D%22%23EFECE6%22%2F%3E%3Cg%3E%3Ctext%20x%3D%22180%22%20y%3D%22205%22%3EImage%20Unavailable%3C%2Ftext%3E%3C%2Fg%3E%3C%2Fg%3E%3C%2Fsvg%3E';
    }
  }
}
