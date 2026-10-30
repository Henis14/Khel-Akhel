import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../common/services/api.service';
import { HeroBannerItem } from '../../../admin/models/hero-banner.model';
import { environment } from '../../../../environments/environment';
import { HomeStory } from './home-story/home-story';

@Component({
  selector: 'app-user-home',
  standalone: true,
  imports: [CommonModule, HomeStory],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class UserHomeComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);

  // State
  heroMedia = signal<HeroBannerItem[]>([]);
  activeSlideIndex = signal<number>(0);
  isLoading = signal<boolean>(true);
  hasError = signal<boolean>(false);

  private autoSlideTimer: ReturnType<typeof setInterval> | null = null;
  private isDestroyed = false;

  // Mode resolution
  isImageMode = computed(() => {
    const list = this.heroMedia();
    return list.length > 0 && list.some((m) => m.isImage);
  });

  isVideoMode = computed(() => {
    const list = this.heroMedia();
    return list.length > 0 && list.some((m) => m.isVideo);
  });

  imageBanners = computed(() => {
    return this.heroMedia().filter((m) => m.isImage);
  });

  videoBanner = computed(() => {
    return this.heroMedia().find((m) => m.isVideo) || null;
  });

  currentSlide = computed(() => {
    const images = this.imageBanners();
    if (images.length === 0) return null;
    const index = this.activeSlideIndex() % images.length;
    return images[index];
  });

  currentBackgroundUrl = computed(() => {
    const slide = this.currentSlide();
    if (slide) {
      return this.getFullMediaUrl(slide.mediaUrl);
    }
    return '';
  });

  ngOnInit(): void {
    this.fetchHeroMedia();
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.stopAutoSlide();
  }

  fetchHeroMedia(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    this.api.get<HeroBannerItem[]>('hero-banner').subscribe({
      next: (res) => {
        if (this.isDestroyed) return;
        this.isLoading.set(false);
        if (res.success && res.data && res.data.length > 0) {
          // Sort by display order ascending
          const sorted = [...res.data].sort((a, b) => a.displayOrder - b.displayOrder);
          this.heroMedia.set(sorted);

          // If multiple images, start carousel auto-play
          if (this.isImageMode() && this.imageBanners().length > 1) {
            this.startAutoSlide();
          }
        } else {
          this.heroMedia.set([]);
        }
      },
      error: () => {
        if (this.isDestroyed) return;
        this.isLoading.set(false);
        this.hasError.set(true);
      },
    });
  }

  getFullMediaUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    const base = environment.apiBaseUrl.replace(/\/api\/?$/, '');
    return `${base}${path.startsWith('/') ? path : '/' + path}`;
  }

  prevSlide(): void {
    const count = this.imageBanners().length;
    if (count <= 1) return;
    this.activeSlideIndex.update((curr) => (curr - 1 + count) % count);
    this.restartAutoSlide();
  }

  nextSlide(): void {
    const count = this.imageBanners().length;
    if (count <= 1) return;
    this.activeSlideIndex.update((curr) => (curr + 1) % count);
    this.restartAutoSlide();
  }

  goToSlide(index: number): void {
    const count = this.imageBanners().length;
    if (index >= 0 && index < count) {
      this.activeSlideIndex.set(index);
      this.restartAutoSlide();
    }
  }

  private startAutoSlide(): void {
    this.stopAutoSlide();
    this.autoSlideTimer = setInterval(() => {
      if (this.imageBanners().length > 1) {
        this.activeSlideIndex.update((curr) => (curr + 1) % this.imageBanners().length);
      }
    }, 5500);
  }

  private stopAutoSlide(): void {
    if (this.autoSlideTimer) {
      clearInterval(this.autoSlideTimer);
      this.autoSlideTimer = null;
    }
  }

  private restartAutoSlide(): void {
    this.startAutoSlide();
  }
}
